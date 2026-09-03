package net.pollyspeople.storagelabels.core.auth

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import java.util.Base64

class JwtClaimsTest {

    @Test
    fun `reads repeated permission claims as a list`() {
        // JwtTokenService adds one "permission" claim per permission; System.IdentityModel
        // serialises repeats as a JSON array.
        val token = tokenWithPayload(
            """{"permission":["read:user","write:user"],"exp":1893456000}""",
        )

        assertEquals(listOf("read:user", "write:user"), JwtClaims.permissions(token))
    }

    @Test
    fun `reads a single permission claim that is not an array`() {
        // One permission collapses to a bare string rather than a one-element array.
        val token = tokenWithPayload("""{"permission":"read:user"}""")

        assertEquals(listOf("read:user"), JwtClaims.permissions(token))
    }

    @Test
    fun `returns empty when the token carries no permissions`() {
        val token = tokenWithPayload("""{"sub":"abc","exp":1893456000}""")

        assertTrue(JwtClaims.permissions(token).isEmpty())
    }

    @Test
    fun `reads roles from both the short and schema claim names`() {
        val token = tokenWithPayload(
            """{"role":"Admin","http://schemas.microsoft.com/ws/2008/06/identity/claims/role":["Admin","Auditor"]}""",
        )

        assertEquals(listOf("Admin", "Auditor"), JwtClaims.roles(token))
    }

    @Test
    fun `reads the expiry`() {
        val token = tokenWithPayload("""{"exp":1893456000}""")

        assertEquals(1893456000L, JwtClaims.expiresAt(token))
    }

    @Test
    fun `survives payloads whose length is not a multiple of four`() {
        // JWT segments drop base64 padding; a strict decoder would throw here.
        val token = tokenWithPayload("""{"permission":"read:user","sub":"a"}""")

        assertEquals(listOf("read:user"), JwtClaims.permissions(token))
    }

    @Test
    fun `returns nothing for malformed tokens instead of throwing`() {
        assertTrue(JwtClaims.permissions("not-a-token").isEmpty())
        assertTrue(JwtClaims.permissions("").isEmpty())
        assertTrue(JwtClaims.permissions("aa.!!!not-base64!!!.cc").isEmpty())
        assertNull(JwtClaims.expiresAt("not-a-token"))
    }

    private fun tokenWithPayload(payloadJson: String): String {
        val encoder = Base64.getUrlEncoder().withoutPadding()
        val header = encoder.encodeToString("""{"alg":"HS256","typ":"JWT"}""".toByteArray())
        val payload = encoder.encodeToString(payloadJson.toByteArray())
        return "$header.$payload.signature-not-verified"
    }
}
