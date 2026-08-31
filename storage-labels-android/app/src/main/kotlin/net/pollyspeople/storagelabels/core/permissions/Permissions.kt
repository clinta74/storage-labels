package net.pollyspeople.storagelabels.core.permissions

import androidx.compose.runtime.compositionLocalOf

/** The six permissions the API defines in Models/Authorization.cs. */
object Permissions {
    const val WRITE_USER = "write:user"
    const val READ_USER = "read:user"
    const val WRITE_COMMON_LOCATIONS = "write:common-locations"
    const val READ_COMMON_LOCATIONS = "read:common-locations"
    const val WRITE_ENCRYPTION_KEYS = "write:encryption-keys"
    const val READ_ENCRYPTION_KEYS = "read:encryption-keys"
}

/**
 * Granted permissions for the signed-in session. In "None" auth mode the repository fills this
 * with every permission, matching the web client's behaviour of treating every check as true.
 */
val LocalPermissions = compositionLocalOf { emptyList<String>() }

fun List<String>.hasPermission(vararg required: String): Boolean =
    required.any { it in this }
