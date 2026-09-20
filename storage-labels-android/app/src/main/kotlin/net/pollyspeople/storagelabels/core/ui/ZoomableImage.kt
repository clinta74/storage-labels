package net.pollyspeople.storagelabels.core.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.gestures.detectTransformGestures
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties

/** Past this the pan clamp has nothing left to hold on to, and it stops being a photo. */
private const val MAX_SCALE = 5f

/** What a double-tap settles on: close enough to read a label, far enough to still place it. */
private const val DOUBLE_TAP_SCALE = 2.5f

/**
 * A photo that opens full-screen when tapped, for when the inline frame is too small to make
 * out what is actually in the box. Inline it behaves exactly like [AuthenticatedImage] with
 * [ContentScale.Fit] — the whole picture, at the size the screen gave it.
 *
 * Nothing is clickable when there is no photo to open, or when the user has images turned
 * off: a viewer that opens onto the same placeholder is a door to nowhere.
 */
@Composable
fun ZoomableAuthenticatedImage(
    url: String?,
    contentDescription: String?,
    showImages: Boolean,
    modifier: Modifier = Modifier,
) {
    var open by remember { mutableStateOf(false) }
    val canOpen = showImages && !url.isNullOrBlank()

    AuthenticatedImage(
        url = url,
        contentDescription = contentDescription,
        showImages = showImages,
        contentScale = ContentScale.Fit,
        modifier = if (canOpen) {
            modifier.clickable(onClickLabel = "View full screen") { open = true }
        } else {
            modifier
        },
    )

    if (open && canOpen) {
        ZoomableImageDialog(
            url = url,
            contentDescription = contentDescription,
            onDismiss = { open = false },
        )
    }
}

/**
 * The photo on its own, over everything else, with pinch to zoom, double-tap to jump, and a
 * drag to move around once there is somewhere to move to. Back or the close button leaves.
 */
@Composable
private fun ZoomableImageDialog(
    url: String?,
    contentDescription: String?,
    onDismiss: () -> Unit,
) {
    Dialog(
        onDismissRequest = onDismiss,
        // The point is the whole screen; the default dialog inset would waste most of it.
        properties = DialogProperties(usePlatformDefaultWidth = false),
    ) {
        var scale by remember { mutableFloatStateOf(1f) }
        var offset by remember { mutableStateOf(Offset.Zero) }
        var size by remember { mutableStateOf(IntSize.Zero) }

        // Panning is only allowed as far as the zoom actually created somewhere to go, so a
        // photo can never be flung off the screen and lost.
        fun clamp(candidate: Offset, atScale: Float): Offset {
            val maxX = (size.width * (atScale - 1f)) / 2f
            val maxY = (size.height * (atScale - 1f)) / 2f
            return Offset(
                candidate.x.coerceIn(-maxX, maxX),
                candidate.y.coerceIn(-maxY, maxY),
            )
        }

        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Black.copy(alpha = 0.92f))
                .onSizeChanged { size = it },
            contentAlignment = Alignment.Center,
        ) {
            AuthenticatedImage(
                url = url,
                contentDescription = contentDescription,
                showImages = true,
                contentScale = ContentScale.Fit,
                modifier = Modifier
                    .fillMaxSize()
                    .pointerInput(Unit) {
                        detectTransformGestures { _, pan, zoom, _ ->
                            val next = (scale * zoom).coerceIn(1f, MAX_SCALE)
                            scale = next
                            // Back at rest the photo belongs in the middle again, whatever
                            // the pinch was doing on the way down.
                            offset = if (next == 1f) Offset.Zero else clamp(offset + pan, next)
                        }
                    }
                    .pointerInput(Unit) {
                        detectTapGestures(
                            onDoubleTap = { tap ->
                                if (scale > 1f) {
                                    scale = 1f
                                    offset = Offset.Zero
                                } else {
                                    // Zoom towards what was tapped rather than the middle,
                                    // so the thing being inspected is what comes closer.
                                    val centre = Offset(size.width / 2f, size.height / 2f)
                                    scale = DOUBLE_TAP_SCALE
                                    offset = clamp(
                                        -(tap - centre) * DOUBLE_TAP_SCALE,
                                        DOUBLE_TAP_SCALE,
                                    )
                                }
                            },
                        )
                    }
                    .graphicsLayer {
                        scaleX = scale
                        scaleY = scale
                        translationX = offset.x
                        translationY = offset.y
                    },
            )

            IconButton(
                onClick = onDismiss,
                modifier = Modifier
                    .align(Alignment.TopEnd)
                    .padding(8.dp),
            ) {
                Icon(Icons.Filled.Close, contentDescription = "Close", tint = Color.White)
            }
        }
    }
}
