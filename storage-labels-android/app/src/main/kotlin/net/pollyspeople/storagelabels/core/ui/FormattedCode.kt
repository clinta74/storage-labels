package net.pollyspeople.storagelabels.core.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.withStyle
import net.pollyspeople.storagelabels.core.code.CodeColorPattern
import net.pollyspeople.storagelabels.core.code.CodeColorPattern.SegmentColor

/**
 * Renders a box code with the user's colour pattern applied, as the web app's FormattedCode
 * does. Monospaced, because these are meant to be compared against a printed label.
 */
@Composable
fun FormattedCode(
    code: String,
    pattern: String?,
    modifier: Modifier = Modifier,
    style: TextStyle = MaterialTheme.typography.bodyMedium,
) {
    val segments = CodeColorPattern.parse(pattern, code)

    val text = buildAnnotatedString {
        segments.forEach { segment ->
            val color = segment.color.toComposeColor()
            withStyle(
                SpanStyle(
                    color = color ?: Color.Unspecified,
                    fontWeight = if (color != null) FontWeight.SemiBold else FontWeight.Normal,
                ),
            ) {
                append(segment.text)
            }
        }
    }

    Text(text = text, style = style.copy(fontFamily = FontFamily.Monospace), modifier = modifier)
}

/**
 * Maps the web palette's semantic names onto this app's scheme. "Default" returns null so the
 * text keeps the surrounding colour.
 */
@Composable
private fun SegmentColor.toComposeColor(): Color? = when (this) {
    SegmentColor.Primary -> MaterialTheme.colorScheme.primary
    SegmentColor.Secondary -> MaterialTheme.colorScheme.secondary
    SegmentColor.Error -> MaterialTheme.colorScheme.error
    SegmentColor.Warning -> WarningColor
    SegmentColor.Info -> MaterialTheme.colorScheme.tertiary
    SegmentColor.Success -> SuccessColor
    SegmentColor.Default -> null
}

// Material 3 has no warning/success roles; these match the web theme's intent.
private val WarningColor = Color(0xFFB26A00)
private val SuccessColor = Color(0xFF2E7D32)
