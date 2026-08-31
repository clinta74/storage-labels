package net.pollyspeople.storagelabels.core.auth

import android.content.Context
import android.content.SharedPreferences
import dagger.hilt.android.qualifiers.ApplicationContext
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Holds the access token, and backs the refresh cookie store
 * ([net.pollyspeople.storagelabels.core.network.PersistentCookieJar]).
 *
 * Values are encrypted with a Keystore key before they touch disk — Jetpack Security's
 * EncryptedSharedPreferences is deprecated, so [KeystoreCrypto] does that job directly.
 *
 * Reads are synchronous because the OkHttp interceptor and authenticator run on network
 * threads and need the current token without suspending.
 */
@Singleton
class TokenStore @Inject constructor(
    @ApplicationContext context: Context,
) {
    private val prefs: SharedPreferences =
        context.getSharedPreferences("session", Context.MODE_PRIVATE)

    private val crypto = KeystoreCrypto(KEY_ALIAS)

    var accessToken: String?
        get() = prefs.getString(KEY_ACCESS_TOKEN, null)?.let(crypto::decrypt)
        set(value) {
            val encrypted = value?.let(crypto::encrypt)
            prefs.edit().apply {
                if (encrypted == null) remove(KEY_ACCESS_TOKEN) else putString(KEY_ACCESS_TOKEN, encrypted)
            }.apply()
        }

    fun clear() {
        prefs.edit().remove(KEY_ACCESS_TOKEN).apply()
    }

    /** Encrypts a value for callers sharing this store, such as the cookie jar. */
    internal fun protect(value: String): String? = crypto.encrypt(value)

    internal fun reveal(value: String): String? = crypto.decrypt(value)

    internal fun preferences(): SharedPreferences = prefs

    private companion object {
        const val KEY_ACCESS_TOKEN = "access_token"
        const val KEY_ALIAS = "storage_labels_session"
    }
}
