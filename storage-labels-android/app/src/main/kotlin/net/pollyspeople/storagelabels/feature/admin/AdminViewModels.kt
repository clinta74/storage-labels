package net.pollyspeople.storagelabels.feature.admin

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.admin.RotationProgressStream
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.data.api.AdminUserApi
import net.pollyspeople.storagelabels.data.api.CommonLocationApi
import net.pollyspeople.storagelabels.data.api.EncryptionKeyApi
import net.pollyspeople.storagelabels.data.dto.AdminResetPasswordRequest
import net.pollyspeople.storagelabels.data.dto.CommonLocation
import net.pollyspeople.storagelabels.data.dto.CommonLocationRequest
import net.pollyspeople.storagelabels.data.dto.CreateEncryptionKeyRequest
import net.pollyspeople.storagelabels.data.dto.EncryptionKey
import net.pollyspeople.storagelabels.data.dto.EncryptionKeyRotation
import net.pollyspeople.storagelabels.data.dto.EncryptionKeyStats
import net.pollyspeople.storagelabels.data.dto.RotationProgress
import net.pollyspeople.storagelabels.data.dto.StartRotationRequest
import net.pollyspeople.storagelabels.data.dto.UpdateUserRoleRequest
import net.pollyspeople.storagelabels.data.dto.UserWithRoles
import javax.inject.Inject

data class CommonLocationsState(
    val locations: List<CommonLocation> = emptyList(),
    val loading: Boolean = true,
    val error: String? = null,
)

