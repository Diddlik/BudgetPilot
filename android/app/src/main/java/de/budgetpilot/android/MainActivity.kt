package de.budgetpilot.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import dagger.hilt.android.AndroidEntryPoint
import de.budgetpilot.android.data.local.SettingsStore
import de.budgetpilot.android.data.local.TokenStore
import de.budgetpilot.android.ui.AppNavigation
import de.budgetpilot.android.ui.Routes
import de.budgetpilot.android.ui.theme.BudgetPilotTheme
import javax.inject.Inject

@AndroidEntryPoint
class MainActivity : ComponentActivity() {

    @Inject lateinit var settings: SettingsStore
    @Inject lateinit var tokens: TokenStore

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        // Startziel: ohne Instanz-URL -> Setup, ohne Token -> Login, sonst Dashboard.
        val start = when {
            settings.instanceUrl().isNullOrBlank() -> Routes.SETUP
            tokens.accessToken().isNullOrBlank() -> Routes.LOGIN
            else -> Routes.DASHBOARD
        }

        setContent {
            BudgetPilotTheme {
                Surface(modifier = Modifier.fillMaxSize()) {
                    Scaffold { padding ->
                        AppNavigation(
                            startDestination = start,
                            modifier = Modifier.padding(padding),
                        )
                    }
                }
            }
        }
    }
}
