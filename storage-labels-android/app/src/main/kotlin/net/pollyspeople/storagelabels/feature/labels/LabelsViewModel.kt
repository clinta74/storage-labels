package net.pollyspeople.storagelabels.feature.labels

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
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.core.user.UserRepository
import net.pollyspeople.storagelabels.data.api.LabelApi
import net.pollyspeople.storagelabels.data.dto.CreateLabelPrintJobRequest
import net.pollyspeople.storagelabels.data.dto.LabelFormat
import net.pollyspeople.storagelabels.data.dto.LabelIncrementAlgorithm
import net.pollyspeople.storagelabels.data.dto.LabelPage
import net.pollyspeople.storagelabels.data.dto.LabelPrintJob
import net.pollyspeople.storagelabels.data.dto.UpdateLabelPrintJobRequest
import net.pollyspeople.storagelabels.navigation.Route
import javax.inject.Inject

data class LabelsState(
    val jobs: List<LabelPrintJob> = emptyList(),
    val loading: Boolean = true,
    val error: String? = null,
)

@HiltViewModel
class LabelsViewModel @Inject constructor(
    private val api: LabelApi,
) : ViewModel() {

    private val _state = MutableStateFlow(LabelsState())
    val state: StateFlow<LabelsState> = _state.asStateFlow()

    init {
        refresh()
    }

    fun refresh() {
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            when (val result = apiCall { api.getJobs() }) {
                is ApiResult.Success -> _state.update { it.copy(jobs = result.value, loading = false) }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun delete(job: LabelPrintJob, onDone: (String) -> Unit) {
        viewModelScope.launch {
            when (val result = apiCall { api.deleteJob(job.id) }) {
                is ApiResult.Success -> {
                    onDone("Deleted ${job.name}.")
                    refresh()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }
}

data class LabelJobDetailState(
    val job: LabelPrintJob? = null,
    /** The page most recently allocated. Kept so rotating the phone doesn't burn another. */
    val page: LabelPage? = null,
    val loading: Boolean = true,
    val allocating: Boolean = false,
    val error: String? = null,
)

@HiltViewModel
class LabelJobDetailViewModel @Inject constructor(
    private val savedStateHandle: SavedStateHandle,
    private val api: LabelApi,
) : ViewModel() {

    private val jobId: String = savedStateHandle.toRoute<Route.LabelJob>().jobId

    private val _state = MutableStateFlow(LabelJobDetailState())
    val state: StateFlow<LabelJobDetailState> = _state.asStateFlow()

    init {
        refresh()
        // A page survives process death: the codes are already spent server-side, so losing
        // them would mean a gap in the printed sequence.
        savedStateHandle.get<String>(KEY_PAGE_CODES)?.let { restorePage(it) }
    }

    fun refresh() {
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            when (val result = apiCall { api.getJob(jobId) }) {
                is ApiResult.Success -> _state.update { it.copy(job = result.value, loading = false) }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = result.error.userMessage())
                }
            }
        }
    }

    /**
     * Allocates the next sheet. Guarded against double-taps because every call permanently
     * advances the job's counter — a wasted call means a gap in the printed codes.
     */
    fun allocateNextPage(onReady: () -> Unit) {
        if (_state.value.allocating) return
        _state.update { it.copy(allocating = true, error = null) }

        viewModelScope.launch {
            when (val result = apiCall { api.getNextPage(jobId) }) {
                is ApiResult.Success -> {
                    savedStateHandle[KEY_PAGE_CODES] = result.value.labels.joinToString(",") {
                        "${it.code}:${it.labelNumber}"
                    }
                    _state.update { it.copy(page = result.value, allocating = false) }
                    refresh()
                    onReady()
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(allocating = false, error = result.error.userMessage())
                }
            }
        }
    }

    private fun restorePage(encoded: String) {
        val labels = encoded.split(",").mapNotNull { entry ->
            val code = entry.substringBeforeLast(':')
            val number = entry.substringAfterLast(':').toLongOrNull()
            if (code.isBlank() || number == null) {
                null
            } else {
                net.pollyspeople.storagelabels.data.dto.LabelCodeItem(code, number)
            }
        }
        if (labels.isNotEmpty()) {
            _state.update { it.copy(page = LabelPage(jobId = jobId, labels = labels)) }
        }
    }

    private companion object {
        const val KEY_PAGE_CODES = "allocated_page"
    }
}

data class LabelJobEditState(
    val name: String = "",
    val labelFormat: LabelFormat = LabelFormat.Avery94107,
    val algorithm: LabelIncrementAlgorithm = LabelIncrementAlgorithm.Base36Suffix,
    val prefix: String = "",
    val suffixLength: Int = 4,
    val startIndex: Long = 0,
    val codeColorPattern: String = "",
    val isNew: Boolean = true,
    val loading: Boolean = false,
    val saving: Boolean = false,
    val error: String? = null,
    val fieldErrors: Map<String, String> = emptyMap(),
)

@HiltViewModel
class LabelJobEditViewModel @Inject constructor(
    savedStateHandle: SavedStateHandle,
    private val api: LabelApi,
    userRepository: UserRepository,
) : ViewModel() {

    private val jobId: String? = savedStateHandle.toRoute<Route.LabelJobEdit>().jobId

    private val _state = MutableStateFlow(LabelJobEditState(isNew = jobId == null))
    val state: StateFlow<LabelJobEditState> = _state.asStateFlow()

    init {
        if (jobId != null) {
            load(jobId)
        } else {
            // A new job starts from the colour pattern the user already prefers.
            viewModelScope.launch {
                userRepository.preferences.collect { preferences ->
                    val pattern = preferences?.codeColorPattern.orEmpty()
                    _state.update {
                        if (it.codeColorPattern.isBlank()) it.copy(codeColorPattern = pattern) else it
                    }
                }
            }
        }
    }

    private fun load(id: String) {
        _state.update { it.copy(loading = true) }
        viewModelScope.launch {
            when (val result = apiCall { api.getJob(id) }) {
                is ApiResult.Success -> _state.update {
                    it.copy(
                        name = result.value.name,
                        labelFormat = result.value.labelFormat,
                        algorithm = result.value.incrementAlgorithm,
                        prefix = result.value.algorithmPrefix.orEmpty(),
                        suffixLength = result.value.algorithmSuffixLength,
                        codeColorPattern = result.value.codeColorPattern,
                        loading = false,
                    )
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(loading = false, error = result.error.userMessage())
                }
            }
        }
    }

    fun onNameChange(value: String) = edit { it.copy(name = value) }
    fun onAlgorithmChange(value: LabelIncrementAlgorithm) = edit { it.copy(algorithm = value) }
    fun onPrefixChange(value: String) = edit { it.copy(prefix = value) }
    fun onSuffixLengthChange(value: Int) = edit { it.copy(suffixLength = value) }
    fun onStartIndexChange(value: Long) = edit { it.copy(startIndex = value) }
    fun onPatternChange(value: String) = edit { it.copy(codeColorPattern = value) }

    private fun edit(block: (LabelJobEditState) -> LabelJobEditState) {
        _state.update { block(it).copy(fieldErrors = emptyMap(), error = null) }
    }

    fun save(onSaved: (String) -> Unit) {
        val current = _state.value
        val errors = validate(current)
        if (errors.isNotEmpty()) {
            _state.update { it.copy(fieldErrors = errors) }
            return
        }

        _state.update { it.copy(saving = true, error = null) }
        viewModelScope.launch {
            val result = if (jobId == null) {
                apiCall {
                    api.createJob(
                        CreateLabelPrintJobRequest(
                            name = current.name.trim(),
                            labelFormat = current.labelFormat,
                            incrementAlgorithm = current.algorithm,
                            algorithmPrefix = current.prefix.trim().ifBlank { null },
                            algorithmSuffixLength = current.suffixLength,
                            startIndex = current.startIndex,
                            codeColorPattern = current.codeColorPattern,
                        ),
                    )
                }
            } else {
                apiCall {
                    api.updateJob(
                        jobId,
                        UpdateLabelPrintJobRequest(
                            name = current.name.trim(),
                            labelFormat = current.labelFormat,
                            incrementAlgorithm = current.algorithm,
                            algorithmPrefix = current.prefix.trim().ifBlank { null },
                            algorithmSuffixLength = current.suffixLength,
                            codeColorPattern = current.codeColorPattern,
                        ),
                    )
                }
            }

            when (result) {
                is ApiResult.Success -> onSaved(
                    if (jobId == null) "Created ${result.value.name}." else "Saved ${result.value.name}.",
                )
                is ApiResult.Failure -> _state.update {
                    it.copy(saving = false, error = result.error.userMessage())
                }
            }
        }
    }

    companion object {
        const val FIELD_NAME = "name"
        const val FIELD_SUFFIX = "suffixLength"
        const val FIELD_PREFIX = "prefix"
        const val FIELD_START = "startIndex"

        /** Mirrors the web app's create form, which the API also enforces. */
        fun validate(state: LabelJobEditState): Map<String, String> = buildMap {
            if (state.name.isBlank()) put(FIELD_NAME, "Name is required.")
            if (state.suffixLength !in 1..10) put(FIELD_SUFFIX, "Suffix length must be between 1 and 10.")
            if (state.startIndex < 0) put(FIELD_START, "Start index must be 0 or more.")
            if (state.prefix.length > 50) put(FIELD_PREFIX, "Prefix can't exceed 50 characters.")
        }
    }
}
