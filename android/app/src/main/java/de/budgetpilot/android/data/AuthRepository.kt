package de.budgetpilot.android.data

import de.budgetpilot.android.data.local.SettingsStore
import de.budgetpilot.android.data.local.TokenStore
import de.budgetpilot.android.data.remote.AuthApi
import de.budgetpilot.android.data.remote.LoginRequest
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRepository @Inject constructor(
    private val authApi: AuthApi,
    private val tokens: TokenStore,
    private val settings: SettingsStore,
) {
    fun isLoggedIn(): Boolean = !tokens.accessToken().isNullOrBlank()

    suspend fun login(email: String, password: String): Result<Unit> = withContext(Dispatchers.IO) {
        runCatching {
            val response = authApi.login(LoginRequest(email.trim(), password))
            tokens.save(response.accessToken, response.refreshToken)
        }
    }

    fun logout() = tokens.clear()

    fun forgetInstance() {
        tokens.clear()
        settings.clear()
    }
}
