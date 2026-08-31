package net.pollyspeople.storagelabels.core.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.BrokenImage
import androidx.compose.material.icons.filled.Image
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import coil3.compose.SubcomposeAsyncImage

/**
 * Images on this API are access-controlled, so they load through the app's authenticated
 * OkHttp client rather than a plain fetch. The API returns relative paths
 * (/api/images/{id}); RelativeUrlMapper turns those into absolute ones at load time.
 *
 * [showImages] carries the user's preference: when it's off the app draws a placeholder and
 * downloads nothing, which is the entire point of the setting.
 */
@Composable
fun AuthenticatedImage(
    url: String?,
    contentDescription: String?,
    showImages: Boolean,
    modifier: Modifier = Modifier,
    contentScale: ContentScale = ContentScale.Crop,
) {
    if (!showImages) {
        Placeholder(modifier, Icons.Filled.Image, "Images are off")
        return
    }
    if (url.isNullOrBlank()) {
        Placeholder(modifier, Icons.Filled.Image, "No photo")
        return
    }

    SubcomposeAsyncImage(
        model = url,
        contentDescription = contentDescription,
        contentScale = contentScale,
        modifier = modifier,
        loading = {
            Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        },
        error = {
            Placeholder(Modifier.fillMaxSize(), Icons.Filled.BrokenImage, "Couldn't load this photo")
        },
    )
}

@Composable
private fun Placeholder(
    modifier: Modifier,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
) {
    Surface(
        color = MaterialTheme.colorScheme.surfaceVariant,
        contentColor = MaterialTheme.colorScheme.onSurfaceVariant,
        modifier = modifier,
    ) {
        Column(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Icon(icon, contentDescription = null)
            Text(label, style = MaterialTheme.typography.labelSmall)
        }
    }
}
