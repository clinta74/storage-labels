package net.pollyspeople.storagelabels.navigation

import net.pollyspeople.storagelabels.core.permissions.Permissions
import org.junit.Assert.assertEquals
import org.junit.Test

/**
 * The drawer must hide what an account cannot use, the way the web navigation bar does —
 * an Auditor should not be offered "Users", and a standard User should see neither the
 * admin entries nor common locations.
 */
class NavEntryPermissionTest {

    @Test
    fun `a standard user sees only the three everyday sections`() {
        val visible = PrimaryNavEntries.visibleTo(emptyList()).map { it.label }

        assertEquals(listOf("Locations", "Images", "Labels"), visible)
    }

    @Test
    fun `an auditor gets the read-only admin sections but not user management`() {
        val auditor = listOf(
            Permissions.READ_USER,
            Permissions.READ_COMMON_LOCATIONS,
            Permissions.READ_ENCRYPTION_KEYS,
        )

        val visible = PrimaryNavEntries.visibleTo(auditor).map { it.label }

        assertEquals(
            listOf("Locations", "Images", "Labels", "Common locations", "Encryption keys"),
            visible,
        )
    }

    @Test
    fun `an admin sees everything`() {
        val admin = listOf(
            Permissions.READ_USER,
            Permissions.WRITE_USER,
            Permissions.READ_COMMON_LOCATIONS,
            Permissions.WRITE_COMMON_LOCATIONS,
            Permissions.READ_ENCRYPTION_KEYS,
            Permissions.WRITE_ENCRYPTION_KEYS,
        )

        assertEquals(PrimaryNavEntries.size, PrimaryNavEntries.visibleTo(admin).size)
    }

    @Test
    fun `account entries are always available`() {
        assertEquals(AccountNavEntries.size, AccountNavEntries.visibleTo(emptyList()).size)
    }
}
