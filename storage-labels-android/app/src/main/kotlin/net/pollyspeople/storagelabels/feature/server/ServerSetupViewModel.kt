package net.pollyspeople.storagelabels.feature.server

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.auth.AuthRepository
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.settings.ServerSettings
import net.pollyspeople.storagelabels.core.ui.userMessage
import javax.inject.Inject

data class ServerSetupState(
    val address: String = "",
    val allowCleartext: Boolean = false,
    val checking: Boolean = false,
    val error: String? = null,
)

@HiltViewModel
class ServerSetupViewModel @Inject constructor(
    private val authRepository: AuthRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(ServerSetupState())
    val state: StateFlow<ServerSetupState> = _state.asStateFlow()

    fun onAddressChange(value: String) {
        _state.update { it.copy(address = value, error = null) }
    }

    fun onAllowCleartextChange(value: Boolean) {
        _state.update { it.copy(allowCleartext = value, error = null) }
    }

    /**
     * Validates the address by asking the server for its auth config — the same call the web
     * client makes on load. Only a server that answers gets saved, so a typo is caught here
     * rather than surfacing as a broken login screen.
     */
    fun connect() {
        val current = _state.value
        val normalized = ServerSettings.normalizeUrl(current.address)
        if (normalized == null) {
            _state.update { it.copy(error = "That doesn't look like a server address.") }
            return
        }
        if (normalized.startsWith("http://") && !current.allowCleartext) {
            _state.update {
                it.copy(error = "This address uses plain HTTP. Turn on \"Allow plain HTTP\" to use it.")
            }
            return
        }

        _state.update { it.copy(checking = true, error = null) }

        viewModelScope.launch {
            when (val result = authRepository.probeServer(normalized)) {
                is ApiResult.Success -> {
                    // Saving the address flips the session state, which moves the UI on.
                    authRepository.setServer(normalized, current.allowCleartext)
                }

                is ApiResult.Failure -> _state.update {
                    it.copy(checking = false, error = result.error.userMessage())
                }
            }
        }
    }
}
