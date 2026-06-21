package de.budgetpilot.android.data

import de.budgetpilot.android.data.remote.BudgetApi
import de.budgetpilot.android.data.remote.CategoryDto
import de.budgetpilot.android.data.remote.MonthlyProjectionDto
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class BudgetRepository @Inject constructor(
    private val api: BudgetApi,
) {
    suspend fun categories(): Result<List<CategoryDto>> = withContext(Dispatchers.IO) {
        runCatching { api.categories() }
    }

    suspend fun monthly(year: Int, month: Int, mode: String = "Cashflow"): Result<MonthlyProjectionDto> =
        withContext(Dispatchers.IO) {
            runCatching { api.monthlyProjection(year, month, mode) }
        }
}
