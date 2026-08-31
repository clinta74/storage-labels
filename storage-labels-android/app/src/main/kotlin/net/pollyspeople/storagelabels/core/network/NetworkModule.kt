package net.pollyspeople.storagelabels.core.network

import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.serialization.json.Json
import net.pollyspeople.storagelabels.BuildConfig
import net.pollyspeople.storagelabels.data.api.AuthApi
import net.pollyspeople.storagelabels.data.api.BoxApi
import net.pollyspeople.storagelabels.data.api.ItemApi
import net.pollyspeople.storagelabels.data.api.LocationApi
import net.pollyspeople.storagelabels.data.api.UserApi
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory
import java.util.concurrent.TimeUnit
import javax.inject.Named
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {

    /**
     * Requests are rewritten onto the user's server by [HostSelectionInterceptor], so this
     * value is never contacted — Retrofit just requires a syntactically valid base URL.
     */
    private const val PLACEHOLDER_BASE_URL = "http://server.invalid/"

    @Provides
    @Singleton
    fun provideJson(): Json = Json {
        ignoreUnknownKeys = true
        explicitNulls = false
        coerceInputValues = true
    }

    @Provides
    @Singleton
    fun provideLoggingInterceptor(): HttpLoggingInterceptor =
        HttpLoggingInterceptor().apply {
            // Headers carry the bearer token, so bodies and headers stay out of release logs.
            level = if (BuildConfig.DEBUG) HttpLoggingInterceptor.Level.BASIC else HttpLoggingInterceptor.Level.NONE
        }

    /** Client used for the refresh call itself: no authenticator, so a 401 cannot recurse. */
    @Provides
    @Singleton
    @Named("refresh")
    fun provideRefreshClient(
        hostSelection: HostSelectionInterceptor,
        cookieJar: PersistentCookieJar,
        logging: HttpLoggingInterceptor,
    ): OkHttpClient = OkHttpClient.Builder()
        .cookieJar(cookieJar)
        .addInterceptor(hostSelection)
        .addInterceptor(logging)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .build()

    @Provides
    @Singleton
    fun provideOkHttpClient(
        hostSelection: HostSelectionInterceptor,
        auth: AuthInterceptor,
        authenticator: TokenAuthenticator,
        cookieJar: PersistentCookieJar,
        logging: HttpLoggingInterceptor,
    ): OkHttpClient = OkHttpClient.Builder()
        .cookieJar(cookieJar)
        .addInterceptor(hostSelection)
        .addInterceptor(auth)
        .addInterceptor(logging)
        .authenticator(authenticator)
        .connectTimeout(30, TimeUnit.SECONDS)
        // Image uploads and key rotations can be slow; the web client allows four minutes.
        .readTimeout(240, TimeUnit.SECONDS)
        .writeTimeout(240, TimeUnit.SECONDS)
        .build()

    private fun retrofit(client: OkHttpClient, json: Json): Retrofit = Retrofit.Builder()
        .baseUrl(PLACEHOLDER_BASE_URL)
        .client(client)
        .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
        .build()

    /**
     * Images are fetched by absolute URL (the API returns paths like /api/images/{id}), so
     * this client deliberately omits the host-rewriting interceptor while keeping the bearer
     * token and refresh behaviour.
     */
    @Provides
    @Singleton
    @Named("images")
    fun provideImageClient(
        auth: AuthInterceptor,
        authenticator: TokenAuthenticator,
        cookieJar: PersistentCookieJar,
    ): OkHttpClient = OkHttpClient.Builder()
        .cookieJar(cookieJar)
        .addInterceptor(auth)
        .authenticator(authenticator)
        .connectTimeout(30, TimeUnit.SECONDS)
        .readTimeout(60, TimeUnit.SECONDS)
        .build()

    @Provides
    @Singleton
    fun provideRetrofit(client: OkHttpClient, json: Json): Retrofit = retrofit(client, json)

    @Provides
    @Singleton
    @Named("refresh")
    fun provideRefreshRetrofit(@Named("refresh") client: OkHttpClient, json: Json): Retrofit =
        retrofit(client, json)

    @Provides
    @Singleton
    fun provideAuthApi(retrofit: Retrofit): AuthApi = retrofit.create(AuthApi::class.java)

    @Provides
    @Singleton
    fun provideUserApi(retrofit: Retrofit): UserApi = retrofit.create(UserApi::class.java)

    @Provides
    @Singleton
    fun provideLocationApi(retrofit: Retrofit): LocationApi = retrofit.create(LocationApi::class.java)

    @Provides
    @Singleton
    fun provideBoxApi(retrofit: Retrofit): BoxApi = retrofit.create(BoxApi::class.java)

    @Provides
    @Singleton
    fun provideItemApi(retrofit: Retrofit): ItemApi = retrofit.create(ItemApi::class.java)

    @Provides
    @Singleton
    @Named("refresh")
    fun provideRefreshAuthApi(@Named("refresh") retrofit: Retrofit): AuthApi =
        retrofit.create(AuthApi::class.java)
}
