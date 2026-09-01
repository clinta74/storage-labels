package net.pollyspeople.storagelabels.feature.search

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.search.SearchRepository
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.data.dto.SearchResult
import javax.inject.Inject

data class SearchState(
    val query: String = "",
    val results: List<SearchResult> = emptyList(),
    val totalCount: Int = 0,
    val page: Int = 1,
    val hasMore: Boolean = false,
    val searching: Boolean = false,
    val loadingMore: Boolean = false,
    val error: String? = null,
    /** Set when a scanned label matches nothing, which isn't an error worth a banner. */
    val scanMiss: String? = null,
)

@OptIn(FlowPreview::class)
@HiltViewModel
class SearchViewModel @Inject constructor(
    private val search: SearchRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(SearchState())
    val state: StateFlow<SearchState> = _state.asStateFlow()

    private val queries = MutableStateFlow("")

    /** Held so a new query cancels the one in flight instead of racing it. */
    private var searchJob: Job? = null

    init {
        viewModelScope.launch {
            // The web app searches as you type; debouncing keeps that feel without a request
            // per keystroke, which the API rate-limits anyway.
            queries
                .debounce(300)
                .distinctUntilChanged()
                .collect { query ->
                    if (query.isBlank()) clearResults() else runSearch(query, page = 1)
                }
        }
    }

    fun onQueryChange(value: String) {
        _state.update { it.copy(query = value, scanMiss = null) }
        queries.value = value.trim()
    }

    fun clear() {
        searchJob?.cancel()
        _state.value = SearchState()
        queries.value = ""
    }

    private fun clearResults() = _state.update {
        it.copy(results = emptyList(), totalCount = 0, page = 1, hasMore = false, searching = false)
    }

    private fun runSearch(query: String, page: Int) {
        searchJob?.cancel()
        _state.update {
            if (page == 1) it.copy(searching = true, error = null) else it.copy(loadingMore = true)
        }

        searchJob = viewModelScope.launch {
            when (val result = search.search(query, page)) {
                is ApiResult.Success -> _state.update { current ->
                    val combined = if (page == 1) {
                        result.value.results
                    } else {
                        current.results + result.value.results
                    }
                    current.copy(
                        results = combined,
                        totalCount = result.value.totalCount,
                        page = result.value.pageNumber,
                        hasMore = result.value.hasMore,
                        searching = false,
                        loadingMore = false,
                    )
                }

                is ApiResult.Failure -> _state.update {
                    it.copy(
                        searching = false,
                        loadingMore = false,
                        error = result.error.userMessage(),
                    )
                }
            }
        }
    }

    fun loadMore() {
        val current = _state.value
        if (!current.hasMore || current.loadingMore || current.searching) return
        runSearch(current.query.trim(), current.page + 1)
    }

    /** Looks up a scanned label and hands back the box it belongs to, if any. */
    fun onCodeScanned(code: String, onFound: (locationId: Long, boxId: String) -> Unit) {
        viewModelScope.launch {
            when (val result = search.findByCode(code)) {
                is ApiResult.Success -> {
                    val hit = result.value
                    val locationId = hit?.locationIdOrNull
                    val boxId = hit?.boxId
                    if (hit == null || locationId == null || boxId == null) {
                        _state.update { it.copy(scanMiss = "No box or item carries the code $code.") }
                    } else {
                        onFound(locationId, boxId)
                    }
                }
                is ApiResult.Failure -> _state.update {
                    it.copy(error = result.error.userMessage())
                }
            }
        }
    }

    fun clearScanMiss() = _state.update { it.copy(scanMiss = null) }
}
