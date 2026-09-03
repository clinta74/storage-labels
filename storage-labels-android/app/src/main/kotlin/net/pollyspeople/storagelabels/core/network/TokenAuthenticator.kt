package net.pollyspeople.storagelabels.core.network

import net.pollyspeople.storagelabels.core.auth.TokenStore
import okhttp3.Authenticator
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route
import javax.inject.Inject
import javax.inject.Provider
import javax.inject.Singleton

/**
 * Refreshes the access token on a 401 and replays the request once — the same contract the
 * web client implements in its axios response interceptor, including the single-flight
 * guarantee: concurrent 401s share one refresh call rather than each firing their own.
 *
 * The refresh itself goes through [RefreshCoordinator], which uses a client with no
 * authenticator so a failing refresh cannot recurse.
 */
@Singleton
class TokenAuthenticator @Inject constructor(
    private val tokenStore: TokenStore,
    private val refreshCoordinator: Provider<RefreshCoordinator>,
) : Authenticator {

    override fun authenticate(route: Route?, response: Response): Request? {
        // Signing in, out and refreshing own their own 401s.
        if (response.request.url.isSessionEndpoint()) return null

        // Only ever retry once; OkHttp calls back with the whole chain of prior responses.
        if (response.priorResponseCount() >= 1) return null

        val tokenUsed = response.request.header("Authorization")?.removePrefix("Bearer ")?.trim()
        val refreshed = refreshCoordinator.get().refresh(tokenUsed) ?: return null

        return response.request.newBuilder()
            .header("Authorization", "Bearer $refreshed")
            .build()
    }

    private fun Response.priorResponseCount(): Int {
        var count = 0
        var prior = priorResponse
        while (prior != null) {
            count++
            prior = prior.priorResponse
        }
        return count
    }

    /** Exposed for the session layer to drop a dead session. */
    fun clearSession() = tokenStore.clear()
}