@HiltViewModel
class CommonLocationsViewModel @Inject constructor(
    private val api: CommonLocationApi,
) : ViewModel() {

    private val _state = MutableStateFlow(CommonLocationsState())
    val state: StateFlow<CommonLocationsState> = _state.asStateFlow()

    fun refresh() {
        _state.update { it.copy(loading = it.locations.isEmpty(), error = null) }
        viewModelScope.launch {
            when (val result = apiCall { api.getCommonLocations() }) {
                is ApiResult.Success -> _state.update {
                    it.copy(locations = result.value, loading = false)
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun create(name: String, onDone: (String) -> Unit) {
        if (name.isBlank()) return
        viewModelScope.launch {
            when (val result = apiCall { api.createCommonLocation(CommonLocationRequest(name.trim())) }) {
                is ApiResult.Success -> {
                    onDone("Added ${result.value.name}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    fun delete(location: CommonLocation, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = apiCall { api.deleteCommonLocation(location.commonLocationId) }) {
                is ApiResult.Success -> {
                    onDone("Deleted ${location.name}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }
}

data class UserManagementState(
    val users: List<UserWithRoles> = emptyList(),
    val loading: Boolean = true,
    val busyUserId: String? = null,
    val error: String? = null,
)

@HiltViewModel
class UserManagementViewModel @Inject constructor(
    private val api: AdminUserApi,
) : ViewModel() {

    private val _state = MutableStateFlow(UserManagementState())
    val state: StateFlow<UserManagementState> = _state.asStateFlow()

    fun refresh() {
        _state.update { it.copy(loading = it.users.isEmpty(), error = null) }
        viewModelScope.launch {
            when (val result = apiCall { api.getAllUsers() }) {
                is ApiResult.Success -> _state.update { it.copy(users = result.value, loading = false) }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun changeRole(user: UserWithRoles, role: String, onDone: (String) -> Unit) {
        _state.update { it.copy(busyUserId = user.userId) }
        viewModelScope.launch {
            when (val result = apiCall { api.updateRole(user.userId, UpdateUserRoleRequest(role)) }) {
                is ApiResult.Success -> {
                    _state.update { it.copy(busyUserId = null) }
                    onDone("${user.displayName} is now $role.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(busyUserId = null, error = result.error.userMessage())
                }
            }
        }
    }

    fun resetPassword(user: UserWithRoles, newPassword: String, onDone: (String) -> Unit) {
        _state.update { it.copy(busyUserId = user.userId) }
        viewModelScope.launch {
            val request = AdminResetPasswordRequest(user.userId, newPassword)
            when (val result = apiCall { api.resetPassword(request) }) {
                is ApiResult.Success -> {
                    _state.update { it.copy(busyUserId = null) }
                    // The API invalidates their sessions, so say so rather than just "done".
                    onDone("Password reset. ${user.displayName} has been signed out everywhere.")
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(busyUserId = null, error = result.error.userMessage())
                }
            }
        }
    }

    fun deleteUser(user: UserWithRoles, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = apiCall { api.deleteUser(user.userId) }) {
                is ApiResult.Success -> {
                    onDone("Deleted ${user.displayName}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }
}

data class EncryptionKeysState(
    val keys: List<EncryptionKey> = emptyList(),
    val stats: Map<Int, EncryptionKeyStats> = emptyMap(),
    val rotations: List<EncryptionKeyRotation> = emptyList(),
    val liveProgress: RotationProgress? = null,
    val loading: Boolean = true,
    val busy: Boolean = false,
    val error: String? = null,
)

@HiltViewModel
class EncryptionKeysViewModel @Inject constructor(
    private val api: EncryptionKeyApi,
    private val progressStream: RotationProgressStream,
) : ViewModel() {

    private val _state = MutableStateFlow(EncryptionKeysState())
    val state: StateFlow<EncryptionKeysState> = _state.asStateFlow()

    private var watchJob: Job? = null

    fun refresh() {
        _state.update { it.copy(loading = it.keys.isEmpty(), error = null) }
        viewModelScope.launch {
            val keys = apiCall { api.getKeys() }
            val rotations = apiCall { api.getRotations() }

            when (keys) {
                is ApiResult.Success -> _state.update {
                    it.copy(
                        keys = keys.value.sortedByDescending(EncryptionKey::version),
                        rotations = (rotations as? ApiResult.Success)?.value.orEmpty(),
                        loading = false,
                    )
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = keys.error.userMessage())
                }
            }

            // Reattach to a rotation that's still running, so reopening the screen doesn't
            // lose the live view.
            (rotations as? ApiResult.Success)?.value
                ?.firstOrNull { rotation -> !rotation.status.isFinished }
                ?.let { watchRotation(it.id) }
        }
    }

    fun loadStats(kid: Int) {
        viewModelScope.launch {
            val result = apiCall { api.getStats(kid) }
            if (result is ApiResult.Success) {
                _state.update { it.copy(stats = it.stats + (kid to result.value)) }
            }
        }
    }

    fun createKey(description: String, onDone: (String) -> Unit) {
        _state.update { it.copy(busy = true) }
        viewModelScope.launch {
            val request = CreateEncryptionKeyRequest(description.trim().ifBlank { null })
            when (val result = apiCall { api.createKey(request) }) {
                is ApiResult.Success -> {
                    _state.update { it.copy(busy = false) }
                    onDone("Created key version ${result.value.version}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(busy = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun activate(key: EncryptionKey, autoRotate: Boolean, onDone: (String) -> Unit) {
        _state.update { it.copy(busy = true) }
        viewModelScope.launch {
            when (val result = apiCall { api.activate(key.kid, autoRotate) }) {
                is ApiResult.Success -> {
                    _state.update { it.copy(busy = false) }
                    onDone(
                        if (autoRotate) {
                            "Key ${key.version} is active. Re-encryption has started."
                        } else {
                            "Key ${key.version} is active."
                        },
                    )
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(busy = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun retire(key: EncryptionKey, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = apiCall { api.retire(key.kid) }) {
                is ApiResult.Success -> {
                    onDone("Retired key ${key.version}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    fun startRotation(fromKeyId: Int?, toKeyId: Int, onDone: (String) -> Unit) {
        _state.update { it.copy(busy = true) }
        viewModelScope.launch {
            val request = StartRotationRequest(fromKeyId = fromKeyId, toKeyId = toKeyId)
            when (val result = apiCall { api.startRotation(request) }) {
                is ApiResult.Success -> {
                    _state.update { it.copy(busy = false) }
                    onDone(
                        if (fromKeyId == null) {
                            "Encrypting images that weren't encrypted yet."
                        } else {
                            "Re-encryption started."
                        },
                    )
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(busy = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun cancelRotation(rotation: EncryptionKeyRotation, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = apiCall { api.cancelRotation(rotation.id) }) {
                is ApiResult.Success -> {
                    watchJob?.cancel()
                    onDone("Rotation cancelled.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    /** Follows a running rotation over SSE until it finishes. */
    fun watchRotation(rotationId: String) {
        watchJob?.cancel()
        watchJob = viewModelScope.launch {
            progressStream.observe(rotationId).collect { progress ->
                _state.update { it.copy(liveProgress = progress) }
                if (progress.status.isFinished) refresh()
            }
        }
    }

    override fun onCleared() {
        watchJob?.cancel()
        super.onCleared()
    }

    fun clearError() = _state.update { it.copy(error = null) }
}
