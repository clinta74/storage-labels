package net.pollyspeople.storagelabels.feature.locations

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.inventory.LocationRepository
import net.pollyspeople.storagelabels.core.result.ApiError
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.data.dto.AccessLevel
import net.pollyspeople.storagelabels.data.dto.LocationUser
import net.pollyspeople.storagelabels.navigation.Route
import javax.inject.Inject

data class LocationUsersState(
    val users: List<LocationUser> = emptyList(),
    val email: String = "",
    val accessLevel: AccessLevel = AccessLevel.Edit,
    val loading: Boolean = true,
    val busy: Boolean = false,
    val error: String? = null,
    /** The API only shares with people who already have an account. */
    val userNotFound: Boolean = false,
)

@HiltViewModel
class LocationUsersViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val locations: LocationRepository,
) : ViewModel() {

    private val locationId: Long = savedStateHandle.toRoute<Route.LocationUsers>().locationId

    private val _state = MutableStateFlow(LocationUsersState())
    val state: StateFlow<LocationUsersState> = _state.asStateFlow()

    init {
        refresh()
    }

    fun refresh() {
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            when (val result = locations.users(locationId)) {
                is ApiResult.Success -> _state.update {
                    it.copy(users = result.value, loading = false)
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun onEmailChange(value: String) = _state.update {
        it.copy(email = value, error = null, userNotFound = false)
    }

    fun onAccessLevelChange(value: AccessLevel) = _state.update { it.copy(accessLevel = value) }

    fun addUser(onDone: (String) -> Unit) {
        val current = _state.value
        val email = current.email.trim()
        if (!email.contains("@") || !email.substringAfter("@").contains(".")) {
            _state.update { it.copy(error = "Enter a valid email address.") }
            return
        }

        _state.update { it.copy(busy = true, error = null) }
        viewModelScope.launch {
            when (val result = locations.addUser(locationId, email, current.accessLevel)) {
                is ApiResult.Success -> {
                    _state.update { it.copy(busy = false, email = "", accessLevel = AccessLevel.Edit) }
                    onDone("Shared with $email.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(
                        busy = false,
                        // 404 here means no account with that address, which is worth saying
                        // plainly rather than as a generic failure.
                        userNotFound = result.error is ApiError.NotFound,
                        error = if (result.error is ApiError.NotFound) {
                            "No account uses $email. They need to register first."
                        } else {
                            result.error.userMessage()
                        },
                    )
                }
            }
        }
    }

    fun changeAccess(user: LocationUser, accessLevel: AccessLevel, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = locations.updateUserAccess(locationId, user.userId, accessLevel)) {
                is ApiResult.Success -> {
                    onDone("${user.displayName} is now ${accessLevel.name}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    fun removeUser(user: LocationUser, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = locations.removeUser(locationId, user.userId)) {
                is ApiResult.Success -> {
                    onDone("Removed ${user.displayName}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }
}
