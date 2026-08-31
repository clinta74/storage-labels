package net.pollyspeople.storagelabels.core.ui

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp

/**
 * The leading square on a list row. The web app puts a generic icon avatar here; a phone has
 * the room to show the actual photo, which is far easier to recognise when you are looking
 * for one box among thirty. Falls back to the icon when there is no photo, or when the
 * "show images" preference is off.
 */
@Composable
fun RowThumbnail(
    imageUrl: String?,
    contentDescription: String?,
    showImages: Boolean,
    fallbackIcon: ImageVector,
    modifier: Modifier = Modifier,
    size: androidx.compose.ui.unit.Dp = 48.dp,
) {
    val shape = RoundedCornerShape(8.dp)

    if (!imageUrl.isNullOrBlank() && showImages) {
        AuthenticatedImage(
            url = imageUrl,
            contentDescription = contentDescription,
            showImages = true,
            modifier = modifier
                .size(size)
                .clip(shape),
        )
        return
    }

    Surface(
        color = MaterialTheme.colorScheme.secondaryContainer,
        contentColor = MaterialTheme.colorScheme.onSecondaryContainer,
        shape = shape,
        modifier = modifier.size(size),
    ) {
        Box(contentAlignment = Alignment.Center) {
            Icon(fallbackIcon, contentDescription = null)
        }
    }
}
