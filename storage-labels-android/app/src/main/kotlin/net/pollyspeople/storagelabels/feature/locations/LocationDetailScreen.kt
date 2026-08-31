package net.pollyspeople.storagelabels.feature.locations

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Badge
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
import net.pollyspeople.storagelabels.core.ui.ConfirmDeleteDialog
import net.pollyspeople.storagelabels.core.ui.EmptyState
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.core.ui.FormattedCode
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.core.ui.MenuAction
import net.pollyspeople.storagelabels.core.ui.OverflowMenu
import net.pollyspeople.storagelabels.feature.search.InlineSearchBar
import net.pollyspeople.storagelabels.data.dto.Box as BoxDto

@Composable
fun LocationDetailScreen(
    onOpenBox: (String) -> Unit,
    onOpenSearchResult: (locationId: Long, boxId: String) -> Unit,
    onAddBox: () -> Unit,
    onMessage: (String) -> Unit,
    viewModel: LocationDetailViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    // Loads on first show and again whenever the screen comes back to the front, so a
    // box or item added on a pushed screen is there when you return.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }
    var deleting by remember { mutableStateOf<BoxDto?>(null) }
    val canEdit = state.location?.accessLevel?.canEdit == true

    Column(Modifier.fillMaxSize()) {
        InlineSearchBar(onOpenBox = onOpenSearchResult)

        // The screen has to say which location you are standing in; the app bar only has
        // room for the section name.
        state.location?.let { location ->
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.padding(start = 16.dp, end = 16.dp, bottom = 4.dp),
            ) {
                Text(location.name, style = MaterialTheme.typography.headlineSmall)
                AssistChip(onClick = {}, label = { Text(location.accessLevel.name) })
            }
        }

    Box(Modifier.fillMaxSize()) {
        when {
            state.loading -> LoadingBox()

            state.error != null && state.boxes.isEmpty() ->
                ErrorBanner(state.error!!, onRetry = viewModel::refresh)

            state.boxes.isEmpty() -> EmptyState(
                title = "No boxes here yet",
                message = "Add a box, give it a code, and start listing what's inside.",
                actionLabel = if (canEdit) "Add a box" else null,
                onAction = if (canEdit) onAddBox else null,
            )

            else -> LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 8.dp, bottom = 88.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                items(state.boxes, key = { it.boxId }) { box ->
                    BoxCard(
                        box = box,
                        itemCount = state.itemCounts[box.boxId],
                        codeColorPattern = state.codeColorPattern,
                        canEdit = canEdit,
                        onOpen = { onOpenBox(box.boxId) },
                        onDelete = { deleting = box },
                    )
                }
            }
        }

        if (canEdit) {
            FloatingActionButton(
                onClick = onAddBox,
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(16.dp),
            ) {
                Icon(Icons.Filled.Add, contentDescription = "Add box")
            }
        }
    }
    }

    deleting?.let { box ->
        ConfirmDeleteDialog(
            title = "Delete ${box.name}?",
            message = "This removes the box and its code. Items inside it go too.",
            forceLabel = "Also delete the items inside".takeIf { (state.itemCounts[box.boxId] ?: 0) > 0 },
            onConfirm = { force ->
                viewModel.deleteBox(box, force) { onMessage(it) }
                deleting = null
            },
            onDismiss = { deleting = null },
        )
    }
}

@Composable
private fun BoxCard(
    box: BoxDto,
    itemCount: Int?,
    codeColorPattern: String,
    canEdit: Boolean,
    onOpen: () -> Unit,
    onDelete: () -> Unit,
) {
    Card(modifier = Modifier.fillMaxWidth().clickable(onClick = onOpen)) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(start = 16.dp, top = 12.dp, bottom = 12.dp, end = 4.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Column(Modifier.weight(1f)) {
                Text(box.name, style = MaterialTheme.typography.titleMedium)
                FormattedCode(
                    code = box.code,
                    pattern = codeColorPattern,
                    style = MaterialTheme.typography.bodySmall,
                )
                if (!box.description.isNullOrBlank()) {
                    Text(
                        box.description,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }

            if (itemCount != null) {
                Badge { Text("$itemCount") }
            }

            if (canEdit) {
                OverflowMenu(
                    contentDescription = "Actions for ${box.name}",
                    actions = listOf(
                        MenuAction("Open", onOpen),
                        MenuAction("Delete", onDelete, Icons.Filled.Delete, destructive = true),
                    ),
                )
            }
        }
    }
}
