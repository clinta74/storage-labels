package net.pollyspeople.storagelabels.feature.locations

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.People
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Card
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
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
import net.pollyspeople.storagelabels.core.ui.ConfirmDeleteDialog
import net.pollyspeople.storagelabels.core.ui.EmptyState
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.core.ui.MenuAction
import net.pollyspeople.storagelabels.core.ui.OverflowMenu
import net.pollyspeople.storagelabels.feature.search.InlineSearchBar
import net.pollyspeople.storagelabels.data.dto.StorageLocation

@Composable
fun LocationsScreen(
    onOpenLocation: (Long) -> Unit,
    onOpenBox: (locationId: Long, boxId: String) -> Unit,
    onManageUsers: (Long) -> Unit,
    onMessage: (String) -> Unit,
    viewModel: LocationsViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    // Loads on first show and again whenever the screen comes back to the front, so a
    // box or item added on a pushed screen is there when you return.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }

    var editing by remember { mutableStateOf<StorageLocation?>(null) }
    var creating by remember { mutableStateOf(false) }
    var deleting by remember { mutableStateOf<StorageLocation?>(null) }

    Column(Modifier.fillMaxSize()) {
        InlineSearchBar(onOpenBox = onOpenBox)

    Box(Modifier.fillMaxSize()) {
        when {
            state.loading -> LoadingBox()

            state.error != null && state.locations.isEmpty() ->
                ErrorBanner(state.error!!, onRetry = viewModel::refresh)

            state.locations.isEmpty() -> EmptyState(
                title = "No locations yet",
                message = "A location is a place you keep boxes — a garage, a storage unit, an attic.",
                actionLabel = "Add a location",
                onAction = { creating = true },
            )

            else -> LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = androidx.compose.foundation.layout.PaddingValues(
                    start = 16.dp, end = 16.dp, top = 8.dp, bottom = 88.dp,
                ),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                items(state.locations, key = { it.locationId }) { location ->
                    LocationCard(
                        location = location,
                        onOpen = { onOpenLocation(location.locationId) },
                        onEdit = { editing = location },
                        onManageUsers = { onManageUsers(location.locationId) },
                        onDelete = { deleting = location },
                    )
                }
            }
        }

        FloatingActionButton(
            onClick = { creating = true },
            modifier = Modifier
                .align(Alignment.BottomEnd)
                .padding(16.dp),
        ) {
            Icon(Icons.Filled.Add, contentDescription = "Add location")
        }
    }
    }

    if (creating) {
        NameDialog(
            title = "Add location",
            initialName = "",
            confirmLabel = "Create",
            busy = state.saving,
            onConfirm = { name ->
                viewModel.create(name) { onMessage(it) }
                creating = false
            },
            onDismiss = { creating = false },
        )
    }

    editing?.let { location ->
        NameDialog(
            title = "Rename location",
            initialName = location.name,
            confirmLabel = "Save",
            busy = state.saving,
            onConfirm = { name ->
                viewModel.rename(location.locationId, name) { onMessage(it) }
                editing = null
            },
            onDismiss = { editing = null },
        )
    }

    deleting?.let { location ->
        ConfirmDeleteDialog(
            title = "Delete ${location.name}?",
            message = "Deleting a location removes it for everyone it's shared with.",
            forceLabel = "Also delete the boxes it still holds",
            onConfirm = { force ->
                viewModel.delete(location, force) { onMessage(it) }
                deleting = null
            },
            onDismiss = { deleting = null },
        )
    }
}

@Composable
private fun LocationCard(
    location: StorageLocation,
    onOpen: () -> Unit,
    onEdit: () -> Unit,
    onManageUsers: () -> Unit,
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
                Text(location.name, style = MaterialTheme.typography.titleMedium)
                AssistChip(
                    onClick = onOpen,
                    label = { Text(location.accessLevel.name) },
                )
            }

            // Sharing and deleting are the owner's to do; editing needs write access.
            OverflowMenu(
                contentDescription = "Actions for ${location.name}",
                actions = buildList {
                    if (location.accessLevel.canEdit) {
                        add(MenuAction("Rename", onEdit, Icons.Filled.Edit))
                    }
                    if (location.accessLevel.canManageUsers) {
                        add(MenuAction("Share", onManageUsers, Icons.Filled.People))
                        add(MenuAction("Delete", onDelete, Icons.Filled.Delete, destructive = true))
                    }
                },
            )
        }
    }
}

@Composable
private fun NameDialog(
    title: String,
    initialName: String,
    confirmLabel: String,
    busy: Boolean,
    onConfirm: (String) -> Unit,
    onDismiss: () -> Unit,
) {
    var name by remember { mutableStateOf(initialName) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            OutlinedTextField(
                value = name,
                onValueChange = { name = it },
                label = { Text("Name") },
                singleLine = true,
                enabled = !busy,
                modifier = Modifier.fillMaxWidth(),
            )
        },
        confirmButton = {
            TextButton(onClick = { onConfirm(name) }, enabled = name.isNotBlank() && !busy) {
                Text(confirmLabel)
            }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } },
    )
}
