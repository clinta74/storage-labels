package net.pollyspeople.storagelabels.feature

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.auth.AuthRepository
import net.pollyspeople.storagelabels.core.auth.SessionState
import net.pollyspeople.storagelabels.core.network.ServerUrlProvider
import javax.inject.Inject

@HiltViewModel
class SessionViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val serverUrlProvider: ServerUrlProvider,
) : ViewModel() {

    val state: StateFlow<SessionState> = authRepository.state

    fun serverAddress(): String = serverUrlProvider.current().orEmpty()

    fun retry() {
        viewModelScope.launch { authRepository.bootstrap() }
    }

    fun changeServer() {
        viewModelScope.launch { authRepository.clearServer() }
    }

    fun signOut() {
        viewModelScope.launch { authRepository.logout() }
    }
}
