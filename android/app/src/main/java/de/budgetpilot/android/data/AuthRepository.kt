package de.budgetpilot.android.data

import de.budgetpilot.android.data.local.SettingsStore
import de.budgetpilot.android.data.local.TokenStore
import de.budgetpilot.android.data.remote.AuthApi
import de.budgetpilot.android.data.remote.LoginRequest
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import retrofit2.HttpException
import java.io.IOException
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
        try {
            val response = authApi.login(LoginRequest(email.trim(), password))
            tokens.save(response.accessToken, response.refreshToken)
            Result.success(Unit)
        } catch (e: HttpException) {
            // Echte HTTP-Antwort vom Server: 401 = wirklich falsche Zugangsdaten.
            val message = if (e.code() == 401)
                "E-Mail oder Passwort ist falsch."
            else
                "Server antwortete mit HTTP ${e.code()}."
            Result.failure(IllegalStateException(message, e))
        } catch (e: IOException) {
            // Kein HTTP-Status: Server nicht erreichbar, falsche URL, HTTPS-Redirect,
            // Cleartext blockiert. NICHT mit „falsches Passwort" verwechseln.
            Result.failure(IllegalStateException(
                "Instanz nicht erreichbar. URL/Verbindung prüfen " +
                    "(Emulator: http://10.0.2.2:5070). Details: ${e.message}", e))
        }
    }

    fun logout() = tokens.clear()

    fun forgetInstance() {
        tokens.clear()
        settings.clear()
    }
}
