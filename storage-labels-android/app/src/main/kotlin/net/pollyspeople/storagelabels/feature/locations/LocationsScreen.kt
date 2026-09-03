package net.pollyspeople.storagelabels.feature.locations

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.People
import androidx.compose.material3.AlertDialog
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
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.ui.ConfirmDeleteDialog
import net.pollyspeople.storagelabels.core.ui.ActionErrorEffect
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

    ActionErrorEffect(
        error = state.error,
        bannerVisible = state.locations.isEmpty(),
        onMessage = onMessage,
        onClear = viewModel::clearError,
    )

    var editing by remember { mutableStateOf<StorageLocation?>(null) }
    var creating by remember { mutableStateOf(false) }
    var deleting by remember { mutableStateOf<StorageLocation?>(null) }

    Column(Modifier.fillMaxSize()) {
        InlineSearchBar(onOpenBox = onOpenBox)

    Box(Modifier.fillMaxSize()) {
        when {
            state.loading -> LoadingBox()

            state.error != null && state.locations.isEmpty() ->
                ErrorBanner(state.error.orEmpty(), onRetry = viewModel::refresh)

            state.locations.isEmpty() -> EmptyState(
                title = "No locations yet",
                message = "A location is a place you keep boxes — a garage, a storage unit, an attic.",
                actionLabel = "Add a location",
                onAction = { creating = true },
            )

            else -> LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(
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
            suggestions = state.commonLocationNames,
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
            suggestions = state.commonLocationNames,
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
            // The list doesn't know whether this location holds anything, so the tick is
            // offered rather than demanded; the server rejects a delete that needs it.
            forceLabel = "Also delete the boxes it still holds",
            forceRequired = false,
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
            Text(
                location.name,
                style = MaterialTheme.typography.titleMedium,
                modifier = Modifier.weight(1f),
            )

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
    suggestions: List<String>,
    onConfirm: (String) -> Unit,
    onDismiss: () -> Unit,
) {
    var name by remember { mutableStateOf(initialName) }

    // Common locations are offered, never imposed: the field stays free text, and the list
    // narrows as you type, matching the web app's autocomplete.
    val matches = remember(name, suggestions) {
        suggestions
            .filter { it.contains(name.trim(), ignoreCase = true) && !it.equals(name.trim(), true) }
            .take(6)
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text("Name") },
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(
                        capitalization = KeyboardCapitalization.Words,
                        imeAction = ImeAction.Done,
                    ),
                    enabled = !busy,
                    modifier = Modifier.fillMaxWidth(),
                )

                if (matches.isNotEmpty()) {
                    Text(
                        "Common locations",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(top = 8.dp),
                    )
                    Column(Modifier.heightIn(max = 180.dp).verticalScroll(rememberScrollState())) {
                        matches.forEach { suggestion ->
                            Text(
                                suggestion,
                                style = MaterialTheme.typography.bodyLarge,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .clickable(enabled = !busy) { name = suggestion }
                                    .padding(vertical = 10.dp),
                            )
                        }
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = { onConfirm(name) }, enabled = name.isNotBlank() && !busy) {
                Text(confirmLabel)
            }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Cancel") } },
    )
}
