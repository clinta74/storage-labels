package net.pollyspeople.storagelabels.core.images

import android.content.Context
import android.net.Uri
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import net.pollyspeople.storagelabels.core.result.ApiError
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.data.api.ImageApi
import net.pollyspeople.storagelabels.data.dto.ImageMetadata
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.File
import java.io.IOException
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ImageRepository @Inject constructor(
    private val api: ImageApi,
    @ApplicationContext private val context: Context,
) {

    suspend fun list(): ApiResult<List<ImageMetadata>> = apiCall { api.getUserImages() }

    /** Uploads a JPEG the camera wrote to app storage. */
    suspend fun upload(file: File): ApiResult<ImageMetadata> = apiCall {
        val part = MultipartBody.Part.createFormData(
            name = "file",
            filename = file.name,
            body = file.asRequestBody(JPEG),
        )
        api.uploadImage(part)
    }

    /**
     * Uploads a picked image. Content-URI reads have to happen off the main thread and the
     * bytes are buffered because the API needs a known length.
     */
    suspend fun upload(uri: Uri, fileName: String): ApiResult<ImageMetadata> {
        val bytes = withContext(Dispatchers.IO) {
            runCatching {
                context.contentResolver.openInputStream(uri)?.use { it.readBytes() }
            }.getOrNull()
        } ?: return ApiResult.Failure(ApiError.Network(IOException("Couldn't read that image.")))

        return apiCall {
            val part = MultipartBody.Part.createFormData(
                name = "file",
                filename = fileName,
                body = bytes.toRequestBody(JPEG),
            )
            api.uploadImage(part)
        }
    }

    suspend fun delete(imageId: String, force: Boolean): ApiResult<Unit> {
        val result = if (force) {
            apiCall { api.forceDeleteImage(imageId) }
        } else {
            apiCall { api.deleteImage(imageId) }
        }
        return when (result) {
            is ApiResult.Success -> ApiResult.Success(Unit)
            is ApiResult.Failure -> result
        }
    }

    private companion object {
        val JPEG = "image/jpeg".toMediaType()
    }
}
