package net.pollyspeople.storagelabels.core.settings

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class ServerSettingsUrlTest {

    @Test
    fun `assumes https when no scheme is typed`() {
        assertEquals("https://storage.example.net", ServerSettings.normalizeUrl("storage.example.net"))
    }

    @Test
    fun `keeps an explicit http scheme for lan servers`() {
        assertEquals("http://192.168.1.10:8080", ServerSettings.normalizeUrl("http://192.168.1.10:8080"))
    }

    @Test
    fun `trims whitespace and trailing slashes`() {
        assertEquals("https://storage.example.net", ServerSettings.normalizeUrl("  https://storage.example.net/  "))
    }

    @Test
    fun `keeps a subpath for servers hosted under one`() {
        assertEquals("https://example.net/storage", ServerSettings.normalizeUrl("https://example.net/storage/"))
    }

    @Test
    fun `rejects input that is not an address`() {
        assertNull(ServerSettings.normalizeUrl(""))
        assertNull(ServerSettings.normalizeUrl("   "))
        assertNull(ServerSettings.normalizeUrl("ftp://storage.example.net"))
        assertNull(ServerSettings.normalizeUrl("https://"))
    }
}
