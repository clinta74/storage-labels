package net.pollyspeople.storagelabels.core.auth

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.longOrNull
import java.util.Base64

/**
 * The API puts one "permission" claim per permission into the JWT (JwtTokenService), and the
 * web client reads them straight off the token rather than calling an endpoint. We do the same.
 *
 * Only the payload is read. The signature is the server's business — nothing decided here is
 * trusted for authorization, it just decides which menu entries and actions to show.
 *
 * Deliberately free of Android framework types so it runs in plain JVM unit tests.
 */
object JwtClaims {

    private const val ROLE_CLAIM_URI =
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"

    private val json = Json { ignoreUnknownKeys = true }

    fun permissions(token: String): List<String> =
        payload(token)?.let { claimValues(it, "permission") }.orEmpty()

    fun roles(token: String): List<String> =
        payload(token)
            ?.let { claimValues(it, "role") + claimValues(it, ROLE_CLAIM_URI) }
            .orEmpty()
            .distinct()

    /** Expiry in epoch seconds, or null when the token carries none. */
    fun expiresAt(token: String): Long? =
        (payload(token)?.get("exp") as? JsonPrimitive)?.longOrNull

    private fun claimValues(payload: JsonObject, name: String): List<String> =
        when (val raw = payload[name]) {
            is JsonArray -> raw.mapNotNull { (it as? JsonPrimitive)?.contentOrNullIfBlank() }
            is JsonPrimitive -> listOfNotNull(raw.contentOrNullIfBlank())
            else -> emptyList()
        }

    private fun JsonPrimitive.contentOrNullIfBlank(): String? = content.takeIf { it.isNotBlank() }

    private fun payload(token: String): JsonObject? {
        val parts = token.split(".")
        if (parts.size < 2) return null
        return runCatching {
            val decoded = Base64.getUrlDecoder().decode(parts[1].padForBase64())
            json.parseToJsonElement(String(decoded, Charsets.UTF_8)) as? JsonObject
        }.getOrNull()
    }

    /** JWT segments drop base64 padding; the strict JDK decoder wants it back. */
    private fun String.padForBase64(): String = when (length % 4) {
        2 -> this + "=="
        3 -> this + "="
        else -> this
    }
}
