package de.budgetpilot.android.di

import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import de.budgetpilot.android.data.remote.AuthApi
import de.budgetpilot.android.data.remote.AuthInterceptor
import de.budgetpilot.android.data.remote.BaseUrlInterceptor
import de.budgetpilot.android.data.remote.BudgetApi
import de.budgetpilot.android.data.remote.TokenAuthenticator
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory
import javax.inject.Named
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {

    // Echte Ziel-URL setzt der BaseUrlInterceptor zur Laufzeit; das hier ist Platzhalter.
    private const val PLACEHOLDER_BASE_URL = "https://placeholder.invalid/"

    @Provides
    @Singleton
    fun provideJson(): Json = Json {
        ignoreUnknownKeys = true
        isLenient = true
    }

    @Provides
    @Singleton
    fun provideLogging(): HttpLoggingInterceptor =
        HttpLoggingInterceptor().apply { level = HttpLoggingInterceptor.Level.BASIC }

    // Auth-Client: nur Host-Umschreibung + Logging, KEIN Authenticator (für login/refresh).
    @Provides
    @Singleton
    @Named("auth")
    fun provideAuthClient(
        baseUrl: BaseUrlInterceptor,
        logging: HttpLoggingInterceptor,
    ): OkHttpClient = OkHttpClient.Builder()
        .addInterceptor(baseUrl)
        .addInterceptor(logging)
        .build()

    @Provides
    @Singleton
    fun provideAuthApi(@Named("auth") client: OkHttpClient, json: Json): AuthApi =
        buildRetrofit(client, json).create(AuthApi::class.java)

    // API-Client: Host-Umschreibung + Bearer-Header + Auto-Refresh + Logging.
    @Provides
    @Singleton
    @Named("api")
    fun provideApiClient(
        baseUrl: BaseUrlInterceptor,
        auth: AuthInterceptor,
        authenticator: TokenAuthenticator,
        logging: HttpLoggingInterceptor,
    ): OkHttpClient = OkHttpClient.Builder()
        .addInterceptor(baseUrl)
        .addInterceptor(auth)
        .authenticator(authenticator)
        .addInterceptor(logging)
        .build()

    @Provides
    @Singleton
    fun provideBudgetApi(@Named("api") client: OkHttpClient, json: Json): BudgetApi =
        buildRetrofit(client, json).create(BudgetApi::class.java)

    private fun buildRetrofit(client: OkHttpClient, json: Json): Retrofit {
        val contentType = "application/json".toMediaType()
        return Retrofit.Builder()
            .baseUrl(PLACEHOLDER_BASE_URL)
            .client(client)
            .addConverterFactory(json.asConverterFactory(contentType))
            .build()
    }
}
