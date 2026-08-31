package net.pollyspeople.storagelabels.feature.auth

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

class RegisterValidationTest {

    private val valid = RegisterState(
        email = "sam@example.net",
        username = "sam",
        firstName = "Sam",
        lastName = "Rivers",
        password = "correct horse",
        confirmPassword = "correct horse",
    )

    @Test
    fun `accepts a complete form`() {
        assertTrue(RegisterViewModel.validate(valid).isEmpty())
    }

    @Test
    fun `requires every field the api requires`() {
        val errors = RegisterViewModel.validate(RegisterState())

        assertEquals(
            setOf(
                RegisterViewModel.FIELD_EMAIL,
                RegisterViewModel.FIELD_USERNAME,
                RegisterViewModel.FIELD_FIRST_NAME,
                RegisterViewModel.FIELD_LAST_NAME,
                RegisterViewModel.FIELD_PASSWORD,
            ),
            errors.keys,
        )
    }

    @Test
    fun `rejects an address that is not an email`() {
        val errors = RegisterViewModel.validate(valid.copy(email = "sam-at-example"))

        assertTrue(errors.containsKey(RegisterViewModel.FIELD_EMAIL))
    }

    @Test
    fun `catches mismatched passwords before they reach the server`() {
        val errors = RegisterViewModel.validate(valid.copy(confirmPassword = "something else"))

        assertEquals("Passwords don't match.", errors[RegisterViewModel.FIELD_CONFIRM])
    }

    @Test
    fun `leaves password strength to the server`() {
        // Complexity rules are configurable per deployment, so a short password is not
        // rejected here — the API answers with a 400 that the screen shows.
        val errors = RegisterViewModel.validate(valid.copy(password = "a", confirmPassword = "a"))

        assertTrue(errors.isEmpty())
    }
}
