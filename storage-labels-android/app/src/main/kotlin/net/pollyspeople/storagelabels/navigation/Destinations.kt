package net.pollyspeople.storagelabels.navigation

import kotlinx.serialization.Serializable
import net.pollyspeople.storagelabels.core.permissions.Permissions

/**
 * Type-safe routes, named after the web app's URLs so the two stay recognisably the same
 * product. Destinations for Phase 2+ exist here already because the drawer needs to show
 * the same entries the web navigation bar shows.
 */
sealed interface Route {
    @Serializable data object Locations : Route
    @Serializable data object Images : Route
    @Serializable data object Search : Route

    /** Picks a photo and hands it back to the box or item form that opened it. */
    @Serializable data object ImagePicker : Route

    /** Scans a label and hands the code back to the box form. */
    @Serializable data object CodeScanner : Route
    @Serializable data object Labels : Route
    @Serializable data object CommonLocations : Route
    @Serializable data object EncryptionKeys : Route
    @Serializable data object Users : Route
    @Serializable data object Preferences : Route
    @Serializable data object ChangePassword : Route

    @Serializable data class LocationDetail(val locationId: Long) : Route
    @Serializable data class LocationUsers(val locationId: Long) : Route

    /** [boxId] null means "create a box here". */
    @Serializable data class BoxEdit(val locationId: Long, val boxId: String? = null) : Route
    @Serializable data class BoxDetail(val locationId: Long, val boxId: String) : Route

    /** [itemId] null means "add an item to this box". */
    @Serializable data class ItemEdit(
        val locationId: Long,
        val boxId: String,
        val itemId: String? = null,
    ) : Route
}

/**
 * A drawer entry. [permission] mirrors the web navigation bar: entries the account can't use
 * are not shown at all.
 */
data class NavEntry(
    val label: String,
    val route: Route,
    val permission: String? = null,
)

val PrimaryNavEntries = listOf(
    NavEntry("Search", Route.Search),
    NavEntry("Locations", Route.Locations),
    NavEntry("Images", Route.Images),
    NavEntry("Labels", Route.Labels),
    NavEntry("Common locations", Route.CommonLocations, Permissions.READ_COMMON_LOCATIONS),
    NavEntry("Encryption keys", Route.EncryptionKeys, Permissions.READ_ENCRYPTION_KEYS),
    NavEntry("Users", Route.Users, Permissions.WRITE_USER),
)

val AccountNavEntries = listOf(
    NavEntry("Preferences", Route.Preferences),
    NavEntry("Change password", Route.ChangePassword),
)

/** Filters the drawer to what this session may actually open. */
fun List<NavEntry>.visibleTo(permissions: List<String>): List<NavEntry> =
    filter { entry -> entry.permission == null || entry.permission in permissions }
