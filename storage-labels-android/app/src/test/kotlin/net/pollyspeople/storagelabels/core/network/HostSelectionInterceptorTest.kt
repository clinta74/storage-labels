package net.pollyspeople.storagelabels.core.network

import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.mockwebserver.MockResponse
import okhttp3.mockwebserver.MockWebServer
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Before
import org.junit.Test

/**
 * Retrofit is built against a placeholder host, so the rewrite done here is what actually
 * decides where every call in the app lands.
 */
class HostSelectionInterceptorTest {

    private lateinit var server: MockWebServer
    private val urlProvider = ServerUrlProvider()

    @Before
    fun setUp() {
        server = MockWebServer()
        server.start()
    }

    @After
    fun tearDown() {
        server.shutdown()
    }

    @Test
    fun `rewrites onto the configured server under the api prefix`() {
        urlProvider.update(server.url("/").toString().trimEnd('/'))
        server.enqueue(MockResponse().setResponseCode(200))

        client().newCall(request("http://server.invalid/auth/login")).execute().close()

        assertEquals("/api/auth/login", server.takeRequest().path)
    }

    @Test
    fun `keeps the query string`() {
        urlProvider.update(server.url("/").toString().trimEnd('/'))
        server.enqueue(MockResponse().setResponseCode(200))

        client().newCall(request("http://server.invalid/search?query=blue&pageNumber=2")).execute().close()

        assertEquals("/api/search?query=blue&pageNumber=2", server.takeRequest().path)
    }

    @Test
    fun `preserves a subpath when the server is hosted under one`() {
        urlProvider.update(server.url("/storage").toString())
        server.enqueue(MockResponse().setResponseCode(200))

        client().newCall(request("http://server.invalid/location")).execute().close()

        assertEquals("/storage/api/location", server.takeRequest().path)
    }

    @Test
    fun `fails clearly when no server is configured`() {
        urlProvider.update(null)

        assertThrows(NoServerConfiguredException::class.java) {
            client().newCall(request("http://server.invalid/auth/config")).execute()
        }
    }

    private fun client() = OkHttpClient.Builder()
        .addInterceptor(HostSelectionInterceptor(urlProvider))
        .build()

    private fun request(url: String) = Request.Builder().url(url).build()
}
