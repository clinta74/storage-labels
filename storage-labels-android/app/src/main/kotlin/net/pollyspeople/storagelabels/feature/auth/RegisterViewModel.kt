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

data class RegisterState(
    val email: String = "",
    val username: String = "",
    val firstName: String = "",
    val lastName: String = "",
    val password: String = "",
    val confirmPassword: String = "",
    val submitting: Boolean = false,
    val fieldErrors: Map<String, String> = emptyMap(),
    val error: String? = null,
)

@HiltViewModel
class RegisterViewModel @Inject constructor(
    private val authRepository: AuthRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(RegisterState())
    val state: StateFlow<RegisterState> = _state.asStateFlow()

    fun onEmailChange(value: String) = update { it.copy(email = value) }
    fun onUsernameChange(value: String) = update { it.copy(username = value) }
    fun onFirstNameChange(value: String) = update { it.copy(firstName = value) }
    fun onLastNameChange(value: String) = update { it.copy(lastName = value) }
    fun onPasswordChange(value: String) = update { it.copy(password = value) }
    fun onConfirmPasswordChange(value: String) = update { it.copy(confirmPassword = value) }

    private fun update(block: (RegisterState) -> RegisterState) {
        _state.update { block(it).copy(fieldErrors = emptyMap(), error = null) }
    }

    fun submit() {
        val current = _state.value
        val errors = validate(current)
        if (errors.isNotEmpty()) {
            _state.update { it.copy(fieldErrors = errors) }
            return
        }

        _state.update { it.copy(submitting = true, error = null) }

        viewModelScope.launch {
            val result = authRepository.register(
                email = current.email.trim(),
                username = current.username.trim(),
                password = current.password,
                firstName = current.firstName.trim(),
                lastName = current.lastName.trim(),
            )
            when (result) {
                // The API signs the new account in, so the session state moves the UI on.
                is ApiResult.Success -> Unit
                is ApiResult.Failure -> _state.update {
                    it.copy(submitting = false, error = result.error.userMessage())
                }
            }
        }
    }

    companion object {
        /**
         * Client-side checks only catch the obvious. The server owns password policy
         * (length, character classes are configurable per deployment) and reports violations
         * as a 400, which surfaces as [RegisterState.error].
         */
        fun validate(state: RegisterState): Map<String, String> = buildMap {
            if (state.email.isBlank()) {
                put(FIELD_EMAIL, "Email is required.")
            } else if (!state.email.contains("@") || !state.email.substringAfter("@").contains(".")) {
                put(FIELD_EMAIL, "Enter a valid email address.")
            }
            if (state.username.isBlank()) put(FIELD_USERNAME, "Username is required.")
            if (state.firstName.isBlank()) put(FIELD_FIRST_NAME, "First name is required.")
            if (state.lastName.isBlank()) put(FIELD_LAST_NAME, "Last name is required.")
            if (state.password.isBlank()) {
                put(FIELD_PASSWORD, "Password is required.")
            }
            if (state.confirmPassword != state.password) {
                put(FIELD_CONFIRM, "Passwords don't match.")
            }
        }

        const val FIELD_EMAIL = "email"
        const val FIELD_USERNAME = "username"
        const val FIELD_FIRST_NAME = "firstName"
        const val FIELD_LAST_NAME = "lastName"
        const val FIELD_PASSWORD = "password"
        const val FIELD_CONFIRM = "confirmPassword"
    }
}
