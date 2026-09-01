package net.pollyspeople.storagelabels.feature.locations

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.inventory.LocationRepository
import net.pollyspeople.storagelabels.core.result.ApiError
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.data.api.CommonLocationApi
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.data.dto.StorageLocation
import javax.inject.Inject

data class LocationsState(
    val locations: List<StorageLocation> = emptyList(),
    val loading: Boolean = true,
    val error: String? = null,
    val saving: Boolean = false,
    val message: String? = null,
    /** Shared place names offered as suggestions when naming a location. */
    val commonLocationNames: List<String> = emptyList(),
)

@HiltViewModel
class LocationsViewModel @Inject constructor(
    private val locations: LocationRepository,
    private val commonLocationApi: CommonLocationApi,
) : ViewModel() {

    private val _state = MutableStateFlow(LocationsState())
    val state: StateFlow<LocationsState> = _state.asStateFlow()

    /**
     * Common locations are suggestions, not a requirement — if the call fails (or the
     * deployment has none) naming a location still works, it just offers nothing.
     */
    private fun loadCommonLocations() {
        viewModelScope.launch {
            val result = apiCall { commonLocationApi.getCommonLocations() }
            if (result is ApiResult.Success) {
                _state.update { it.copy(commonLocationNames = result.value.map { c -> c.name }) }
            }
        }
    }

    /** Quiet once something is on screen, so returning to the list doesn't flash a spinner. */
    fun refresh() {
        loadCommonLocations()
        _state.update { it.copy(loading = it.locations.isEmpty(), error = null) }
        viewModelScope.launch {
            when (val result = locations.list()) {
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
        _state.update { it.copy(saving = true) }
        viewModelScope.launch {
            when (val result = locations.create(name.trim())) {
                is ApiResult.Success -> {
                    _state.update { it.copy(saving = false) }
                    onDone("Created ${result.value.name}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(saving = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun rename(locationId: Long, name: String, onDone: (String) -> Unit) {
        if (name.isBlank()) return
        _state.update { it.copy(saving = true) }
        viewModelScope.launch {
            when (val result = locations.rename(locationId, name.trim())) {
                is ApiResult.Success -> {
                    _state.update { it.copy(saving = false) }
                    onDone("Renamed to ${result.value.name}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(saving = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun delete(location: StorageLocation, force: Boolean, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = locations.delete(location.locationId, force)) {
                is ApiResult.Success -> {
                    onDone("Deleted ${location.name}.")
                    refresh()
                }
                // The API answers a non-forced delete of a location that still holds boxes
                // with a validation problem whose title says nothing useful. Say what to do.
                is ApiResult.Failure -> {
                    val message = if (!force && result.error is ApiError.Validation) {
                        "${location.name} still holds boxes. Tick \"Also delete the boxes it " +
                            "still holds\" to remove it anyway."
                    } else {
                        result.error.userMessage()
                    }
                    _state.update { it.copy(error = message) }
                }
            }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }
}
