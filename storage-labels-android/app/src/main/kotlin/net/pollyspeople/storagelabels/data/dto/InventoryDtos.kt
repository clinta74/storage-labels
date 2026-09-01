package net.pollyspeople.storagelabels.data.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
enum class AccessLevel {
    @SerialName("None")
    None,

    @SerialName("View")
    View,

    @SerialName("Edit")
    Edit,

    @SerialName("Owner")
    Owner,
    ;

    val canEdit: Boolean get() = this == Edit || this == Owner
    val canManageUsers: Boolean get() = this == Owner
}

@Serializable
data class StorageLocation(
    val locationId: Long,
    val name: String,
    val accessLevel: AccessLevel = AccessLevel.View,
    val created: String? = null,
    val updated: String? = null,
)

@Serializable
data class LocationRequest(val name: String)

@Serializable
data class Box(
    val boxId: String,
    val code: String,
    val name: String,
    val description: String? = null,
    val imageUrl: String? = null,
    val imageMetadataId: String? = null,
    val locationId: Long,
    /** The API counts these server-side; older servers omit the field and it reads zero. */
    val itemCount: Int = 0,
    val created: String? = null,
    val updated: String? = null,
    val lastAccessed: String? = null,
) {
    /**
     * Boxes saved before the upload response was understood carry an id but no url, so fall
     * back to the id. Either way this is what the UI should show.
     */
    val photoUrl: String? get() = imageUrl?.takeIf(String::isNotBlank)
        ?: imageMetadataId?.takeIf(String::isNotBlank)?.let(::imagePath)
}

@Serializable
data class BoxRequest(
    val code: String,
    val name: String,
    val locationId: Long,
    val description: String? = null,
    val imageUrl: String? = null,
    val imageMetadataId: String? = null,
)

@Serializable
data class MoveBoxRequest(val destinationLocationId: Long)

@Serializable
data class Item(
    val itemId: String,
    val boxId: String,
    val name: String,
    val description: String? = null,
    val imageUrl: String? = null,
    val imageMetadataId: String? = null,
    val created: String? = null,
    val updated: String? = null,
) {
    val photoUrl: String? get() = imageUrl?.takeIf(String::isNotBlank)
        ?: imageMetadataId?.takeIf(String::isNotBlank)?.let(::imagePath)
}

@Serializable
data class ItemRequest(
    val boxId: String,
    val name: String,
    val description: String? = null,
    val imageUrl: String? = null,
    val imageMetadataId: String? = null,
)

@Serializable
data class LocationUser(
    val userId: String,
    val firstName: String = "",
    val lastName: String = "",
    val emailAddress: String = "",
    val accessLevel: AccessLevel = AccessLevel.View,
) {
    val displayName: String
        get() = listOf(firstName, lastName).filter { it.isNotBlank() }.joinToString(" ")
            .ifBlank { emailAddress }
}

@Serializable
data class AddUserLocationRequest(
    val emailAddress: String,
    val accessLevel: AccessLevel,
)

@Serializable
data class UpdateUserLocationRequest(val accessLevel: AccessLevel)
