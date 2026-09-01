package net.pollyspeople.storagelabels.feature.locations

import androidx.lifecycle.SavedStateHandle
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.navigation.toRoute
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.inventory.BoxRepository
import net.pollyspeople.storagelabels.core.inventory.ItemRepository
import net.pollyspeople.storagelabels.core.inventory.LocationRepository
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.core.user.UserRepository
import net.pollyspeople.storagelabels.data.dto.Box
import net.pollyspeople.storagelabels.data.dto.StorageLocation
import net.pollyspeople.storagelabels.navigation.Route
import javax.inject.Inject

data class LocationDetailState(
    val location: StorageLocation? = null,
    val boxes: List<Box> = emptyList(),
    val itemCounts: Map<String, Int> = emptyMap(),
    val codeColorPattern: String = "",
    val showImages: Boolean = true,
    val loading: Boolean = true,
    val error: String? = null,
)

@HiltViewModel
class LocationDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val locations: LocationRepository,
    private val boxes: BoxRepository,
    private val items: ItemRepository,
    userRepository: UserRepository,
) : ViewModel() {

    private val locationId: Long = savedStateHandle.toRoute<Route.LocationDetail>().locationId

    private val _state = MutableStateFlow(LocationDetailState())
    val state: StateFlow<LocationDetailState> = _state.asStateFlow()

    init {
        viewModelScope.launch {
            userRepository.preferences.collect { preferences ->
                _state.update {
                    it.copy(
                        codeColorPattern = preferences?.codeColorPattern.orEmpty(),
                        showImages = preferences?.showImages ?: true,
                    )
                }
            }
        }
    }

    fun refresh() {
        _state.update { it.copy(loading = it.boxes.isEmpty(), error = null) }
        viewModelScope.launch {
            // Independent reads, so one wall-clock wait rather than two.
            val (locationResult, boxesResult) = coroutineScope {
                val location = async { locations.get(locationId) }
                val boxList = async { boxes.listByLocation(locationId) }
                location.await() to boxList.await()
            }

            val error = (locationResult as? ApiResult.Failure)?.error
                ?: (boxesResult as? ApiResult.Failure)?.error

            if (error != null) {
                _state.update { it.copy(loading = false, error = error.userMessage()) }
                return@launch
            }

            val loadedBoxes = (boxesResult as ApiResult.Success).value
            _state.update {
                it.copy(
                    location = (locationResult as ApiResult.Success).value,
                    boxes = loadedBoxes,
                    loading = false,
                )
            }

            loadItemCounts(loadedBoxes)
        }
    }

    /**
     * The API exposes no per-box item count, so the web app fetches each box's items to show
     * a badge. Same here, but in parallel and after the list has already rendered — a count
     * that fails to load simply doesn't appear.
     */
    private suspend fun loadItemCounts(boxes: List<Box>) {
        val counts = coroutineScope {
            boxes.map { box ->
                async { box.boxId to (items.listByBox(box.boxId) as? ApiResult.Success)?.value?.size }
            }.awaitAll()
        }

        _state.update { current ->
            current.copy(
                itemCounts = counts.mapNotNull { (id, count) -> count?.let { id to it } }.toMap(),
            )
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }
}
