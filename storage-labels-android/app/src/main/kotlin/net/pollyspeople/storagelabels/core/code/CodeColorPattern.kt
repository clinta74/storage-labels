package net.pollyspeople.storagelabels.core.code

/**
 * Box codes can be shown with coloured segments so a label is recognisable at a glance.
 * The pattern is a user preference shared with the web app, e.g.
 *
 *     3:primary,2:secondary,*,4:error
 *
 * meaning: first three characters primary, next two secondary, an uncoloured middle of
 * whatever length is left over, last four error.
 *
 * The web app implements this twice (formatted-code.tsx and label-item.tsx); this is the
 * single Kotlin port of that behaviour, including its quirks:
 *  - a segment that would run past the end of the code is dropped, not truncated;
 *  - anything left over after the pattern is appended uncoloured;
 *  - a malformed pattern degrades to the plain code rather than throwing.
 */
object CodeColorPattern {

    enum class SegmentColor {
        Primary, Secondary, Error, Warning, Info, Success, Default;

        companion object {
            fun from(name: String?): SegmentColor = when (name?.trim()?.lowercase()) {
                "primary" -> Primary
                "secondary" -> Secondary
                "error" -> Error
                "warning" -> Warning
                "info" -> Info
                "success" -> Success
                else -> Default
            }
        }
    }

    data class Segment(val text: String, val color: SegmentColor)

    fun parse(pattern: String?, code: String): List<Segment> {
        if (pattern.isNullOrBlank()) return listOf(Segment(code, SegmentColor.Default))

        return runCatching { parseOrThrow(pattern, code) }
            .getOrElse { listOf(Segment(code, SegmentColor.Default)) }
    }

    private fun parseOrThrow(pattern: String, code: String): List<Segment> {
        val segments = mutableListOf<Segment>()
        val parts = pattern.split(",")
        var index = 0

        for ((position, rawPart) in parts.withIndex()) {
            val part = rawPart.trim()

            if (part == "*") {
                // The wildcard takes whatever the later fixed-length segments don't need.
                val reserved = parts.drop(position + 1)
                    .map { it.trim() }
                    .filter { it != "*" }
                    .sumOf { it.substringBefore(':').toIntOrNull() ?: 0 }

                val skipLength = code.length - index - reserved
                if (skipLength > 0) {
                    segments += Segment(code.substring(index, index + skipLength), SegmentColor.Default)
                    index += skipLength
                }
                continue
            }

            val length = part.substringBefore(':').toIntOrNull() ?: 0
            val color = SegmentColor.from(part.substringAfter(':', missingDelimiterValue = ""))

            if (index + length <= code.length) {
                segments += Segment(code.substring(index, index + length), color)
                index += length
            }
        }

        if (index < code.length) {
            segments += Segment(code.substring(index), SegmentColor.Default)
        }

        // A zero-length part (a typo, or a pattern with a stray comma) contributes nothing to
        // render; the web app emits an empty span there, which looks the same.
        return segments.filter { it.text.isNotEmpty() }
    }
}
