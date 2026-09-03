package net.pollyspeople.storagelabels.feature.auth

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
import net.pollyspeople.storagelabels.core.ui.userMessage
import javax.inject.Inject

data class LoginState(
    val usernameOrEmail: String = "",
    val password: String = "",
    val submitting: Boolean = false,
    val error: String? = null,
)

@HiltViewModel
class LoginViewModel @Inject constructor(
    private val authRepository: AuthRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(LoginState())
    val state: StateFlow<LoginState> = _state.asStateFlow()

    fun onUsernameChange(value: String) = _state.update { it.copy(usernameOrEmail = value, error = null) }

    fun onPasswordChange(value: String) = _state.update { it.copy(password = value, error = null) }

    fun submit() {
        val current = _state.value
        if (current.usernameOrEmail.isBlank() || current.password.isBlank()) {
            _state.update { it.copy(error = "Enter your username or email and password.") }
            return
        }

        _state.update { it.copy(submitting = true, error = null) }

        viewModelScope.launch {
            when (val result = authRepository.login(current.usernameOrEmail.trim(), current.password)) {
                // Success flips the session state; this screen is replaced.
                is ApiResult.Success -> Unit
                is ApiResult.Failure -> _state.update {
                    it.copy(submitting = false, password = "", error = result.error.userMessage())
                }
            }
        }
    }

    fun changeServer() {
        viewModelScope.launch { authRepository.clearServer() }
    }
}
