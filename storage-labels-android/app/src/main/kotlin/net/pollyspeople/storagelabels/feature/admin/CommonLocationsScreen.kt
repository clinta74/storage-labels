package net.pollyspeople.storagelabels.feature.admin

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
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.permissions.LocalPermissions
import net.pollyspeople.storagelabels.core.permissions.Permissions
import net.pollyspeople.storagelabels.core.permissions.hasPermission
import net.pollyspeople.storagelabels.core.ui.ConfirmDeleteDialog
import net.pollyspeople.storagelabels.core.ui.EmptyState
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.data.dto.CommonLocation

@Composable
fun CommonLocationsScreen(
    onMessage: (String) -> Unit,
    viewModel: CommonLocationsViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val canWrite = LocalPermissions.current.hasPermission(Permissions.WRITE_COMMON_LOCATIONS)

    var adding by remember { mutableStateOf(false) }
    var deleting by remember { mutableStateOf<CommonLocation?>(null) }

    Box(Modifier.fillMaxSize()) {
        when {
            state.loading -> LoadingBox()

            state.error != null && state.locations.isEmpty() ->
                ErrorBanner(state.error!!, onRetry = viewModel::refresh)

            state.locations.isEmpty() -> EmptyState(
                title = "No common locations",
                message = "These are the shared place names everyone can pick from.",
                actionLabel = if (canWrite) "Add one" else null,
                onAction = if (canWrite) ({ adding = true }) else null,
            )

            else -> LazyColumn(
                contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 8.dp, bottom = 88.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.fillMaxSize(),
            ) {
                items(state.locations, key = { it.commonLocationId }) { location ->
                    Card(Modifier.fillMaxWidth()) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(start = 16.dp, top = 12.dp, bottom = 12.dp, end = 4.dp),
                            verticalAlignment = Alignment.CenterVertically,
                        ) {
                            Text(
                                location.name,
                                style = MaterialTheme.typography.bodyLarge,
                                modifier = Modifier.weight(1f),
                            )
                            if (canWrite) {
                                IconButton(onClick = { deleting = location }) {
                                    Icon(
                                        Icons.Filled.Delete,
                                        contentDescription = "Delete ${location.name}",
                                    )
                                }
                            }
                        }
                    }
                }
            }
        }

        if (canWrite) {
            FloatingActionButton(
                onClick = { adding = true },
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(16.dp),
            ) {
                Icon(Icons.Filled.Add, contentDescription = "Add common location")
            }
        }
    }

    if (adding) {
        var name by remember { mutableStateOf("") }
        AlertDialog(
            onDismissRequest = { adding = false },
            title = { Text("Add common location") },
            text = {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text("Name") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        viewModel.create(name, onMessage)
                        adding = false
                    },
                    enabled = name.isNotBlank(),
                ) {
                    Text("Add")
                }
            },
            dismissButton = { TextButton(onClick = { adding = false }) { Text("Cancel") } },
        )
    }

    deleting?.let { location ->
        ConfirmDeleteDialog(
            title = "Delete ${location.name}?",
            message = "It disappears from the list everyone picks from.",
            onConfirm = {
                viewModel.delete(location, onMessage)
                deleting = null
            },
            onDismiss = { deleting = null },
        )
    }
}
