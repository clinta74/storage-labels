package net.pollyspeople.storagelabels.core.code

import net.pollyspeople.storagelabels.core.code.CodeColorPattern.SegmentColor
import org.junit.Assert.assertEquals
import org.junit.Test

/**
 * These cases are taken from the behaviour of the web app's parser, which this port has to
 * match exactly — the same pattern must colour a code identically in both clients, and on
 * printed labels.
 */
class CodeColorPatternTest {

    @Test
    fun `no pattern leaves the code uncoloured`() {
        assertEquals(
            listOf(CodeColorPattern.Segment("ABC123", SegmentColor.Default)),
            CodeColorPattern.parse(null, "ABC123"),
        )
        assertEquals(
            listOf(CodeColorPattern.Segment("ABC123", SegmentColor.Default)),
            CodeColorPattern.parse("", "ABC123"),
        )
    }

    @Test
    fun `splits fixed length segments in order`() {
        val segments = CodeColorPattern.parse("3:primary,3:error", "ABC123")

        assertEquals(
            listOf(
                CodeColorPattern.Segment("ABC", SegmentColor.Primary),
                CodeColorPattern.Segment("123", SegmentColor.Error),
            ),
            segments,
        )
    }

    @Test
    fun `the wildcard absorbs the middle and later segments still land`() {
        // The documented example from the preferences screen.
        val segments = CodeColorPattern.parse("3:primary,2:secondary,*,4:error", "ABCDE-MIDDLE-WXYZ")

        assertEquals(
            listOf(
                CodeColorPattern.Segment("ABC", SegmentColor.Primary),
                CodeColorPattern.Segment("DE", SegmentColor.Secondary),
                CodeColorPattern.Segment("-MIDDLE-", SegmentColor.Default),
                CodeColorPattern.Segment("WXYZ", SegmentColor.Error),
            ),
            segments,
        )
    }

    @Test
    fun `anything past the pattern is appended uncoloured`() {
        val segments = CodeColorPattern.parse("2:primary", "ABCDEF")

        assertEquals(
            listOf(
                CodeColorPattern.Segment("AB", SegmentColor.Primary),
                CodeColorPattern.Segment("CDEF", SegmentColor.Default),
            ),
            segments,
        )
    }

    @Test
    fun `a segment longer than what remains is dropped rather than truncated`() {
        val segments = CodeColorPattern.parse("2:primary,10:error", "ABCD")

        assertEquals(
            listOf(
                CodeColorPattern.Segment("AB", SegmentColor.Primary),
                CodeColorPattern.Segment("CD", SegmentColor.Default),
            ),
            segments,
        )
    }

    @Test
    fun `an unknown colour name falls back to no colour`() {
        val segments = CodeColorPattern.parse("3:chartreuse", "ABC123")

        assertEquals(SegmentColor.Default, segments.first().color)
    }

    @Test
    fun `a wildcard with nothing left to absorb adds no segment`() {
        val segments = CodeColorPattern.parse("3:primary,*,3:error", "ABC123")

        assertEquals(
            listOf(
                CodeColorPattern.Segment("ABC", SegmentColor.Primary),
                CodeColorPattern.Segment("123", SegmentColor.Error),
            ),
            segments,
        )
    }

    @Test
    fun `a malformed pattern degrades to the plain code`() {
        assertEquals(
            listOf(CodeColorPattern.Segment("ABC123", SegmentColor.Default)),
            CodeColorPattern.parse("not-a-pattern", "ABC123"),
        )
    }

    @Test
    fun `an empty code stays empty`() {
        assertEquals(emptyList<CodeColorPattern.Segment>(), CodeColorPattern.parse("3:primary", ""))
    }
}
