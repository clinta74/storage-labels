package net.pollyspeople.storagelabels.core.auth

import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Lets the network layer tell the session layer that a refresh failed, without the two
 * depending on each other. The web client does the equivalent by navigating to
 * /login?notice=session-expired from its axios interceptor.
 */
@Singleton
class SessionEvents @Inject constructor() {

    private val _expired = MutableSharedFlow<Unit>(
        replay = 0,
        extraBufferCapacity = 1,
        onBufferOverflow = BufferOverflow.DROP_OLDEST,
    )
    val expired: SharedFlow<Unit> = _expired.asSharedFlow()

    /** Called from OkHttp's authenticator thread when the refresh cookie is spent. */
    fun notifyExpired() {
        _expired.tryEmit(Unit)
    }
}
