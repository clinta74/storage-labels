package net.pollyspeople.storagelabels.feature.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.data.api.AuthApi
import net.pollyspeople.storagelabels.data.dto.ChangePasswordRequest
import javax.inject.Inject

data class ChangePasswordState(
    val currentPassword: String = "",
    val newPassword: String = "",
    val confirmPassword: String = "",
    val submitting: Boolean = false,
    val error: String? = null,
    val succeeded: Boolean = false,
)

@HiltViewModel
class ChangePasswordViewModel @Inject constructor(
    private val authApi: AuthApi,
) : ViewModel() {

    private val _state = MutableStateFlow(ChangePasswordState())
    val state: StateFlow<ChangePasswordState> = _state.asStateFlow()

    fun onCurrentChange(value: String) = _state.update {
        it.copy(currentPassword = value, error = null, succeeded = false)
    }

    fun onNewChange(value: String) = _state.update {
        it.copy(newPassword = value, error = null, succeeded = false)
    }

    fun onConfirmChange(value: String) = _state.update {
        it.copy(confirmPassword = value, error = null, succeeded = false)
    }

    fun submit() {
        val current = _state.value
        val problem = validate(current)
        if (problem != null) {
            _state.update { it.copy(error = problem) }
            return
        }

        _state.update { it.copy(submitting = true, error = null) }

        viewModelScope.launch {
            val result = apiCall {
                authApi.changePassword(
                    ChangePasswordRequest(current.currentPassword, current.newPassword),
                )
            }
            _state.value = when (result) {
                is ApiResult.Success -> ChangePasswordState(succeeded = true)
                is ApiResult.Failure -> current.copy(
                    submitting = false,
                    error = result.error.userMessage(),
                )
            }
        }
    }

    fun acknowledgeSuccess() = _state.update { it.copy(succeeded = false) }

    companion object {
        /** Password complexity is configured per deployment, so the server has the final say. */
        fun validate(state: ChangePasswordState): String? = when {
            state.currentPassword.isBlank() -> "Enter your current password."
            state.newPassword.isBlank() -> "Enter a new password."
            state.newPassword != state.confirmPassword -> "The new passwords don't match."
            state.newPassword == state.currentPassword -> "The new password must be different."
            else -> null
        }
    }
}
