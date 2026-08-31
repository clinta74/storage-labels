package net.pollyspeople.storagelabels.data.api

import net.pollyspeople.storagelabels.data.dto.ImageMetadata
import net.pollyspeople.storagelabels.data.dto.SearchResult
import okhttp3.MultipartBody
import retrofit2.Response
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.Part
import retrofit2.http.Path
import retrofit2.http.Query

interface ImageApi {

    @GET("images")
    suspend fun getUserImages(): List<ImageMetadata>

    /** JPEG only — the API rejects anything else. The part name must be "file". */
    @Multipart
    @POST("images")
    suspend fun uploadImage(@Part file: MultipartBody.Part): ImageMetadata

    @DELETE("images/{imageId}")
    suspend fun deleteImage(@Path("imageId") imageId: String): Response<Unit>

    /** Deletes an image that boxes or items still reference. */
    @DELETE("images/{imageId}/force")
    suspend fun forceDeleteImage(@Path("imageId") imageId: String): Response<Unit>
}

interface SearchApi {

    /**
     * Returns a page of results; the total lives in the x-total-count header, so the raw
     * [Response] is needed rather than just the body.
     */
    @GET("search")
    suspend fun search(
        @Query("query") query: String,
        @Query("pageNumber") pageNumber: Int = 1,
        @Query("pageSize") pageSize: Int = 10,
        @Query("locationId") locationId: String? = null,
        @Query("boxId") boxId: String? = null,
    ): Response<List<SearchResult>>

    @GET("search/qrcode/{code}")
    suspend fun searchByQrCode(@Path("code") code: String): SearchResult
}
