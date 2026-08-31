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
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.core.user.UserRepository
import net.pollyspeople.storagelabels.data.dto.BoxRequest
import net.pollyspeople.storagelabels.navigation.Route
import javax.inject.Inject

data class BoxEditState(
    val code: String = "",
    val name: String = "",
    val description: String = "",
    val imageUrl: String? = null,
    val imageMetadataId: String? = null,
    val codeColorPattern: String = "",
    val showImages: Boolean = true,
    val isNew: Boolean = true,
    val loading: Boolean = false,
    val saving: Boolean = false,
    val error: String? = null,
    val fieldErrors: Map<String, String> = emptyMap(),
)

@HiltViewModel
class BoxEditViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val boxes: BoxRepository,
    userRepository: UserRepository,
) : ViewModel() {

    private val route = savedStateHandle.toRoute<Route.BoxEdit>()

    private val _state = MutableStateFlow(BoxEditState(isNew = route.boxId == null))
    val state: StateFlow<BoxEditState> = _state.asStateFlow()

    init {
        route.boxId?.let { load(it) }
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

    private fun load(boxId: String) {
        _state.update { it.copy(loading = true) }
        viewModelScope.launch {
            when (val result = boxes.get(boxId)) {
                is ApiResult.Success -> _state.update {
                    it.copy(
                        code = result.value.code,
                        name = result.value.name,
                        description = result.value.description.orEmpty(),
                        imageUrl = result.value.imageUrl,
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

    fun onCodeChange(value: String) = _state.update {
        it.copy(code = value, fieldErrors = it.fieldErrors - FIELD_CODE, error = null)
    }

    fun onNameChange(value: String) = _state.update {
        it.copy(name = value, fieldErrors = it.fieldErrors - FIELD_NAME, error = null)
    }

    fun onDescriptionChange(value: String) = _state.update { it.copy(description = value) }

    fun onImageSelected(imageUrl: String?, imageMetadataId: String?) = _state.update {
        it.copy(imageUrl = imageUrl, imageMetadataId = imageMetadataId)
    }

    fun save(onSaved: (String) -> Unit) {
        val current = _state.value
        val errors = buildMap {
            if (current.code.isBlank()) put(FIELD_CODE, "A code is required — it's what the label carries.")
            if (current.name.isBlank()) put(FIELD_NAME, "Give the box a name.")
        }
        if (errors.isNotEmpty()) {
            _state.update { it.copy(fieldErrors = errors) }
            return
        }

        val request = BoxRequest(
            code = current.code.trim(),
            name = current.name.trim(),
            locationId = route.locationId,
            description = current.description.trim().ifBlank { null },
            imageUrl = current.imageUrl,
            imageMetadataId = current.imageMetadataId,
        )

        _state.update { it.copy(saving = true, error = null) }
        viewModelScope.launch {
            val result = route.boxId
                ?.let { boxes.update(it, request) }
                ?: boxes.create(request)

            when (result) {
                is ApiResult.Success -> onSaved(
                    if (route.boxId == null) "Added ${result.value.name}." else "Saved ${result.value.name}.",
                )
                is ApiResult.Failure -> _state.update {
                    it.copy(saving = false, error = result.error.userMessage())
                }
            }
        }
    }

    companion object {
        const val FIELD_CODE = "code"
        const val FIELD_NAME = "name"
    }
}
