package de.budgetpilot.android.data.remote

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query
import retrofit2.http.PUT

interface AuthApi {
    @POST("/api/auth/login")
    suspend fun login(@Body body: LoginRequest): TokenResponse

    @POST("/api/auth/refresh")
    suspend fun refresh(@Body body: RefreshRequest): TokenResponse
}

interface BudgetApi {
    @GET("/api/v1/settings/household")
    suspend fun householdSettings(): HouseholdSettingsDto

    @GET("/api/v1/occurrences/open")
    suspend fun openOccurrences(): List<PlannedOccurrenceDto>

    @POST("/api/v1/occurrences/{budgetItemId}/{plannedDueDate}/confirm")
    suspend fun confirmOccurrence(
        @Path("budgetItemId") budgetItemId: String,
        @Path("plannedDueDate") plannedDueDate: String,
        @Body body: ConfirmOccurrenceRequest,
    ): OccurrenceResolutionDto

    @POST("/api/v1/occurrences/{budgetItemId}/{plannedDueDate}/skip")
    suspend fun skipOccurrence(
        @Path("budgetItemId") budgetItemId: String,
        @Path("plannedDueDate") plannedDueDate: String,
        @Body body: SkipOccurrenceRequest,
    ): OccurrenceResolutionDto

    @GET("/api/v1/occurrences/resolved")
    suspend fun resolvedOccurrences(@Query("max") max: Int = 10): List<OccurrenceResolutionDto>

    @PUT("/api/v1/occurrences/{budgetItemId}/{plannedDueDate}/confirmed")
    suspend fun correctOccurrence(
        @Path("budgetItemId") budgetItemId: String,
        @Path("plannedDueDate") plannedDueDate: String,
        @Body body: CorrectOccurrenceRequest,
    ): OccurrenceResolutionDto

    @POST("/api/v1/occurrences/{budgetItemId}/{plannedDueDate}/reopen")
    suspend fun reopenOccurrence(
        @Path("budgetItemId") budgetItemId: String,
        @Path("plannedDueDate") plannedDueDate: String,
    )

    @GET("/api/v1/categories")
    suspend fun categories(): List<CategoryDto>

    @GET("/api/v1/budget-items")
    suspend fun budgetItems(): List<QuickBudgetItemDto>

    @POST("/api/v1/actual-transactions/quick-expense")
    suspend fun createQuickExpense(@Body body: CreateQuickExpenseRequest): ActualTransactionDto

    @GET("/api/v1/projections/monthly")
    suspend fun monthlyProjection(
        @Query("year") year: Int,
        @Query("month") month: Int,
        @Query("mode") mode: String = "Cashflow",
    ): MonthlyProjectionDto

    @GET("/api/v1/comparisons/monthly")
    suspend fun monthlyComparison(
        @Query("year") year: Int,
        @Query("month") month: Int,
    ): PlanActualComparisonDto

    @GET("/api/v1/comparisons/yearly")
    suspend fun yearlyComparison(@Query("year") year: Int): PlanActualComparisonDto
}
