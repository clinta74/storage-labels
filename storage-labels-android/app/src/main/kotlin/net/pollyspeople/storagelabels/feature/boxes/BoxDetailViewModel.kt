package net.pollyspeople.storagelabels.feature.boxes

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
import net.pollyspeople.storagelabels.core.inventory.BoxRepository
import net.pollyspeople.storagelabels.core.inventory.ItemRepository
import net.pollyspeople.storagelabels.core.inventory.LocationRepository
import net.pollyspeople.storagelabels.core.result.ApiError
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.core.user.UserRepository
import net.pollyspeople.storagelabels.data.dto.Box
import net.pollyspeople.storagelabels.data.dto.Item
import net.pollyspeople.storagelabels.data.dto.StorageLocation
import net.pollyspeople.storagelabels.navigation.Route
import javax.inject.Inject

data class BoxDetailState(
    val box: Box? = null,
    val items: List<Item> = emptyList(),
    val codeColorPattern: String = "",
    val showImages: Boolean = true,
    val canEdit: Boolean = false,
    val moveTargets: List<StorageLocation> = emptyList(),
    val loading: Boolean = true,
    val error: String? = null,
)

@HiltViewModel
class BoxDetailViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val boxes: BoxRepository,
    private val items: ItemRepository,
    private val locations: LocationRepository,
    userRepository: UserRepository,
) : ViewModel() {

    private val route = savedStateHandle.toRoute<Route.BoxDetail>()

    private val _state = MutableStateFlow(BoxDetailState())
    val state: StateFlow<BoxDetailState> = _state.asStateFlow()

    init {
        refresh()
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
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            val boxResult = boxes.get(route.boxId)
            if (boxResult is ApiResult.Failure) {
                _state.update { it.copy(loading = false, error = boxResult.error.userMessage()) }
                return@launch
            }

            val itemsResult = items.listByBox(route.boxId)
            val location = locations.get(route.locationId)

            _state.update {
                it.copy(
                    box = (boxResult as ApiResult.Success).value,
                    items = (itemsResult as? ApiResult.Success)?.value.orEmpty(),
                    canEdit = (location as? ApiResult.Success)?.value?.accessLevel?.canEdit ?: false,
                    loading = false,
                    error = (itemsResult as? ApiResult.Failure)?.error?.userMessage(),
                )
            }
        }
    }

    fun deleteItem(item: Item, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = items.delete(item.itemId)) {
                is ApiResult.Success -> {
                    onDone("Deleted ${item.name}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    fun deleteBox(force: Boolean, onDone: (String) -> Unit) {
        val box = _state.value.box ?: return
        viewModelScope.launch {
            when (val result = boxes.delete(box.boxId, force)) {
                is ApiResult.Success -> onDone("Deleted ${box.name}.")
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    /** Loads the locations this box could move to — everywhere but where it already is. */
    fun loadMoveTargets() {
        viewModelScope.launch {
            when (val result = locations.list()) {
                is ApiResult.Success -> _state.update { current ->
                    current.copy(
                        moveTargets = result.value.filter {
                            it.locationId != route.locationId && it.accessLevel.canEdit
                        },
                    )
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    fun moveBox(destinationLocationId: Long, onMoved: (Long, String) -> Unit) {
        val box = _state.value.box ?: return
        viewModelScope.launch {
            when (val result = boxes.move(box.boxId, destinationLocationId)) {
                is ApiResult.Success -> onMoved(destinationLocationId, "Moved ${box.name}.")
                is ApiResult.Failure -> _state.update {
                    it.copy(
                        // The API answers 400 when the destination isn't yours to write to.
                        error = if (result.error is ApiError.Validation) {
                            "You need edit access to the location you're moving this box to."
                        } else {
                            result.error.userMessage()
                        },
                    )
                }
            }
        }
    }

    fun clearError() = _state.update { it.copy(error = null) }
}
