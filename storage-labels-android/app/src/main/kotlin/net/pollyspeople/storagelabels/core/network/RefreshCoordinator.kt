package net.pollyspeople.storagelabels.core.network

import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import net.pollyspeople.storagelabels.core.auth.SessionEvents
import net.pollyspeople.storagelabels.core.auth.TokenStore
import net.pollyspeople.storagelabels.data.api.AuthApi
import javax.inject.Inject
import javax.inject.Named
import javax.inject.Singleton

/**
 * Single-flight token refresh.
 *
 * Called from OkHttp's authenticator, which is a blocking callback on a network thread —
 * hence [runBlocking]. The mutex means ten parallel 401s produce one refresh call, and the
 * nine that queue behind it see the token another thread already fetched instead of spending
 * the (single-use) refresh cookie again.
 */
@Singleton
class RefreshCoordinator @Inject constructor(
    private val tokenStore: TokenStore,
    @Named("refresh") private val authApi: AuthApi,
    private val cookieJar: PersistentCookieJar,
    private val sessionEvents: SessionEvents,
) {
    private val mutex = Mutex()

    /**
     * @param staleToken the token that just produced a 401, if any. When it no longer matches
     * what's stored, another thread already refreshed and that newer token is returned as-is.
     * @return a usable access token, or null when the session is gone for good.
     */
    fun refresh(staleToken: String?): String? = runBlocking {
        mutex.withLock {
            val current = tokenStore.accessToken
            if (!current.isNullOrBlank() && current != staleToken) {
                return@withLock current
            }

            val result = runCatching { authApi.refresh() }
            result.fold(
                onSuccess = { authResult ->
                    tokenStore.accessToken = authResult.token
                    authResult.token
                },
                onFailure = {
                    // Refresh cookie rejected or absent: the session is over. Clearing here
                    // means the next UI read sees a signed-out session, and the event moves
                    // the UI to the sign-in screen with an explanation.
                    tokenStore.clear()
                    cookieJar.clear()
                    sessionEvents.notifyExpired()
                    null
                },
            )
        }
    }
}
