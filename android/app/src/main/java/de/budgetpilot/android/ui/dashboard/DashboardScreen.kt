package de.budgetpilot.android.ui.dashboard

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
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
import kotlinx.coroutines.launch
import java.text.NumberFormat
import java.time.LocalDate
import java.util.Locale
import javax.inject.Inject

data class DashboardUiState(
    val loading: Boolean = true,
    val error: String? = null,
    val income: Double = 0.0,
    val expense: Double = 0.0,
    val balance: Double = 0.0,
    val categories: List<CategoryDto> = emptyList(),
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
            val today = LocalDate.now()
            val monthly = repo.monthly(today.year, today.monthValue)
            val categories = repo.categories()

            state = when {
                monthly.isSuccess && categories.isSuccess -> {
                    val m = monthly.getOrThrow()
                    DashboardUiState(
                        loading = false,
                        income = m.totalIncome,
                        expense = m.totalExpense,
                        balance = m.balance,
                        categories = categories.getOrThrow().filter { it.isActive },
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
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    onLoggedOut: () -> Unit,
    viewModel: DashboardViewModel = hiltViewModel(),
) {
    val state = viewModel.state
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Dashboard") },
                actions = {
                    TextButton(onClick = { viewModel.logout(onLoggedOut) }) { Text("Abmelden") }
                },
            )
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
                    KpiCard("Einnahmen", state.income)
                    KpiCard("Ausgaben", state.expense)
                    KpiCard("Saldo", state.balance)
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
}

@Composable
private fun KpiCard(label: String, value: Double) {
    Card(modifier = Modifier.fillMaxWidth().padding(bottom = 8.dp)) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium)
            Text(formatEur(value), style = MaterialTheme.typography.headlineSmall)
        }
    }
}

private fun formatEur(value: Double): String =
    NumberFormat.getCurrencyInstance(Locale.GERMANY).format(value)
