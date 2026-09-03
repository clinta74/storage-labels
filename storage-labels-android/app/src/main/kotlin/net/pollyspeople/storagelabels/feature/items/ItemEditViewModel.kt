package net.pollyspeople.storagelabels.feature.items

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
import net.pollyspeople.storagelabels.core.inventory.ItemRepository
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.core.user.UserRepository
import net.pollyspeople.storagelabels.data.dto.ItemRequest
import net.pollyspeople.storagelabels.navigation.Route
import javax.inject.Inject

data class ItemEditState(
    val name: String = "",
    val description: String = "",
    val imageUrl: String? = null,
    val imageMetadataId: String? = null,
    val showImages: Boolean = true,
    val isNew: Boolean = true,
    val loading: Boolean = false,
    val saving: Boolean = false,
    val error: String? = null,
    val nameError: String? = null,
)

@HiltViewModel
class ItemEditViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val items: ItemRepository,
    userRepository: UserRepository,
) : ViewModel() {

    private val route = savedStateHandle.toRoute<Route.ItemEdit>()

    private val _state = MutableStateFlow(ItemEditState(isNew = route.itemId == null))
    val state: StateFlow<ItemEditState> = _state.asStateFlow()

    init {
        route.itemId?.let { load(it) }
        viewModelScope.launch {
            userRepository.preferences.collect { preferences ->
                _state.update { it.copy(showImages = preferences?.showImages ?: true) }
            }
        }
    }

    private fun load(itemId: String) {
        _state.update { it.copy(loading = true) }
        viewModelScope.launch {
            when (val result = items.get(itemId)) {
                is ApiResult.Success -> _state.update {
                    it.copy(
                        name = result.value.name,
                        description = result.value.description.orEmpty(),
                        imageUrl = result.value.photoUrl,
                        imageMetadataId = result.value.imageMetadataId,
                        loading = false,
                    )
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun onNameChange(value: String) = _state.update {
        it.copy(name = value, nameError = null, error = null)
    }

    fun onDescriptionChange(value: String) = _state.update { it.copy(description = value) }

    fun onImageSelected(imageUrl: String?, imageMetadataId: String?) = _state.update {
        it.copy(imageUrl = imageUrl, imageMetadataId = imageMetadataId)
    }

    fun save(onSaved: (String) -> Unit) {
        val current = _state.value
        if (current.name.isBlank()) {
            _state.update { it.copy(nameError = "Give the item a name.") }
            return
        }

        val request = ItemRequest(
            boxId = route.boxId,
            name = current.name.trim(),
            description = current.description.trim().ifBlank { null },
            imageUrl = current.imageUrl,
            imageMetadataId = current.imageMetadataId,
        )

        _state.update { it.copy(saving = true, error = null) }
        viewModelScope.launch {
            val result = route.itemId
                ?.let { items.update(it, request) }
                ?: items.create(request)

            when (result) {
                is ApiResult.Success -> onSaved(
                    if (route.itemId == null) "Added ${result.value.name}." else "Saved ${result.value.name}.",
                )
                is ApiResult.Failure -> _state.update {
                    it.copy(saving = false, error = result.error.userMessage())
                }
            }
        }
    }
}
