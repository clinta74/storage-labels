package net.pollyspeople.storagelabels.feature.boxes

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Label
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.automirrored.filled.DriveFileMove
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
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
import net.pollyspeople.storagelabels.core.ui.FormattedCode
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.core.ui.RowThumbnail
import net.pollyspeople.storagelabels.core.ui.MenuAction
import net.pollyspeople.storagelabels.core.ui.OverflowMenu
import net.pollyspeople.storagelabels.feature.search.InlineSearchBar
import net.pollyspeople.storagelabels.data.dto.Item

@Composable
fun BoxDetailScreen(
    onEditBox: () -> Unit,
    onOpenBox: (locationId: Long, boxId: String) -> Unit,
    onAddItem: () -> Unit,
    onEditItem: (String) -> Unit,
    onDeleted: () -> Unit,
    onMoved: (Long) -> Unit,
    onMessage: (String) -> Unit,
    viewModel: BoxDetailViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    // Loads on first show and again whenever the screen comes back to the front, so a
    // box or item added on a pushed screen is there when you return.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }

    var itemToDelete by remember { mutableStateOf<Item?>(null) }
    var deletingBox by remember { mutableStateOf(false) }
    var moving by remember { mutableStateOf(false) }
    var viewing by remember { mutableStateOf<Item?>(null) }

    val box = state.box

    if (state.loading && box == null) {
        LoadingBox()
        return
    }
    if (box == null) {
        ErrorBanner(state.error ?: "This box couldn't be loaded.", onRetry = viewModel::refresh)
        return
    }

    Column(Modifier.fillMaxSize()) {
        InlineSearchBar(onOpenBox = onOpenBox)

    Box(Modifier.fillMaxSize()) {
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 8.dp, bottom = 88.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            item {
                Row(verticalAlignment = Alignment.Top, modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.weight(1f)) {
                        Text(box.name, style = MaterialTheme.typography.headlineSmall)
                        FormattedCode(
                            code = box.code,
                            pattern = state.codeColorPattern,
                            style = MaterialTheme.typography.titleMedium,
                        )
                        if (!box.description.isNullOrBlank()) {
                            Text(box.description, style = MaterialTheme.typography.bodyMedium)
                        }
                    }

                    if (state.canEdit) {
                        OverflowMenu(
                            contentDescription = "Actions for this box",
                            actions = listOf(
                                MenuAction("Edit", onEditBox, Icons.Filled.Edit),
                                MenuAction(
                                    "Move to another location",
                                    {
                                        viewModel.loadMoveTargets()
                                        moving = true
                                    },
                                    Icons.AutoMirrored.Filled.DriveFileMove,
                                ),
                                MenuAction(
                                    "Delete box",
                                    { deletingBox = true },
                                    Icons.Filled.Delete,
                                    destructive = true,
                                ),
                            ),
                        )
                    }
                }
            }

            if (!box.imageUrl.isNullOrBlank()) {
                item {
                    AuthenticatedImage(
                        url = box.imageUrl,
                        contentDescription = "Photo of ${box.name}",
                        showImages = state.showImages,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(220.dp),
                    )
                }
            }

            item { HorizontalDivider() }

            item {
                Text(
                    "Contents (${state.items.size})",
                    style = MaterialTheme.typography.titleMedium,
                )
            }

            if (state.items.isEmpty()) {
                item {
                    EmptyState(
                        title = "Nothing listed yet",
                        message = "Add what's inside so you can find it by searching later.",
                        actionLabel = if (state.canEdit) "Add an item" else null,
                        onAction = if (state.canEdit) onAddItem else null,
                        modifier = Modifier.height(200.dp),
                    )
                }
            } else {
                items(state.items, key = { it.itemId }) { item ->
                    ItemRow(
                        item = item,
                        canEdit = state.canEdit,
                        showImages = state.showImages,
                        onOpen = { viewing = item },
                        onEdit = { onEditItem(item.itemId) },
                        onDelete = { itemToDelete = item },
                    )
                }
            }
        }

        if (state.canEdit) {
            FloatingActionButton(
                onClick = onAddItem,
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(16.dp),
            ) {
                Icon(Icons.Filled.Add, contentDescription = "Add item")
            }
        }
    }
    }

    viewing?.let { item ->
        AlertDialog(
            onDismissRequest = { viewing = null },
            title = { Text(item.name) },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text(item.description?.takeIf(String::isNotBlank) ?: "No description.")
                    if (!item.imageUrl.isNullOrBlank()) {
                        AuthenticatedImage(
                            url = item.imageUrl,
                            contentDescription = "Photo of ${item.name}",
                            showImages = state.showImages,
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(180.dp),
                        )
                    }
                }
            },
            confirmButton = { TextButton(onClick = { viewing = null }) { Text("Close") } },
        )
    }

    itemToDelete?.let { item ->
        ConfirmDeleteDialog(
            title = "Delete ${item.name}?",
            message = "This removes the item from this box.",
            onConfirm = {
                viewModel.deleteItem(item, onMessage)
                itemToDelete = null
            },
            onDismiss = { itemToDelete = null },
        )
    }

    if (deletingBox) {
        ConfirmDeleteDialog(
            title = "Delete ${box.name}?",
            message = "The box and its code go, along with anything listed inside it.",
            forceLabel = "Also delete the items inside".takeIf { state.items.isNotEmpty() },
            onConfirm = { force ->
                deletingBox = false
                viewModel.deleteBox(force) { message ->
                    onMessage(message)
                    onDeleted()
                }
            },
            onDismiss = { deletingBox = false },
        )
    }

    if (moving) {
        AlertDialog(
            onDismissRequest = { moving = false },
            title = { Text("Move ${box.name}") },
            text = {
                if (state.moveTargets.isEmpty()) {
                    Text("There's nowhere else you can put this box — you need edit access to another location.")
                } else {
                    Column {
                        state.moveTargets.forEach { target ->
                            Text(
                                target.name,
                                style = MaterialTheme.typography.bodyLarge,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .clickable {
                                        moving = false
                                        viewModel.moveBox(target.locationId) { destination, message ->
                                            onMessage(message)
                                            onMoved(destination)
                                        }
                                    }
                                    .padding(vertical = 12.dp),
                            )
                        }
                    }
                }
            },
            confirmButton = { TextButton(onClick = { moving = false }) { Text("Cancel") } },
        )
    }
}

@Composable
private fun ItemRow(
    item: Item,
    canEdit: Boolean,
    showImages: Boolean,
    onOpen: () -> Unit,
    onEdit: () -> Unit,
    onDelete: () -> Unit,
) {
    Card(modifier = Modifier.fillMaxWidth().clickable(onClick = onOpen)) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(start = 16.dp, top = 8.dp, bottom = 8.dp, end = 4.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            RowThumbnail(
                imageUrl = item.imageUrl,
                contentDescription = "Photo of ${item.name}",
                showImages = showImages,
                fallbackIcon = Icons.AutoMirrored.Filled.Label,
                size = 40.dp,
            )

            Column(Modifier.weight(1f)) {
                Text(item.name, style = MaterialTheme.typography.bodyLarge)
                if (!item.description.isNullOrBlank()) {
                    Text(
                        item.description,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
            if (canEdit) {
                OverflowMenu(
                    contentDescription = "Actions for ${item.name}",
                    actions = listOf(
                        MenuAction("Edit", onEdit, Icons.Filled.Edit),
                        MenuAction("Delete", onDelete, Icons.Filled.Delete, destructive = true),
                    ),
                )
            }
        }
    }
}
