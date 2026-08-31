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

/** True for the endpoints that must never trigger a refresh attempt of their own. */
internal fun HttpUrl.isAuthEndpoint(): Boolean =
    pathSegments.contains("auth")
