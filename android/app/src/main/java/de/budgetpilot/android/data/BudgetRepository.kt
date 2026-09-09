package de.budgetpilot.android.data

import de.budgetpilot.android.data.remote.BudgetApi
import de.budgetpilot.android.data.remote.CategoryDto
import de.budgetpilot.android.data.remote.MonthlyProjectionDto
import de.budgetpilot.android.data.remote.HouseholdSettingsDto
import de.budgetpilot.android.data.remote.PlannedOccurrenceDto
import de.budgetpilot.android.data.remote.ConfirmOccurrenceRequest
import de.budgetpilot.android.data.remote.SkipOccurrenceRequest
import de.budgetpilot.android.data.remote.OccurrenceResolutionDto
import de.budgetpilot.android.data.remote.CorrectOccurrenceRequest
import de.budgetpilot.android.data.remote.PlanActualComparisonDto
import de.budgetpilot.android.data.remote.QuickBudgetItemDto
import de.budgetpilot.android.data.remote.CreateQuickExpenseRequest
import de.budgetpilot.android.data.remote.ActualTransactionDto
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class BudgetRepository @Inject constructor(
    private val api: BudgetApi,
) {
    suspend fun householdSettings(): Result<HouseholdSettingsDto> = withContext(Dispatchers.IO) {
        runCatching { api.householdSettings() }
    }

    suspend fun categories(): Result<List<CategoryDto>> = withContext(Dispatchers.IO) {
        runCatching { api.categories() }
    }

    suspend fun budgetItems(): Result<List<QuickBudgetItemDto>> = withContext(Dispatchers.IO) {
        runCatching { api.budgetItems() }
    }

    suspend fun createQuickExpense(request: CreateQuickExpenseRequest): Result<ActualTransactionDto> =
        withContext(Dispatchers.IO) { runCatching { api.createQuickExpense(request) } }

    suspend fun openOccurrences(): Result<List<PlannedOccurrenceDto>> = withContext(Dispatchers.IO) {
        runCatching { api.openOccurrences() }
    }

    suspend fun confirmOccurrence(
        occurrence: PlannedOccurrenceDto,
        amount: Double,
        date: String,
        description: String?,
    ): Result<OccurrenceResolutionDto> = withContext(Dispatchers.IO) {
        runCatching {
            api.confirmOccurrence(
                occurrence.budgetItemId,
                occurrence.dueDate,
                ConfirmOccurrenceRequest(amount, date, description),
            )
        }
    }

    suspend fun skipOccurrence(
        occurrence: PlannedOccurrenceDto,
        reason: String?,
    ): Result<OccurrenceResolutionDto> = withContext(Dispatchers.IO) {
        runCatching {
            api.skipOccurrence(
                occurrence.budgetItemId,
                occurrence.dueDate,
                SkipOccurrenceRequest(reason),
            )
        }
    }

    suspend fun resolvedOccurrences(): Result<List<OccurrenceResolutionDto>> = withContext(Dispatchers.IO) {
        runCatching { api.resolvedOccurrences() }
    }

    suspend fun correctOccurrence(
        resolution: OccurrenceResolutionDto,
        amount: Double,
        date: String,
        description: String?,
    ): Result<OccurrenceResolutionDto> = withContext(Dispatchers.IO) {
        runCatching {
            api.correctOccurrence(
                resolution.budgetItemId,
                resolution.plannedDueDate,
                CorrectOccurrenceRequest(amount, date, description),
            )
        }
    }

    suspend fun reopenOccurrence(resolution: OccurrenceResolutionDto): Result<Unit> = withContext(Dispatchers.IO) {
        runCatching { api.reopenOccurrence(resolution.budgetItemId, resolution.plannedDueDate) }
    }

    suspend fun monthly(year: Int, month: Int, mode: String = "Cashflow"): Result<MonthlyProjectionDto> =
        withContext(Dispatchers.IO) {
            runCatching { api.monthlyProjection(year, month, mode) }
        }

    suspend fun monthlyComparison(year: Int, month: Int): Result<PlanActualComparisonDto> =
        withContext(Dispatchers.IO) {
            runCatching { api.monthlyComparison(year, month) }
        }

    suspend fun yearlyComparison(year: Int): Result<PlanActualComparisonDto> =
        withContext(Dispatchers.IO) {
            runCatching { api.yearlyComparison(year) }
        }
}
