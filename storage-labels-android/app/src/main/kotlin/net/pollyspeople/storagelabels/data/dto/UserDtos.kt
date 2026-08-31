package net.pollyspeople.storagelabels.data.dto

import kotlinx.serialization.Serializable

/**
 * Preferences are stored server-side as a JSON blob (UserPreferencesResponse), so the same
 * settings follow a person between the web app and this one.
 */
@Serializable
data class UserPreferences(
    val theme: String = THEME_LIGHT,
    val showImages: Boolean = true,
    val codeColorPattern: String = "",
) {
    val isDark: Boolean get() = theme.equals(THEME_DARK, ignoreCase = true)

    companion object {
        const val THEME_LIGHT = "light"
        const val THEME_DARK = "dark"
    }
}

@Serializable
data class UserResponse(
    val userId: String,
    val firstName: String = "",
    val lastName: String = "",
    val emailAddress: String = "",
    val created: String? = null,
    val preferences: UserPreferences? = null,
)
