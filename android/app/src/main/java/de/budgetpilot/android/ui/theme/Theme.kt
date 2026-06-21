package de.budgetpilot.android.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val Accent = Color(0xFFC2410C)   // BudgetPilot-Akzent (wie Web-Prototyp)
private val Income = Color(0xFF1E7A4D)

private val LightColors = lightColorScheme(primary = Accent, secondary = Income)
private val DarkColors = darkColorScheme(primary = Accent, secondary = Income)

@Composable
fun BudgetPilotTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit,
) {
    MaterialTheme(
        colorScheme = if (darkTheme) DarkColors else LightColors,
        content = content,
    )
}
