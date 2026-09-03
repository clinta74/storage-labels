package net.pollyspeople.storagelabels.core.network

import net.pollyspeople.storagelabels.core.auth.TokenStore
import okhttp3.HttpUrl
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import okhttp3.Interceptor
import okhttp3.Response
import java.io.IOException
import javax.inject.Inject
import javax.inject.Singleton

/** Raised when a call is attempted before the user has configured a server. */
class NoServerConfiguredException : IOException("No server has been configured yet.")

/**
 * Retrofit needs a base URL at construction time, but the real one is chosen by the user and
 * can change. Retrofit is therefore built against a placeholder and every request is rewritten
 * here onto the configured server, under its /api prefix.
 */
@Singleton
class HostSelectionInterceptor @Inject constructor(
    private val serverUrlProvider: ServerUrlProvider,
) : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val configured = serverUrlProvider.current() ?: throw NoServerConfiguredException()
        val base = configured.toHttpUrlOrNull() ?: throw NoServerConfiguredException()

        return chain.proceed(request.newBuilder().url(resolve(base, request.url)).build())
    }

    private fun resolve(base: HttpUrl, requested: HttpUrl): HttpUrl {
        val builder = base.newBuilder().addPathSegment("api")
        requested.pathSegments.filter { it.isNotEmpty() }.forEach(builder::addPathSegment)
        requested.encodedQuery?.let(builder::encodedQuery)
        return builder.build()
    }
}

/**
 * Reads the configured base URL without suspending — interceptors run on network threads.
 * Backed by a value the session layer keeps current.
 */
@Singleton
class ServerUrlProvider @Inject constructor() {
    @Volatile
    private var baseUrl: String? = null

    fun current(): String? = baseUrl

    fun update(url: String?) {
        baseUrl = url
    }
}

/**
 * Attaches the bearer token, mirroring the web client: in "None" auth mode the API expects no
 * token at all, and the auth endpoints are called without one.
 */
@Singleton
class AuthInterceptor @Inject constructor(
    private val tokenStore: TokenStore,
) : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request()
        val token = tokenStore.accessToken

        if (token.isNullOrBlank() || request.header("Authorization") != null) {
            return chain.proceed(request)
        }

        return chain.proceed(
            request.newBuilder().header("Authorization", "Bearer $token").build(),
        )
    }
}

/**
 * The calls that establish or end a session. A 401 from one of these is the answer -- bad
 * credentials, a dead refresh cookie -- so refreshing and replaying would be nonsense.
 *
 * Everything else under /auth is an ordinary authenticated call. `auth/me` especially: it is
 * how a stored session is restored at launch, and treating it as exempt meant an expired
 * access token signed the person out instead of being refreshed from the cookie sitting on
 * disk. Nothing here is what stops a refresh recursing -- the refresh call uses a client with
 * no authenticator, and a replay happens at most once.
 */
internal fun HttpUrl.isSessionEndpoint(): Boolean {
    val auth = pathSegments.indexOf("auth")
    if (auth < 0) return false
    return pathSegments.getOrNull(auth + 1) in SESSION_PATHS
}

private val SESSION_PATHS = setOf(
    "login",
    "register",
    "refresh",
    "logout",
    "config",
    "forgot-password",
    "reset-password",
    "confirm-email",
)
