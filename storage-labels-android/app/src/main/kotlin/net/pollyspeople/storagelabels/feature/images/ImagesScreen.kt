package net.pollyspeople.storagelabels.feature.images

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AddAPhoto
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.Card
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.ui.AuthenticatedImage
import net.pollyspeople.storagelabels.core.ui.ConfirmDeleteDialog
import net.pollyspeople.storagelabels.core.ui.EmptyState
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.core.ui.MenuAction
import net.pollyspeople.storagelabels.core.ui.OverflowMenu
import net.pollyspeople.storagelabels.data.dto.ImageMetadata

@Composable
fun ImagesScreen(
    onAddPhoto: () -> Unit,
    onMessage: (String) -> Unit,
    viewModel: ImagesViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    // Loads on first show and again whenever the screen comes back to the front, so a
    // box or item added on a pushed screen is there when you return.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }
    var deleting by remember { mutableStateOf<ImageMetadata?>(null) }

    Box(Modifier.fillMaxSize()) {
        when {
            state.loading -> LoadingBox()

            state.error != null && state.images.isEmpty() ->
                ErrorBanner(state.error!!, onRetry = viewModel::refresh)

            state.images.isEmpty() -> EmptyState(
                title = "No photos yet",
                message = "Photos you attach to boxes and items collect here.",
                actionLabel = "Take a photo",
                onAction = onAddPhoto,
            )

            else -> LazyVerticalGrid(
                columns = GridCells.Adaptive(minSize = 140.dp),
                contentPadding = PaddingValues(start = 12.dp, end = 12.dp, top = 12.dp, bottom = 88.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.fillMaxSize(),
            ) {
                items(state.images, key = { it.imageId }) { image ->
                    ImageCard(
                        image = image,
                        showImages = state.showImages,
                        onDelete = { deleting = image },
                    )
                }
            }
        }

        FloatingActionButton(
            onClick = onAddPhoto,
            modifier = Modifier
                .align(Alignment.BottomEnd)
                .padding(16.dp),
        ) {
            Icon(Icons.Filled.AddAPhoto, contentDescription = "Add a photo")
        }
    }

    deleting?.let { image ->
        ConfirmDeleteDialog(
            title = "Delete this photo?",
            message = if (image.isReferenced) {
                "It's used by ${image.boxReferenceCount} box(es) and ${image.itemReferenceCount} item(s). " +
                    "They'll be left without a photo."
            } else {
                "This photo isn't used anywhere."
            },
            // Only an in-use photo needs the extra confirmation, matching the API's
            // separate force-delete route.
            forceLabel = "Delete it anyway".takeIf { image.isReferenced },
            onConfirm = { force ->
                viewModel.delete(image, force || image.isReferenced) { onMessage(it) }
                deleting = null
            },
            onDismiss = { deleting = null },
        )
    }
}

@Composable
private fun ImageCard(
    image: ImageMetadata,
    showImages: Boolean,
    onDelete: () -> Unit,
) {
    Card {
        Column {
            AuthenticatedImage(
                url = image.url,
                contentDescription = image.fileName,
                showImages = showImages,
                modifier = Modifier
                    .fillMaxWidth()
                    .aspectRatio(1f),
            )
            androidx.compose.foundation.layout.Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(start = 8.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(Modifier.weight(1f)) {
                    Text(
                        formatSize(image.sizeInBytes),
                        style = MaterialTheme.typography.labelSmall,
                    )
                    if (image.isReferenced) {
                        Text(
                            "In use",
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.primary,
                        )
                    }
                }
                OverflowMenu(
                    contentDescription = "Actions for this photo",
                    actions = listOf(
                        MenuAction("Delete", onDelete, Icons.Filled.Delete, destructive = true),
                    ),
                )
            }
        }
    }
}

private fun formatSize(bytes: Long): String = when {
    bytes >= 1_000_000 -> "%.1f MB".format(bytes / 1_000_000.0)
    bytes >= 1_000 -> "${bytes / 1_000} KB"
    else -> "$bytes B"
}
