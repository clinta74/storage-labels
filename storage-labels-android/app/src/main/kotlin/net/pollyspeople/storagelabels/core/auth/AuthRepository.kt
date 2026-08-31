package net.pollyspeople.storagelabels.core.auth

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.network.PersistentCookieJar
import net.pollyspeople.storagelabels.core.network.ServerUrlProvider
import net.pollyspeople.storagelabels.core.result.ApiError
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.core.settings.ServerSettings
import net.pollyspeople.storagelabels.core.user.UserRepository
import net.pollyspeople.storagelabels.data.api.AuthApi
import net.pollyspeople.storagelabels.data.dto.AuthConfigResponse
import net.pollyspeople.storagelabels.data.dto.AuthMode
import net.pollyspeople.storagelabels.data.dto.LoginRequest
import net.pollyspeople.storagelabels.data.dto.RegisterRequest
import net.pollyspeople.storagelabels.data.dto.UserInfoResponse
import javax.inject.Inject
import javax.inject.Singleton

/**
 * What the app knows about the current session. Mirrors the states the web client's
 * AuthProvider moves through, including the "None" auth mode used on trusted networks.
 */
sealed interface SessionState {
    /** Still reading stored settings and probing the server. */
    data object Loading : SessionState

    /** First run, or the server address was cleared. */
    data object NoServer : SessionState

    /** Server reachable, mode is Local, nobody signed in. */
    data class SignedOut(val config: AuthConfigResponse, val notice: String? = null) : SessionState

    data class SignedIn(
        val user: UserInfoResponse,
        val permissions: List<String>,
        val authMode: AuthMode,
    ) : SessionState

    /** The configured address didn't answer as a Storage Labels API. */
    data class ServerUnreachable(val baseUrl: String, val error: ApiError) : SessionState
}

@Singleton
class AuthRepository @Inject constructor(
    private val authApi: AuthApi,
    private val tokenStore: TokenStore,
    private val cookieJar: PersistentCookieJar,
    private val serverSettings: ServerSettings,
    private val serverUrlProvider: ServerUrlProvider,
    private val userRepository: UserRepository,
    private val sessionEvents: SessionEvents,
) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    private val _state = MutableStateFlow<SessionState>(SessionState.Loading)
    val state: StateFlow<SessionState> = _state.asStateFlow()

    init {
        scope.launch {
            serverSettings.config.collect { config ->
                serverUrlProvider.update(config.baseUrl)
                if (config.isConfigured) bootstrap() else _state.value = SessionState.NoServer
            }
        }
        scope.launch {
            sessionEvents.expired.collect {
                signedOut("Your session expired. Sign in again.")
            }
        }
    }

    /** Moves to the sign-in screen, keeping the server configured. */
    private suspend fun signedOut(notice: String?) {
        clearLocalSession()
        val config = apiCall { authApi.getConfig() }
        _state.value = when (config) {
            is ApiResult.Success -> SessionState.SignedOut(config.value, notice)
            is ApiResult.Failure -> SessionState.ServerUnreachable(
                baseUrl = serverUrlProvider.current().orEmpty(),
                error = config.error,
            )
        }
    }

    /**
     * Decides the session state from scratch: read the server's auth config, then either walk
     * straight in (mode None), restore the stored token, or fall back to signed out.
     */
    suspend fun bootstrap() {
        _state.value = SessionState.Loading

        when (val config = apiCall { authApi.getConfig() }) {
            is ApiResult.Failure -> {
                _state.value = SessionState.ServerUnreachable(
                    baseUrl = serverUrlProvider.current().orEmpty(),
                    error = config.error,
                )
            }

            is ApiResult.Success -> {
                if (config.value.mode == AuthMode.None) {
                    // No-auth mode: the API grants every permission server-side.
                    _state.value = SessionState.SignedIn(
                        user = noAuthUser(),
                        permissions = ALL_PERMISSIONS,
                        authMode = AuthMode.None,
                    )
                    return
                }

                val restored = restoreSession()
                _state.value = restored ?: SessionState.SignedOut(config.value)
            }
        }
    }

    /** Sets the signed-in state and pulls the preferences that drive the theme. */
    private suspend fun enterSignedIn(user: UserInfoResponse): SessionState.SignedIn {
        val state = signedIn(user)
        userRepository.load()
        return state
    }

    private suspend fun restoreSession(): SessionState.SignedIn? {
        if (tokenStore.accessToken.isNullOrBlank()) return null

        // A 401 here is handled by the authenticator, which refreshes and replays once.
        return when (val me = apiCall { authApi.getCurrentUser() }) {
            is ApiResult.Success -> enterSignedIn(me.value)
            is ApiResult.Failure -> {
                if (me.error is ApiError.Unauthorized) clearLocalSession()
                null
            }
        }
    }

    suspend fun login(usernameOrEmail: String, password: String): ApiResult<Unit> =
        when (val result = apiCall { authApi.login(LoginRequest(usernameOrEmail, password)) }) {
            is ApiResult.Failure -> result
            is ApiResult.Success -> {
                tokenStore.accessToken = result.value.token
                _state.value = enterSignedIn(result.value.user)
                ApiResult.Success(Unit)
            }
        }

    suspend fun register(
        email: String,
        username: String,
        password: String,
        firstName: String,
        lastName: String,
    ): ApiResult<Unit> {
        val request = RegisterRequest(email, username, password, firstName, lastName)
        return when (val result = apiCall { authApi.register(request) }) {
            is ApiResult.Failure -> result
            // The web client signs in after registering; the API already returns a session.
            is ApiResult.Success -> {
                tokenStore.accessToken = result.value.token
                _state.value = enterSignedIn(result.value.user)
                ApiResult.Success(Unit)
            }
        }
    }

    suspend fun logout(notice: String? = null) {
        apiCall { authApi.logout() }
        signedOut(notice)
    }

    suspend fun setServer(baseUrl: String, allowCleartext: Boolean) {
        clearLocalSession()
        serverSettings.setServer(baseUrl, allowCleartext)
    }

    suspend fun clearServer() {
        clearLocalSession()
        serverUrlProvider.update(null)
        serverSettings.clear()
    }

    /** Probe an address before committing to it, so setup fails on the setup screen. */
    suspend fun probeServer(baseUrl: String): ApiResult<AuthConfigResponse> {
        val previous = serverUrlProvider.current()
        serverUrlProvider.update(baseUrl)
        val result = apiCall { authApi.getConfig() }
        if (result is ApiResult.Failure) serverUrlProvider.update(previous)
        return result
    }

    private fun clearLocalSession() {
        tokenStore.clear()
        cookieJar.clear()
        userRepository.reset()
    }

    private fun signedIn(user: UserInfoResponse): SessionState.SignedIn {
        // Permissions live in the JWT; fall back to whatever /auth/me reported.
        val fromToken = tokenStore.accessToken?.let(JwtClaims::permissions).orEmpty()
        return SessionState.SignedIn(
            user = user,
            permissions = fromToken.ifEmpty { user.permissions },
            authMode = AuthMode.Local,
        )
    }

    private fun noAuthUser() = UserInfoResponse(
        userId = "",
        username = "",
        email = "",
        fullName = null,
        profilePictureUrl = null,
        roles = emptyList(),
        permissions = ALL_PERMISSIONS,
        isActive = true,
    )

    private companion object {
        val ALL_PERMISSIONS = listOf(
            "read:user",
            "write:user",
            "read:common-locations",
            "write:common-locations",
            "read:encryption-keys",
            "write:encryption-keys",
        )
    }
}
