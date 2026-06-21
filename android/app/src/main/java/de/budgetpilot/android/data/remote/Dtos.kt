package de.budgetpilot.android.data.remote

import kotlinx.serialization.Serializable

// ── Auth ─────────────────────────────────────────────────────────────────────
@Serializable
data class LoginRequest(val email: String, val password: String)

@Serializable
data class RefreshRequest(val refreshToken: String)

/** Antwort von /api/auth/login und /refresh (ASP.NET AccessTokenResponse, camelCase). */
@Serializable
data class TokenResponse(
    val tokenType: String? = null,
    val accessToken: String,
    val expiresIn: Long = 0,
    val refreshToken: String,
)

// ── Daten ────────────────────────────────────────────────────────────────────
// Hinweis: Geldbeträge hier vorerst als Double für die Anzeige. Für korrekte
// Rundung später auf BigDecimal/serialisierte Strings umstellen.
@Serializable
data class CategoryDto(
    val id: String,
    val name: String,
    val isActive: Boolean = true,
    val itemCount: Int = 0,
)

@Serializable
data class MonthlyProjectionDto(
    val year: Int = 0,
    val month: Int = 0,
    val totalIncome: Double = 0.0,
    val totalExpense: Double = 0.0,
    val balance: Double = 0.0,
)
