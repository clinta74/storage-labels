package net.pollyspeople.storagelabels.feature.preferences

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.core.user.UserRepository
import net.pollyspeople.storagelabels.data.dto.UserPreferences
import javax.inject.Inject

data class PreferencesUiState(
    val preferences: UserPreferences = UserPreferences(),
    val busy: Boolean = true,
    val error: String? = null,
    /** Set when a save lands, so the screen can report it once. */
    val savedAt: Long? = null,
)

@HiltViewModel
class PreferencesViewModel @Inject constructor(
    private val userRepository: UserRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(PreferencesUiState())
    val state: StateFlow<PreferencesUiState> = _state.asStateFlow()

    init {
        viewModelScope.launch {
            when (val result = userRepository.load()) {
                is ApiResult.Success -> _state.update {
                    it.copy(preferences = result.value, busy = false)
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(busy = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun onThemeChange(theme: String) = edit { it.copy(theme = theme) }

    fun onShowImagesChange(showImages: Boolean) = edit { it.copy(showImages = showImages) }

    fun onCodeColorPatternChange(pattern: String) = edit { it.copy(codeColorPattern = pattern) }

    private fun edit(block: (UserPreferences) -> UserPreferences) {
        _state.update { it.copy(preferences = block(it.preferences), error = null) }
    }

    fun save() {
        val preferences = _state.value.preferences
        _state.update { it.copy(busy = true, error = null) }

        viewModelScope.launch {
            when (val result = userRepository.update(preferences)) {
                // Updating the repository re-themes the app immediately.
                is ApiResult.Success -> _state.update {
                    it.copy(preferences = result.value, busy = false, savedAt = System.currentTimeMillis())
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(busy = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun acknowledgeSaved() = _state.update { it.copy(savedAt = null) }
}
