package de.budgetpilot.android.ui.setup

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import dagger.hilt.android.lifecycle.HiltViewModel
import de.budgetpilot.android.data.local.SettingsStore
import androidx.compose.foundation.text.KeyboardOptions
import javax.inject.Inject

@HiltViewModel
class SetupViewModel @Inject constructor(
    private val settings: SettingsStore,
) : ViewModel() {
    var url by mutableStateOf(settings.instanceUrl() ?: "https://")
        private set

    fun onUrlChange(value: String) { url = value }

    fun save(onSaved: () -> Unit) {
        val value = url.trim()
        if (value.startsWith("http://") || value.startsWith("https://")) {
            settings.setInstanceUrl(value)
            onSaved()
        }
    }
}

@Composable
fun SetupScreen(
    onSaved: () -> Unit,
    viewModel: SetupViewModel = hiltViewModel(),
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.Center,
    ) {
        Text("BudgetPilot", style = MaterialTheme.typography.headlineMedium)
        Text(
            "Adresse deiner Instanz",
            style = MaterialTheme.typography.bodyMedium,
            modifier = Modifier.padding(top = 4.dp, bottom = 16.dp),
        )
        OutlinedTextField(
            value = viewModel.url,
            onValueChange = viewModel::onUrlChange,
            label = { Text("https://budget.deine-domain.de") },
            singleLine = true,
            modifier = Modifier.fillMaxWidth(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri, imeAction = ImeAction.Done),
        )
        Button(
            onClick = { viewModel.save(onSaved) },
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 16.dp),
        ) {
            Text("Weiter")
        }
    }
}
