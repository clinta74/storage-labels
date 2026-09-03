package net.pollyspeople.storagelabels.feature.legal

import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * The legal text is carried over from the web app, so what matters is that it arrived
 * complete — an empty section or a dropped bullet would silently change what the app tells
 * people about their data.
 */
class LegalContentTest {

    @Test
    fun `privacy notice carries every section from the web app`() {
        assertEquals(11, PrivacyNotice.sections.size)
        assertEquals("Important: Self-Hosted Software", PrivacyNotice.sections.first().heading)
    }

    @Test
    fun `terms carry all ten numbered clauses plus the closing sections`() {
        val headings = TermsAndConditions.sections.map { it.heading }

        (1..10).forEach { clause ->
            assertTrue(
                "Clause $clause is missing",
                headings.any { it.startsWith("$clause.") },
            )
        }
    }

    @Test
    fun `no section is empty`() {
        (PrivacyNotice.sections + TermsAndConditions.sections).forEach { section ->
            assertTrue("${section.heading} has no content", section.blocks.isNotEmpty())
        }
    }

    @Test
    fun `no block is blank`() {
        (PrivacyNotice.sections + TermsAndConditions.sections)
            .flatMap { it.blocks }
            .forEach { block ->
                val text = when (block) {
                    is LegalBlock.Paragraph -> block.text
                    is LegalBlock.Bullet -> block.text
                }
                assertTrue("Blank block found", text.isNotBlank())
            }
    }

    @Test
    fun `the self-hosted disclaimer survived the port`() {
        val allText = PrivacyNotice.sections
            .flatMap { it.blocks }
            .joinToString(" ") {
                when (it) {
                    is LegalBlock.Paragraph -> it.text
                    is LegalBlock.Bullet -> it.text
                }
            }

        assertTrue(allText.contains("do not collect, store, or have access to any of your data"))
    }
}
