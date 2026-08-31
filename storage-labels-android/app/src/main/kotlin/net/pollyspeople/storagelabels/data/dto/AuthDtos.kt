package net.pollyspeople.storagelabels.data.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

/**
 * Mirrors of the API's authentication DTOs. The API serialises enums as strings and
 * properties as camelCase, so names map straight across.
 */

@Serializable
enum class AuthMode {
    @SerialName("Local")
    Local,

    @SerialName("None")
    None,
}

@Serializable
data class AuthConfigResponse(
    val mode: AuthMode,
    val allowRegistration: Boolean = false,
    val requireEmailConfirmation: Boolean = false,
)

@Serializable
data class LoginRequest(
    val usernameOrEmail: String,
    val password: String,
    val rememberMe: Boolean = true,
)

@Serializable
data class RegisterRequest(
    val email: String,
    val username: String,
    val password: String,
    val firstName: String,
    val lastName: String,
)

@Serializable
data class ChangePasswordRequest(
    val currentPassword: String,
    val newPassword: String,
)

@Serializable
data class UserInfoResponse(
    val userId: String,
    val username: String,
    val email: String,
    val fullName: String? = null,
    val profilePictureUrl: String? = null,
    val roles: List<String> = emptyList(),
    val permissions: List<String> = emptyList(),
    val isActive: Boolean = true,
)

/**
 * The API deliberately omits the refresh token from this payload — it is issued as an
 * HttpOnly cookie scoped to /api/auth. See [net.pollyspeople.storagelabels.core.network.PersistentCookieJar].
 */
@Serializable
data class AuthenticationResult(
    val token: String,
    val expiresAt: String,
    val user: UserInfoResponse,
)
