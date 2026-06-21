package de.budgetpilot.android.data.remote

import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Query

interface AuthApi {
    @POST("/api/auth/login")
    suspend fun login(@Body body: LoginRequest): TokenResponse

    @POST("/api/auth/refresh")
    suspend fun refresh(@Body body: RefreshRequest): TokenResponse
}

interface BudgetApi {
    @GET("/api/v1/categories")
    suspend fun categories(): List<CategoryDto>

    @GET("/api/v1/projections/monthly")
    suspend fun monthlyProjection(
        @Query("year") year: Int,
        @Query("month") month: Int,
        @Query("mode") mode: String = "Cashflow",
    ): MonthlyProjectionDto
}
