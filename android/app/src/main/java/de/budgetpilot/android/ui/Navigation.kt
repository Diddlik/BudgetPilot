package de.budgetpilot.android.ui

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import de.budgetpilot.android.ui.dashboard.DashboardScreen
import de.budgetpilot.android.ui.login.LoginScreen
import de.budgetpilot.android.ui.setup.SetupScreen

object Routes {
    const val SETUP = "setup"
    const val LOGIN = "login"
    const val DASHBOARD = "dashboard"
}

@Composable
fun AppNavigation(startDestination: String, modifier: Modifier = Modifier) {
    val nav = rememberNavController()
    NavHost(navController = nav, startDestination = startDestination, modifier = modifier) {
        composable(Routes.SETUP) {
            SetupScreen(onSaved = {
                nav.navigate(Routes.LOGIN) { popUpTo(Routes.SETUP) { inclusive = true } }
            })
        }
        composable(Routes.LOGIN) {
            LoginScreen(
                onLoggedIn = {
                    nav.navigate(Routes.DASHBOARD) { popUpTo(Routes.LOGIN) { inclusive = true } }
                },
                onChangeInstance = {
                    nav.navigate(Routes.SETUP) { popUpTo(0) }
                },
            )
        }
        composable(Routes.DASHBOARD) {
            DashboardScreen(onLoggedOut = {
                nav.navigate(Routes.LOGIN) { popUpTo(0) }
            })
        }
    }
}
