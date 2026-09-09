package de.budgetpilot.android.ui.dashboard

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Card
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import de.budgetpilot.android.data.AuthRepository
import de.budgetpilot.android.data.BudgetRepository
import de.budgetpilot.android.data.remote.CategoryDto
import de.budgetpilot.android.data.remote.PlannedOccurrenceDto
import de.budgetpilot.android.data.remote.OccurrenceResolutionDto
import de.budgetpilot.android.data.remote.PlanActualComparisonDto
import de.budgetpilot.android.data.remote.QuickBudgetItemDto
import de.budgetpilot.android.data.remote.CreateQuickExpenseRequest
import kotlinx.coroutines.launch
import java.io.IOException
import java.text.NumberFormat
import java.time.LocalDate
import java.util.Locale
import java.util.UUID
import javax.inject.Inject

data class DashboardUiState(
    val loading: Boolean = true,
    val error: String? = null,
    val income: Double = 0.0,
    val expense: Double = 0.0,
    val balance: Double = 0.0,
    val categories: List<CategoryDto> = emptyList(),
    val budgetItems: List<QuickBudgetItemDto> = emptyList(),
    val openOccurrences: List<PlannedOccurrenceDto> = emptyList(),
    val recentResolutions: List<OccurrenceResolutionDto> = emptyList(),
    val comparison: PlanActualComparisonDto? = null,
    val today: String = "",
    val quickSaving: Boolean = false,
    val quickError: String? = null,
)

