package net.pollyspeople.storagelabels.data.dto

import kotlinx.serialization.Serializable

@Serializable
data class ImageMetadata(
    val imageId: String,
    val fileName: String = "",
    val contentType: String = "",
    /** Relative, e.g. /api/images/{id}. Resolved against the configured server when loaded. */
    val url: String = "",
    val uploadedAt: String? = null,
    val sizeInBytes: Long = 0,
    val boxReferenceCount: Int = 0,
    val itemReferenceCount: Int = 0,
) {
    val isReferenced: Boolean get() = boxReferenceCount > 0 || itemReferenceCount > 0

    /**
     * The upload endpoint returns the ImageMetadata entity, which carries no url — only
     * the list endpoint's response DTO builds one. Derive it the same way the server does
     * so an image is usable the moment it is uploaded.
     */
    val resolvedUrl: String get() = url.ifBlank { imagePath(imageId) }
}

/** The API's route for fetching an image by id. */
fun imagePath(imageId: String): String = "/api/images/" + imageId

@Serializable
data class SearchResult(
    val type: String,
    val rank: Float = 0f,
    val boxId: String? = null,
    val boxName: String? = null,
    val boxCode: String? = null,
    val itemId: String? = null,
    val itemName: String? = null,
    val itemCode: String? = null,
    /** The API returns this as a string even though locations use numeric ids elsewhere. */
    val locationId: String = "",
    val locationName: String = "",
) {
    val isItem: Boolean get() = type.equals("item", ignoreCase = true)

    val title: String get() = if (isItem) itemName.orEmpty() else boxName.orEmpty()

    val code: String? get() = if (isItem) itemCode ?: boxCode else boxCode

    val locationIdOrNull: Long? get() = locationId.toLongOrNull()
}
