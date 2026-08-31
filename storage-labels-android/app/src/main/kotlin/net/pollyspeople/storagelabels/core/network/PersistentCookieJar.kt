package net.pollyspeople.storagelabels.core.network

import net.pollyspeople.storagelabels.core.auth.TokenStore
import okhttp3.Cookie
import okhttp3.CookieJar
import okhttp3.HttpUrl
import javax.inject.Inject
import javax.inject.Singleton

/**
 * The API issues its refresh token as an HttpOnly cookie scoped to /api/auth
 * (CookieHelpers.cs), so a native client has to keep a cookie jar to stay signed in.
 *
 * Two consequences worth knowing:
 *  - the cookie is marked Secure, so refresh only works over HTTPS;
 *  - SameSite=Strict is a browser concept and is irrelevant here.
 *
 * Cookies are encrypted at rest in the same store as the access token, and are dropped when
 * they expire or when the session is cleared.
 */
@Singleton
class PersistentCookieJar @Inject constructor(
    private val store: TokenStore,
) : CookieJar {

    private val prefs = store.preferences()
    private val cache = linkedMapOf<String, Cookie>()

    init {
        loadFromDisk()
    }

    @Synchronized
    override fun saveFromResponse(url: HttpUrl, cookies: List<Cookie>) {
        if (cookies.isEmpty()) return
        cookies.forEach { cookie ->
            val key = keyOf(cookie)
            if (cookie.expiresAt <= System.currentTimeMillis()) {
                cache.remove(key)
            } else {
                cache[key] = cookie
            }
        }
        persist()
    }

    @Synchronized
    override fun loadForRequest(url: HttpUrl): List<Cookie> {
        val now = System.currentTimeMillis()
        val expired = cache.filterValues { it.expiresAt <= now }.keys.toList()
        if (expired.isNotEmpty()) {
            expired.forEach(cache::remove)
            persist()
        }
        return cache.values.filter { it.matches(url) }
    }

    @Synchronized
    fun clear() {
        cache.clear()
        prefs.edit().remove(KEY_COOKIES).apply()
    }

    private fun keyOf(cookie: Cookie): String = cookie.domain + "|" + cookie.path + "|" + cookie.name

    private fun persist() {
        // The refresh cookie is a credential, so it is encrypted at rest like the token.
        val serialized = cache.values
            .mapNotNull { cookie -> store.protect(cookie.toString() + SEPARATOR + cookie.domain) }
            .toSet()
        prefs.edit().putStringSet(KEY_COOKIES, serialized).apply()
    }

    private fun loadFromDisk() {
        val stored = prefs.getStringSet(KEY_COOKIES, emptySet()).orEmpty()
        stored.forEach { encrypted ->
            val entry = store.reveal(encrypted) ?: return@forEach
            val setCookieHeader = entry.substringBefore(SEPARATOR)
            val domain = entry.substringAfter(SEPARATOR, missingDelimiterValue = "")
            if (domain.isBlank()) return@forEach

            val url = HttpUrl.Builder().scheme("https").host(domain).build()
            val cookie = Cookie.parse(url, setCookieHeader) ?: return@forEach
            if (cookie.expiresAt > System.currentTimeMillis()) {
                cache[keyOf(cookie)] = cookie
            }
        }
    }

    private companion object {
        const val KEY_COOKIES = "cookies"
        const val SEPARATOR = " @"
    }
}