@HiltViewModel
class DashboardViewModel @Inject constructor(
    private val repo: BudgetRepository,
    private val auth: AuthRepository,
) : ViewModel() {
    var state by mutableStateOf(DashboardUiState())
        private set

    init { load() }

    fun load() {
        state = state.copy(loading = true, error = null)
        viewModelScope.launch {
            val household = repo.householdSettings()
            val today = household.mapCatching { LocalDate.parse(it.today) }
            val monthly = today.fold(
                onSuccess = { repo.monthly(it.year, it.monthValue) },
                onFailure = { Result.failure(it) },
            )
            val categories = repo.categories()
            val budgetItems = repo.budgetItems()
            val occurrences = repo.openOccurrences()
            val resolutions = repo.resolvedOccurrences()
            val comparison = today.fold(
                onSuccess = { repo.monthlyComparison(it.year, it.monthValue) },
                onFailure = { Result.failure(it) },
            )

            state = when {
                monthly.isSuccess && categories.isSuccess && budgetItems.isSuccess && occurrences.isSuccess && resolutions.isSuccess && comparison.isSuccess -> {
                    val m = monthly.getOrThrow()
                    DashboardUiState(
                        loading = false,
                        income = m.totalIncome,
                        expense = m.totalExpense,
                        balance = m.balance,
                        categories = categories.getOrThrow().filter { it.isActive },
                        budgetItems = budgetItems.getOrThrow().filter { it.isActive && it.type == "Expense" },
                        openOccurrences = occurrences.getOrThrow(),
                        recentResolutions = resolutions.getOrThrow(),
                        comparison = comparison.getOrThrow(),
                        today = today.getOrThrow().toString(),
                    )
                }
                else -> state.copy(
                    loading = false,
                    error = "Daten konnten nicht geladen werden. Verbindung/Anmeldung prüfen.",
                )
            }
        }
    }

    fun logout(onLoggedOut: () -> Unit) {
        auth.logout()
        onLoggedOut()
    }

    fun clearQuickExpenseError() {
        state = state.copy(quickError = null)
    }

    fun createQuickExpense(
        requestId: String,
        amount: Double,
        categoryId: String,
        budgetItemId: String?,
        description: String?,
        onSaved: () -> Unit,
    ) {
        if (state.quickSaving) return
        state = state.copy(quickSaving = true, quickError = null)
        viewModelScope.launch {
            repo.createQuickExpense(CreateQuickExpenseRequest(
                requestId, amount, categoryId, budgetItemId, null, description,
            )).onSuccess {
                state = state.copy(quickSaving = false)
                onSaved()
                load()
            }.onFailure {
                state = state.copy(
                    quickSaving = false,
                    quickError = if (it is IOException)
                        "Keine Verbindung. Du kannst dieselbe Ausgabe sicher erneut senden."
                    else "Die Ausgabe konnte nicht gespeichert werden. Bitte Eingaben prüfen.",
                )
            }
        }
    }

    fun confirmOccurrence(
        occurrence: PlannedOccurrenceDto,
        amount: Double,
        date: String,
        description: String?,
    ) {
        viewModelScope.launch {
            repo.confirmOccurrence(occurrence, amount, date, description)
                .onSuccess { load() }
                .onFailure { state = state.copy(error = "Die Zahlung konnte nicht gespeichert werden.") }
        }
    }

    fun skipOccurrence(occurrence: PlannedOccurrenceDto, reason: String?) {
        viewModelScope.launch {
            repo.skipOccurrence(occurrence, reason)
                .onSuccess { load() }
                .onFailure { state = state.copy(error = "Die Fälligkeit konnte nicht als entfallen markiert werden.") }
        }
    }

    fun correctOccurrence(
        resolution: OccurrenceResolutionDto,
        amount: Double,
        date: String,
        description: String?,
    ) {
        viewModelScope.launch {
            repo.correctOccurrence(resolution, amount, date, description)
                .onSuccess { load() }
                .onFailure { state = state.copy(error = "Die Korrektur konnte nicht gespeichert werden.") }
        }
    }

    fun reopenOccurrence(resolution: OccurrenceResolutionDto) {
        viewModelScope.launch {
            repo.reopenOccurrence(resolution)
                .onSuccess { load() }
                .onFailure { state = state.copy(error = "Die Fälligkeit konnte nicht wieder geöffnet werden.") }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    onLoggedOut: () -> Unit,
    viewModel: DashboardViewModel = hiltViewModel(),
) {
    val state = viewModel.state
    var showAllOccurrences by rememberSaveable { mutableStateOf(false) }
    var confirmOccurrence by remember { mutableStateOf<PlannedOccurrenceDto?>(null) }
    var skipOccurrence by remember { mutableStateOf<PlannedOccurrenceDto?>(null) }
    var correctResolution by remember { mutableStateOf<OccurrenceResolutionDto?>(null) }
    var reopenResolution by remember { mutableStateOf<OccurrenceResolutionDto?>(null) }
    var amount by remember { mutableStateOf("") }
    var actualDate by remember { mutableStateOf("") }
    var note by remember { mutableStateOf("") }
    var showQuickExpense by rememberSaveable { mutableStateOf(false) }
    var quickRequestId by rememberSaveable { mutableStateOf(UUID.randomUUID().toString()) }
    var quickAmount by rememberSaveable { mutableStateOf("") }
    var quickCategoryId by rememberSaveable { mutableStateOf("") }
    var quickBudgetItemId by rememberSaveable { mutableStateOf<String?>(null) }
    var quickNote by rememberSaveable { mutableStateOf("") }
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Dashboard") },
                actions = {
                    TextButton(onClick = { viewModel.logout(onLoggedOut) }) { Text("Abmelden") }
                },
            )
        },
        floatingActionButton = {
            if (!state.loading && state.error == null) {
                ExtendedFloatingActionButton(
                    onClick = {
                        viewModel.clearQuickExpenseError()
                        quickRequestId = UUID.randomUUID().toString()
                        quickAmount = ""
                        quickCategoryId = ""
                        quickBudgetItemId = null
                        quickNote = ""
                        showQuickExpense = true
                    },
                    text = { Text("＋ Ausgabe") },
                )
            }
        },
    ) { padding ->
        when {
            state.loading -> Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center,
            ) { CircularProgressIndicator() }

            state.error != null -> Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center,
            ) {
                Text(state.error, color = MaterialTheme.colorScheme.error)
                TextButton(onClick = { viewModel.load() }) { Text("Erneut versuchen") }
            }

            else -> LazyColumn(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                item {
                    state.comparison?.let { PlanActualCard(it) }
                    Text(
                        "Offene Zahlungen (${state.openOccurrences.size})",
                        style = MaterialTheme.typography.titleMedium,
                        modifier = Modifier.padding(top = 8.dp),
                    )
                }
                items(if (showAllOccurrences) state.openOccurrences else state.openOccurrences.take(6)) { occurrence ->
                    Column(modifier = Modifier.fillMaxWidth().padding(vertical = 10.dp)) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                        ) {
                            Column {
                                Text(occurrence.budgetItemName)
                                Text(
                                    "${occurrence.categoryName} · ${occurrence.dueDate}",
                                    style = MaterialTheme.typography.bodySmall,
                                )
                                Text(
                                    if (occurrence.dueDate < state.today) "Überfällig" else "Geplant",
                                    style = MaterialTheme.typography.labelSmall,
                                    color = if (occurrence.dueDate < state.today)
                                        MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.primary,
                                )
                            }
                            Text(formatEur(occurrence.plannedAmount))
                        }
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            TextButton(onClick = {
                                confirmOccurrence = occurrence
                                amount = occurrence.plannedAmount.toString()
                                actualDate = occurrence.dueDate
                                note = ""
                            }) { Text("Bestätigen") }
                            TextButton(onClick = {
                                skipOccurrence = occurrence
                                note = ""
                            }) { Text("Entfallen") }
                        }
                    }
                    HorizontalDivider()
                }
                if (state.openOccurrences.size > 6) {
                    item {
                        TextButton(onClick = { showAllOccurrences = !showAllOccurrences }) {
                            Text(
                                if (showAllOccurrences) "Weniger anzeigen"
                                else "Weitere ${state.openOccurrences.size - 6} Zahlungen anzeigen",
                            )
                        }
                    }
                }
                if (state.recentResolutions.isNotEmpty()) {
                    item {
                        Text(
                            "Zuletzt bearbeitet",
                            style = MaterialTheme.typography.titleMedium,
                            modifier = Modifier.padding(top = 8.dp),
                        )
                    }
                    items(state.recentResolutions) { resolution ->
                        Column(modifier = Modifier.fillMaxWidth().padding(vertical = 10.dp)) {
                            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                                Column {
                                    Text(resolution.budgetItemName)
                                    Text(
                                        "${resolution.categoryName} · geplant ${resolution.plannedDueDate}",
                                        style = MaterialTheme.typography.bodySmall,
                                    )
                                    Text(if (resolution.kind == "Confirmed") "Bezahlt" else "Entfallen")
                                }
                                Text(resolution.actualAmount?.let(::formatEur) ?: "–")
                            }
                            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                if (resolution.kind == "Confirmed") {
                                    TextButton(onClick = {
                                        correctResolution = resolution
                                        amount = (resolution.actualAmount ?: resolution.plannedAmount).toString()
                                        actualDate = resolution.actualDate ?: resolution.plannedDueDate
                                        note = resolution.note.orEmpty()
                                    }) { Text("Korrigieren") }
                                }
                                TextButton(onClick = { reopenResolution = resolution }) { Text("Wieder öffnen") }
                            }
                        }
                        HorizontalDivider()
                    }
                }
                item {
                    Text(
                        "Kategorien",
                        style = MaterialTheme.typography.titleMedium,
                        modifier = Modifier.padding(top = 8.dp),
                    )
                }
                items(state.categories) { category ->
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(vertical = 10.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                    ) {
                        Text(category.name)
                        Text("${category.itemCount}")
                    }
                    HorizontalDivider()
                }
            }
        }
    }

    if (showQuickExpense) {
        var categoryMenu by remember { mutableStateOf(false) }
        var itemMenu by remember { mutableStateOf(false) }
        val selectedCategory = state.categories.firstOrNull { it.id == quickCategoryId }
        val matchingItems = state.budgetItems.filter { it.categoryId == quickCategoryId }
        val selectedItem = matchingItems.firstOrNull { it.id == quickBudgetItemId }
        AlertDialog(
            onDismissRequest = { if (!state.quickSaving) showQuickExpense = false },
            title = { Text("Ausgabe erfassen") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    OutlinedTextField(
                        value = quickAmount,
                        onValueChange = { quickAmount = it },
                        label = { Text("Betrag in Euro") },
                        singleLine = true,
                    )
                    Box {
                        TextButton(onClick = { categoryMenu = true }, modifier = Modifier.fillMaxWidth()) {
                            Text(selectedCategory?.name ?: "Kategorie auswählen")
                        }
                        DropdownMenu(expanded = categoryMenu, onDismissRequest = { categoryMenu = false }) {
                            state.categories.forEach { category ->
                                DropdownMenuItem(
                                    text = { Text(category.name) },
                                    onClick = {
                                        quickCategoryId = category.id
                                        quickBudgetItemId = null
                                        categoryMenu = false
                                    },
                                )
                            }
                        }
                    }
                    if (quickCategoryId.isNotEmpty() && matchingItems.isNotEmpty()) {
                        Box {
                            TextButton(onClick = { itemMenu = true }, modifier = Modifier.fillMaxWidth()) {
                                Text(selectedItem?.name ?: "Budgetposition (optional)")
                            }
                            DropdownMenu(expanded = itemMenu, onDismissRequest = { itemMenu = false }) {
                                DropdownMenuItem(text = { Text("Keine Zuordnung") }, onClick = {
                                    quickBudgetItemId = null
                                    itemMenu = false
                                })
                                matchingItems.forEach { item ->
                                    DropdownMenuItem(text = { Text(item.name) }, onClick = {
                                        quickBudgetItemId = item.id
                                        itemMenu = false
                                    })
                                }
                            }
                        }
                    }
                    OutlinedTextField(
                        value = quickNote,
                        onValueChange = { quickNote = it.take(500) },
                        label = { Text("Notiz (optional)") },
                    )
                    state.quickError?.let { Text(it, color = MaterialTheme.colorScheme.error) }
                }
            },
            confirmButton = {
                TextButton(
                    enabled = !state.quickSaving && (parseAmount(quickAmount) ?: 0.0) > 0 && quickCategoryId.isNotEmpty(),
                    onClick = {
                        viewModel.createQuickExpense(
                            quickRequestId,
                            parseAmount(quickAmount)!!,
                            quickCategoryId,
                            quickBudgetItemId,
                            quickNote.ifBlank { null },
                        ) { showQuickExpense = false }
                    },
                ) { Text(if (state.quickSaving) "Wird gespeichert …" else "Speichern") }
            },
            dismissButton = {
                TextButton(enabled = !state.quickSaving, onClick = { showQuickExpense = false }) { Text("Abbrechen") }
            },
        )
    }

    confirmOccurrence?.let { occurrence ->
        AlertDialog(
            onDismissRequest = { confirmOccurrence = null },
            title = { Text("Zahlung bestätigen") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    OutlinedTextField(value = amount, onValueChange = { amount = it }, label = { Text("Betrag") })
                    OutlinedTextField(value = actualDate, onValueChange = { actualDate = it }, label = { Text("Bezahlt am (JJJJ-MM-TT)") })
                    OutlinedTextField(value = note, onValueChange = { note = it.take(500) }, label = { Text("Notiz (optional)") })
                }
            },
            confirmButton = {
                TextButton(
                    enabled = amount.toDoubleOrNull() != null && runCatching { LocalDate.parse(actualDate) }.isSuccess,
                    onClick = {
                        viewModel.confirmOccurrence(occurrence, amount.toDouble(), actualDate, note.ifBlank { null })
                        confirmOccurrence = null
                    },
                ) { Text("Als bezahlt speichern") }
            },
            dismissButton = { TextButton(onClick = { confirmOccurrence = null }) { Text("Abbrechen") } },
        )
    }

    skipOccurrence?.let { occurrence ->
        AlertDialog(
            onDismissRequest = { skipOccurrence = null },
            title = { Text("Fälligkeit entfallen lassen?") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    Text("Nur diese einzelne Fälligkeit wird dauerhaft als entfallen markiert.")
                    OutlinedTextField(value = note, onValueChange = { note = it.take(500) }, label = { Text("Grund (optional)") })
                }
            },
            confirmButton = {
                TextButton(onClick = {
                    viewModel.skipOccurrence(occurrence, note.ifBlank { null })
                    skipOccurrence = null
                }) { Text("Wirklich entfallen") }
            },
            dismissButton = { TextButton(onClick = { skipOccurrence = null }) { Text("Abbrechen") } },
        )
    }

    correctResolution?.let { resolution ->
        AlertDialog(
            onDismissRequest = { correctResolution = null },
            title = { Text("Zahlung korrigieren") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    OutlinedTextField(value = amount, onValueChange = { amount = it }, label = { Text("Betrag") })
                    OutlinedTextField(value = actualDate, onValueChange = { actualDate = it }, label = { Text("Bezahlt am (JJJJ-MM-TT)") })
                    OutlinedTextField(value = note, onValueChange = { note = it.take(500) }, label = { Text("Notiz (optional)") })
                }
            },
            confirmButton = {
                TextButton(
                    enabled = amount.toDoubleOrNull() != null && runCatching { LocalDate.parse(actualDate) }.isSuccess,
                    onClick = {
                        viewModel.correctOccurrence(resolution, amount.toDouble(), actualDate, note.ifBlank { null })
                        correctResolution = null
                    },
                ) { Text("Korrektur speichern") }
            },
            dismissButton = { TextButton(onClick = { correctResolution = null }) { Text("Abbrechen") } },
        )
    }

    reopenResolution?.let { resolution ->
        AlertDialog(
            onDismissRequest = { reopenResolution = null },
            title = { Text("Fälligkeit wieder öffnen?") },
            text = { Text("Die Bearbeitung wird zurückgenommen und die Fälligkeit erscheint wieder als offen.") },
            confirmButton = {
                TextButton(onClick = {
                    viewModel.reopenOccurrence(resolution)
                    reopenResolution = null
                }) { Text("Wirklich wieder öffnen") }
            },
            dismissButton = { TextButton(onClick = { reopenResolution = null }) { Text("Abbrechen") } },
        )
    }
}

