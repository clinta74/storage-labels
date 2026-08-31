package net.pollyspeople.storagelabels.data.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class CommonLocation(
    val commonLocationId: Int,
    val name: String,
)

@Serializable
data class CommonLocationRequest(val name: String)

@Serializable
data class UserWithRoles(
    val userId: String,
    val email: String = "",
    val username: String? = null,
    val fullName: String = "",
    val created: String? = null,
    val isActive: Boolean = true,
    val roles: List<String> = emptyList(),
) {
    val displayName: String get() = fullName.ifBlank { username ?: email }
    val role: String get() = roles.firstOrNull() ?: "User"
}

@Serializable
data class UpdateUserRoleRequest(val role: String)

@Serializable
data class AdminResetPasswordRequest(val userId: String, val newPassword: String)

@Serializable
enum class EncryptionKeyStatus {
    @SerialName("Created")
    Created,

    @SerialName("Active")
    Active,

    @SerialName("Deprecated")
    Deprecated,

    @SerialName("Retired")
    Retired,
}

@Serializable
enum class RotationStatus {
    @SerialName("InProgress")
    InProgress,

    @SerialName("Completed")
    Completed,

    @SerialName("Failed")
    Failed,

    @SerialName("Cancelled")
    Cancelled,
    ;

    val isFinished: Boolean get() = this != InProgress
}

@Serializable
data class EncryptionKey(
    val kid: Int,
    val version: Int,
    val status: EncryptionKeyStatus,
    val createdAt: String? = null,
    val activatedAt: String? = null,
    val retiredAt: String? = null,
    val deprecatedAt: String? = null,
    val description: String? = null,
    val createdBy: String? = null,
    val algorithm: String = "AES-256-GCM",
)

@Serializable
data class CreateEncryptionKeyRequest(val description: String? = null)

@Serializable
data class EncryptionKeyStats(
    val kid: Int,
    val version: Int,
    val status: EncryptionKeyStatus,
    val imageCount: Int = 0,
    val totalSizeBytes: Long = 0,
    val createdAt: String? = null,
    val activatedAt: String? = null,
    val retiredAt: String? = null,
)

@Serializable
data class EncryptionKeyRotation(
    val id: String,
    val fromKeyId: Int? = null,
    val toKeyId: Int,
    val status: RotationStatus,
    val startedAt: String? = null,
    val completedAt: String? = null,
    val totalImages: Int = 0,
    val processedImages: Int = 0,
    val failedImages: Int = 0,
    val errorMessage: String? = null,
)

/** [fromKeyId] null means "encrypt the images that aren't encrypted yet". */
@Serializable
data class StartRotationRequest(
    val fromKeyId: Int? = null,
    val toKeyId: Int,
    val batchSize: Int = 50,
)

@Serializable
data class RotationProgress(
    val rotationId: String,
    val status: RotationStatus,
    val totalImages: Int = 0,
    val processedImages: Int = 0,
    val failedImages: Int = 0,
    val percentComplete: Double = 0.0,
    val startedAt: String? = null,
    val completedAt: String? = null,
    val errorMessage: String? = null,
)
