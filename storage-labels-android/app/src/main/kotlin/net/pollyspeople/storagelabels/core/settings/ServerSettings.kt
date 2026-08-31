package net.pollyspeople.storagelabels.core.settings

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

private val Context.settingsDataStore: DataStore<Preferences> by preferencesDataStore(name = "server_settings")

data class ServerConfig(
    /** Base URL of the user's own server, e.g. https://storage.example.net — never a default. */
    val baseUrl: String? = null,
    val allowCleartext: Boolean = false,
) {
    val isConfigured: Boolean get() = !baseUrl.isNullOrBlank()
}

/**
 * Storage Labels is self-hosted, so there is no built-in server address: the user supplies one
 * on first run and it can change later (moving box, new domain, LAN vs. remote).
 */
@Singleton
class ServerSettings @Inject constructor(
    @ApplicationContext private val context: Context,
) {
    private val baseUrlKey = stringPreferencesKey("base_url")
    private val allowCleartextKey = booleanPreferencesKey("allow_cleartext")

    val config: Flow<ServerConfig> = context.settingsDataStore.data.map { prefs ->
        ServerConfig(
            baseUrl = prefs[baseUrlKey],
            allowCleartext = prefs[allowCleartextKey] ?: false,
        )
    }

    suspend fun setServer(baseUrl: String, allowCleartext: Boolean) {
        context.settingsDataStore.edit { prefs ->
            prefs[baseUrlKey] = baseUrl
            prefs[allowCleartextKey] = allowCleartext
        }
    }

    suspend fun clear() {
        context.settingsDataStore.edit { prefs ->
            prefs.remove(baseUrlKey)
            prefs.remove(allowCleartextKey)
        }
    }

    companion object {
        /**
         * Accepts what a person would actually type ("storage.example.net", with or without a
         * scheme or trailing slash) and returns a normalised base URL, or null if it can't be
         * made into one.
         */
        fun normalizeUrl(input: String): String? {
            val trimmed = input.trim()
            if (trimmed.isEmpty()) return null

            val hasScheme = trimmed.contains("://")
            val scheme = if (hasScheme) trimmed.substringBefore("://").lowercase() else "https"
            if (scheme != "http" && scheme != "https") return null

            val rest = (if (hasScheme) trimmed.substringAfter("://") else trimmed)
                .substringBefore("?")
                .substringBefore("#")
                .trimEnd('/')
            if (rest.isEmpty()) return null

            val authority = rest.substringBefore('/')
            val host = authority.substringBefore(':')
            val port = authority.substringAfter(':', missingDelimiterValue = "")
            if (!host.matches(HOST_PATTERN)) return null
            if (port.isNotEmpty() && port.toIntOrNull() !in 1..65535) return null

            return "$scheme://$rest"
        }

        private val HOST_PATTERN = Regex("[A-Za-z0-9._-]+")
    }
}
