package de.budgetpilot.android.data.local

import android.content.Context
import android.content.SharedPreferences
import dagger.hilt.android.qualifiers.ApplicationContext
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Speichert die selbst-gehostete Instanz-URL. Bewusst synchron (SharedPreferences),
 * damit der OkHttp-BaseUrlInterceptor sie ohne Coroutine lesen kann.
 */
@Singleton
class SettingsStore @Inject constructor(
    @ApplicationContext context: Context,
) {
    private val prefs: SharedPreferences =
        context.getSharedPreferences("settings", Context.MODE_PRIVATE)

    fun instanceUrl(): String? = prefs.getString(KEY_URL, null)

    fun setInstanceUrl(url: String) {
        // Normalisieren: trimmen und auf abschließenden Slash bringen.
        val normalized = url.trim().removeSuffix("/") + "/"
        prefs.edit().putString(KEY_URL, normalized).apply()
    }

    fun clear() = prefs.edit().remove(KEY_URL).apply()

    private companion object {
        const val KEY_URL = "instance_url"
    }
}
