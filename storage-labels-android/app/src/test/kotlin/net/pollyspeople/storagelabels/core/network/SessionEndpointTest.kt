package net.pollyspeople.storagelabels.core.network

import okhttp3.HttpUrl.Companion.toHttpUrl
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * This predicate decides whether a 401 gets a refresh and a replay. Reading it too widely --
 * "anything under /auth" -- is what stopped a stored session from being restored: `auth/me`
 * was exempted, so an expired access token signed the person out rather than refreshing.
 */
class SessionEndpointTest {

    @Test
    fun `session endpoints answer their own 401s`() {
        listOf(
            "login",
            "register",
            "refresh",
            "logout",
            "config",
            "forgot-password",
            "reset-password",
            "confirm-email",
        ).forEach { path ->
            assertTrue(path, url("/api/auth/$path").isSessionEndpoint())
        }
    }

    @Test
    fun `restoring a session is an ordinary authenticated call`() {
        assertFalse(url("/api/auth/me").isSessionEndpoint())
        assertFalse(url("/api/auth/change-password").isSessionEndpoint())
    }

    @Test
    fun `everything outside auth is refreshable`() {
        assertFalse(url("/api/boxes").isSessionEndpoint())
        assertFalse(url("/api/locations/4/boxes").isSessionEndpoint())
        assertFalse(url("/api/images").isSessionEndpoint())
    }

    /** A path segment merely containing the word is not the auth area. */
    @Test
    fun `only the auth segment counts`() {
        assertFalse(url("/api/authors/login").isSessionEndpoint())
        assertFalse(url("/api/auth").isSessionEndpoint())
    }

    private fun url(path: String) = ("https://storage.example.net$path").toHttpUrl()
}
