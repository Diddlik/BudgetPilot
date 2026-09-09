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

@Serializable
data class QuickBudgetItemDto(
    val id: String,
    val name: String,
    val type: String,
    val categoryId: String,
    val isActive: Boolean = true,
)

@Serializable
data class CreateQuickExpenseRequest(
    val requestId: String,
    val amount: Double,
    val categoryId: String,
    val budgetItemId: String? = null,
    val date: String? = null,
    val description: String? = null,
)

@Serializable
data class ActualTransactionDto(
    val id: String,
    val date: String,
    val amount: Double,
)

@Serializable
data class CategoryPlanActualDto(
    val categoryId: String,
    val categoryName: String,
    val plannedExpense: Double = 0.0,
    val actualExpense: Double = 0.0,
    val expenseVariance: Double = 0.0,
)

@Serializable
data class PlanActualComparisonDto(
    val year: Int,
    val month: Int? = null,
    val plannedIncome: Double = 0.0,
    val actualIncome: Double = 0.0,
    val incomeVariance: Double = 0.0,
    val plannedExpense: Double = 0.0,
    val actualExpense: Double = 0.0,
    val expenseVariance: Double = 0.0,
    val plannedBalance: Double = 0.0,
    val actualBalance: Double = 0.0,
    val balanceVariance: Double = 0.0,
    val categories: List<CategoryPlanActualDto> = emptyList(),
)

@Serializable
data class HouseholdSettingsDto(
    val timeZoneId: String,
    val timeZoneDisplayName: String,
    val today: String,
)

@Serializable
data class PlannedOccurrenceDto(
    val occurrenceKey: String,
    val budgetItemId: String,
    val budgetItemName: String,
    val categoryId: String,
    val categoryName: String,
    val type: String,
    val dueDate: String,
    val plannedAmount: Double,
)

@Serializable
data class ConfirmOccurrenceRequest(
    val amount: Double? = null,
    val date: String? = null,
    val description: String? = null,
)

@Serializable
data class SkipOccurrenceRequest(val reason: String? = null)

@Serializable
data class OccurrenceResolutionDto(
    val occurrenceKey: String,
    val kind: String,
    val budgetItemId: String,
    val budgetItemName: String,
    val categoryName: String,
    val plannedDueDate: String,
    val plannedAmount: Double,
    val actualTransactionId: String? = null,
    val actualDate: String? = null,
    val actualAmount: Double? = null,
    val note: String? = null,
)

@Serializable
data class CorrectOccurrenceRequest(
    val amount: Double,
    val date: String,
    val description: String? = null,
)
