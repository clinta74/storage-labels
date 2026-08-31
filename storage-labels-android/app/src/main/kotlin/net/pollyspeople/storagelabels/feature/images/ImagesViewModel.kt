package net.pollyspeople.storagelabels.feature.images

import android.net.Uri
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.images.ImageRepository
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.core.user.UserRepository
import net.pollyspeople.storagelabels.data.dto.ImageMetadata
import java.io.File
import javax.inject.Inject

data class ImagesState(
    val images: List<ImageMetadata> = emptyList(),
    val showImages: Boolean = true,
    val loading: Boolean = true,
    val uploading: Boolean = false,
    val error: String? = null,
)

@HiltViewModel
class ImagesViewModel @Inject constructor(
    private val images: ImageRepository,
    userRepository: UserRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(ImagesState())
    val state: StateFlow<ImagesState> = _state.asStateFlow()

    init {
        refresh()
        viewModelScope.launch {
            userRepository.preferences.collect { preferences ->
                _state.update { it.copy(showImages = preferences?.showImages ?: true) }
            }
        }
    }

    fun refresh() {
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            when (val result = images.list()) {
                is ApiResult.Success -> _state.update {
                    it.copy(images = result.value.sortedByDescending(ImageMetadata::uploadedAt), loading = false)
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun uploadFile(file: File, onUploaded: (String, String) -> Unit = { _, _ -> }) {
        _state.update { it.copy(uploading = true, error = null) }
        viewModelScope.launch {
            finishUpload(images.upload(file), onUploaded)
            // The camera writes into the cache; the copy on the server is the one that counts.
            runCatching { file.delete() }
        }
    }

    fun uploadUri(uri: Uri, fileName: String, onUploaded: (String, String) -> Unit = { _, _ -> }) {
        _state.update { it.copy(uploading = true, error = null) }
        viewModelScope.launch {
            finishUpload(images.upload(uri, fileName), onUploaded)
        }
    }

    private fun finishUpload(result: ApiResult<ImageMetadata>, onUploaded: (String, String) -> Unit) {
        when (result) {
            is ApiResult.Success -> {
                _state.update { it.copy(uploading = false) }
                onUploaded(result.value.url, result.value.imageId)
                refresh()
            }
            is ApiResult.Failure -> _state.update {
                it.copy(uploading = false, error = result.error.userMessage())
            }
        }
    }

    fun delete(image: ImageMetadata, force: Boolean, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = images.delete(image.imageId, force)) {
                is ApiResult.Success -> {
                    onDone("Deleted ${image.fileName}.")
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
