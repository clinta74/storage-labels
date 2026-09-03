package net.pollyspeople.storagelabels.core.search

import net.pollyspeople.storagelabels.core.result.ApiError
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.data.api.SearchApi
import net.pollyspeople.storagelabels.data.dto.SearchResult
import retrofit2.HttpException
import javax.inject.Inject
import javax.inject.Singleton

data class SearchPage(
    val results: List<SearchResult>,
    val totalCount: Int,
    val pageNumber: Int,
    val pageSize: Int,
) {
    val totalPages: Int get() = if (pageSize == 0) 0 else (totalCount + pageSize - 1) / pageSize
    val hasMore: Boolean get() = pageNumber < totalPages
}

@Singleton
class SearchRepository @Inject constructor(
    private val api: SearchApi,
) {

    suspend fun search(query: String, pageNumber: Int, pageSize: Int = PAGE_SIZE): ApiResult<SearchPage> =
        apiCall {
            val response = api.search(query, pageNumber, pageSize)
            if (!response.isSuccessful) throw HttpException(response)

            SearchPage(
                results = response.body().orEmpty(),
                // The API reports the total in a header rather than a wrapper object.
                totalCount = response.headers()["x-total-count"]?.toIntOrNull() ?: 0,
                pageNumber = pageNumber,
                pageSize = pageSize,
            )
        }

    /**
     * Exact-code lookup for a scanned label. A 404 here is an ordinary outcome — an unknown
     * label — not a failure worth an error banner.
     */
    suspend fun findByCode(code: String): ApiResult<SearchResult?> =
        when (val result = apiCall { api.searchByQrCode(code) }) {
            is ApiResult.Success -> ApiResult.Success(result.value)
            is ApiResult.Failure ->
                if (result.error is ApiError.NotFound) ApiResult.Success(null) else result
        }

    companion object {
        const val PAGE_SIZE = 10
    }
}
