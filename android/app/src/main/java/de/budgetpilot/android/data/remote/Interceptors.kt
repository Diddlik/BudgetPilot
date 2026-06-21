package de.budgetpilot.android.data.remote

import dagger.Lazy
import de.budgetpilot.android.data.local.SettingsStore
import de.budgetpilot.android.data.local.TokenStore
import kotlinx.coroutines.runBlocking
import okhttp3.Authenticator
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import okhttp3.Interceptor
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route
import javax.inject.Inject

/**
 * Schreibt Schema/Host/Port jeder Anfrage auf die konfigurierte Instanz-URL um.
 * Dadurch ist die Retrofit-Basis-URL nur ein Platzhalter und die Zielinstanz kann
 * zur Laufzeit gewechselt werden (Pfad/Query bleiben erhalten).
 */
class BaseUrlInterceptor @Inject constructor(
    private val settings: SettingsStore,
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val base = settings.instanceUrl()?.toHttpUrlOrNull()
            ?: return chain.proceed(request)

        val newUrl = request.url.newBuilder()
            .scheme(base.scheme)
            .host(base.host)
            .port(base.port)
            .build()
        return chain.proceed(request.newBuilder().url(newUrl).build())
    }
}

/** Hängt den Bearer-Access-Token an (außer an die Auth-Endpunkte selbst). */
class AuthInterceptor @Inject constructor(
    private val tokens: TokenStore,
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val token = tokens.accessToken()
        val isAuthRoute = request.url.encodedPath.startsWith("/api/auth/")
        if (token.isNullOrBlank() || isAuthRoute) {
            return chain.proceed(request)
        }
        return chain.proceed(
            request.newBuilder().header("Authorization", "Bearer $token").build(),
        )
    }
}

/**
 * Erneuert den Token bei 401 automatisch über /api/auth/refresh und wiederholt die
 * Anfrage. Schlägt der Refresh fehl, werden die Token verworfen (-> erneuter Login).
 * authApi als Lazy, um Initialisierungs-Zyklen zu vermeiden (der Refresh-Client hat
 * selbst KEINEN Authenticator).
 */
class TokenAuthenticator @Inject constructor(
    private val authApi: Lazy<AuthApi>,
    private val tokens: TokenStore,
) : Authenticator {
    override fun authenticate(route: Route?, response: Response): Request? {
        if (responseCount(response) >= 2) return null // schon einmal erneuert
        val refresh = tokens.refreshToken() ?: return null

        val newTokens = runBlocking {
            runCatching { authApi.get().refresh(RefreshRequest(refresh)) }.getOrNull()
        }
        if (newTokens == null) {
            tokens.clear()
            return null
        }
        tokens.save(newTokens.accessToken, newTokens.refreshToken)

        return response.request.newBuilder()
            .header("Authorization", "Bearer ${newTokens.accessToken}")
            .build()
    }

    private fun responseCount(response: Response): Int {
        var count = 1
        var prior = response.priorResponse
        while (prior != null) {
            count++
            prior = prior.priorResponse
        }
        return count
    }
}
