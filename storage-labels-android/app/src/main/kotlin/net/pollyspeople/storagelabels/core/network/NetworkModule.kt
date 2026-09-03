package net.pollyspeople.storagelabels.core.network

import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.serialization.json.Json
import net.pollyspeople.storagelabels.BuildConfig
import net.pollyspeople.storagelabels.data.api.AdminUserApi
import net.pollyspeople.storagelabels.data.api.AuthApi
import net.pollyspeople.storagelabels.data.api.CommonLocationApi
import net.pollyspeople.storagelabels.data.api.EncryptionKeyApi
import net.pollyspeople.storagelabels.data.api.BoxApi
import net.pollyspeople.storagelabels.data.api.ImageApi
import net.pollyspeople.storagelabels.data.api.ItemApi
import net.pollyspeople.storagelabels.data.api.LabelApi
import net.pollyspeople.storagelabels.data.api.LocationApi
import net.pollyspeople.storagelabels.data.api.SearchApi
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

    /**
     * A self-hosted server is usually a few milliseconds away on the same network, so ten
     * seconds is already generous. The old thirty meant a server that swallows packets --
     * switched off, or the phone on the wrong network -- left the app sitting on a spinner
     * for half a minute with nothing to say.
     */
    private const val CONNECT_TIMEOUT_SECONDS = 10L

    /**
     * A hard ceiling on a whole call: connect, write, read and any retry in between. Without
     * it the timeouts above are per-attempt, and OkHttp retrying a second address doubles
     * them. Only the calls that carry photos are exempt.
     */
    private const val AUTH_CALL_TIMEOUT_SECONDS = 20L

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

    /**
     * Every /auth call goes through here: sign in, register, the config probe, the session
     * restore, change password and sign out. None of them carry a payload worth waiting on,
     * and all of them are the ones a person is sitting and watching, so they get a hard
     * ceiling the default client cannot use -- it has to allow four minutes for an upload.
     *
     * It still needs the bearer token and the authenticator. `auth/me` and `change-password`
     * are authenticated calls like any other: without the token they answer 401 every time,
     * which is what stopped a stored session from ever being restored. Refreshing cannot
     * recurse from here -- the refresh call has its own client, below.
     */
    @Provides
    @Singleton
    @Named("auth")
    fun provideAuthClient(
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
        .connectTimeout(CONNECT_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .writeTimeout(15, TimeUnit.SECONDS)
        .callTimeout(AUTH_CALL_TIMEOUT_SECONDS, TimeUnit.SECONDS)
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
        .connectTimeout(CONNECT_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        // Image uploads and key rotations can be slow; the web client allows four minutes.
        // Reaching the server is still bounded above, so a dead host fails in seconds and
        // only a server that answers gets to take its time.
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
        .connectTimeout(CONNECT_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .callTimeout(60, TimeUnit.SECONDS)
        .build()

    @Provides
    @Singleton
    fun provideRetrofit(client: OkHttpClient, json: Json): Retrofit = retrofit(client, json)

    @Provides
    @Singleton
    @Named("auth")
    fun provideAuthRetrofit(@Named("auth") client: OkHttpClient, json: Json): Retrofit =
        retrofit(client, json)

    @Provides
    @Singleton
    fun provideAuthApi(@Named("auth") retrofit: Retrofit): AuthApi =
        retrofit.create(AuthApi::class.java)

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
    fun provideImageApi(retrofit: Retrofit): ImageApi = retrofit.create(ImageApi::class.java)

    @Provides
    @Singleton
    fun provideSearchApi(retrofit: Retrofit): SearchApi = retrofit.create(SearchApi::class.java)

    @Provides
    @Singleton
    fun provideLabelApi(retrofit: Retrofit): LabelApi = retrofit.create(LabelApi::class.java)

    @Provides
    @Singleton
    fun provideCommonLocationApi(retrofit: Retrofit): CommonLocationApi =
        retrofit.create(CommonLocationApi::class.java)

    @Provides
    @Singleton
    fun provideAdminUserApi(retrofit: Retrofit): AdminUserApi =
        retrofit.create(AdminUserApi::class.java)

    @Provides
    @Singleton
    fun provideEncryptionKeyApi(retrofit: Retrofit): EncryptionKeyApi =
        retrofit.create(EncryptionKeyApi::class.java)

    /**
     * The refresh call and nothing else. No authenticator, so a 401 here cannot start another
     * refresh; no bearer either, because the refresh cookie is the credential and the token
     * being replaced is by definition the expired one.
     */
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
        .connectTimeout(CONNECT_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        .readTimeout(15, TimeUnit.SECONDS)
        .writeTimeout(15, TimeUnit.SECONDS)
        .callTimeout(AUTH_CALL_TIMEOUT_SECONDS, TimeUnit.SECONDS)
        .build()

    @Provides
    @Singleton
    @Named("refresh")
    fun provideRefreshRetrofit(@Named("refresh") client: OkHttpClient, json: Json): Retrofit =
        retrofit(client, json)

    @Provides
    @Singleton
    @Named("refresh")
    fun provideRefreshAuthApi(@Named("refresh") retrofit: Retrofit): AuthApi =
        retrofit.create(AuthApi::class.java)
}