@Composable
private fun PlanActualCard(comparison: PlanActualComparisonDto) {
    Card(modifier = Modifier.fillMaxWidth().padding(bottom = 8.dp)) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text("Plan und Ist", style = MaterialTheme.typography.titleMedium)
            PlanActualLine("Einnahmen", comparison.plannedIncome, comparison.actualIncome, comparison.incomeVariance)
            PlanActualLine("Ausgaben", comparison.plannedExpense, comparison.actualExpense, comparison.expenseVariance)
            PlanActualLine("Saldo", comparison.plannedBalance, comparison.actualBalance, comparison.balanceVariance)
        }
    }
}

@Composable
private fun PlanActualLine(label: String, planned: Double, actual: Double, variance: Double) {
    Column {
        Text(label, style = MaterialTheme.typography.labelLarge)
        Text("Plan ${formatEur(planned)} · Ist ${formatEur(actual)}")
        Text(
            if (variance == 0.0) "Genau im Plan" else "Abweichung ${formatSignedEur(variance)}",
            style = MaterialTheme.typography.bodySmall,
            color = if (variance == 0.0) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.error,
        )
    }
}

private fun formatEur(value: Double): String =
    NumberFormat.getCurrencyInstance(Locale.GERMANY).format(value)

private fun formatSignedEur(value: Double): String =
    (if (value > 0) "+" else "") + formatEur(value)

private fun parseAmount(value: String): Double? = value.trim().replace(',', '.').toDoubleOrNull()
