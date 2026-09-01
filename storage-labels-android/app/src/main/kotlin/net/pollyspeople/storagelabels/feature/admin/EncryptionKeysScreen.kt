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
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Card
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
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
import net.pollyspeople.storagelabels.core.permissions.LocalPermissions
import net.pollyspeople.storagelabels.core.permissions.Permissions
import net.pollyspeople.storagelabels.core.permissions.hasPermission
import net.pollyspeople.storagelabels.core.ui.EmptyState
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.core.ui.MenuAction
import net.pollyspeople.storagelabels.core.ui.OverflowMenu
import net.pollyspeople.storagelabels.data.dto.EncryptionKey
import net.pollyspeople.storagelabels.data.dto.EncryptionKeyStats
import net.pollyspeople.storagelabels.data.dto.EncryptionKeyStatus

@Composable
fun EncryptionKeysScreen(
    onMessage: (String) -> Unit,
    viewModel: EncryptionKeysViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    // Loads on first show and again whenever the screen comes back to the front, so a
    // box or item added on a pushed screen is there when you return.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }
    val canWrite = LocalPermissions.current.hasPermission(Permissions.WRITE_ENCRYPTION_KEYS)

    var creating by remember { mutableStateOf(false) }
    var activating by remember { mutableStateOf<EncryptionKey?>(null) }

    // Stats are per-key and only worth fetching for keys actually on screen.
    LaunchedEffect(state.keys) {
        state.keys.forEach { viewModel.loadStats(it.kid) }
    }

    if (state.loading) {
        LoadingBox()
        return
    }

    Box(Modifier.fillMaxSize()) {
        LazyColumn(
            contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 8.dp, bottom = 88.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
            modifier = Modifier.fillMaxSize(),
        ) {
            state.error?.let { message ->
                item { ErrorBanner(message, onRetry = viewModel::refresh) }
            }

            state.liveProgress?.let { progress ->
                item {
                    Card(Modifier.fillMaxWidth()) {
                        Column(
                            Modifier.padding(16.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp),
                        ) {
                            Text("Re-encrypting images", style = MaterialTheme.typography.titleMedium)
                            LinearProgressIndicator(
                                progress = { (progress.percentComplete / 100.0).toFloat() },
                                modifier = Modifier.fillMaxWidth(),
                            )
                            Text(
                                "${progress.processedImages} of ${progress.totalImages}" +
                                    if (progress.failedImages > 0) " — ${progress.failedImages} failed" else "",
                                style = MaterialTheme.typography.bodySmall,
                            )
                            progress.errorMessage?.let {
                                Text(it, color = MaterialTheme.colorScheme.error)
                            }
                            if (!progress.status.isFinished && canWrite) {
                                val rotation = state.rotations.firstOrNull { it.id == progress.rotationId }
                                if (rotation != null) {
                                    TextButton(
                                        onClick = { viewModel.cancelRotation(rotation, onMessage) },
                                    ) {
                                        Text("Stop")
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (state.keys.isEmpty()) {
                item {
                    EmptyState(
                        title = "No encryption keys",
                        message = "Images are encrypted at rest with a versioned key.",
                        actionLabel = if (canWrite) "Create the first key" else null,
                        onAction = if (canWrite) ({ creating = true }) else null,
                    )
                }
            } else {
                items(state.keys, key = { it.kid }) { key ->
                    KeyCard(
                        key = key,
                        stats = state.stats[key.kid],
                        canWrite = canWrite && !state.busy,
                        onActivate = { activating = key },
                        onRetire = { viewModel.retire(key, onMessage) },
                    )
                }
            }

            if (state.rotations.isNotEmpty()) {
                item { HorizontalDivider(Modifier.padding(vertical = 8.dp)) }
                item { Text("Rotation history", style = MaterialTheme.typography.titleMedium) }
                items(state.rotations, key = { it.id }) { rotation ->
                    Card(Modifier.fillMaxWidth()) {
                        Column(Modifier.padding(16.dp)) {
                            Text(
                                (rotation.fromKeyId?.let { "Key $it" } ?: "Unencrypted") +
                                    " to key ${rotation.toKeyId}",
                                style = MaterialTheme.typography.bodyLarge,
                            )
                            Text(
                                "${rotation.status} — ${rotation.processedImages}/${rotation.totalImages}",
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                            )
                            if (!rotation.status.isFinished) {
                                TextButton(onClick = { viewModel.watchRotation(rotation.id) }) {
                                    Text("Watch progress")
                                }
                            }
                        }
                    }
                }
            }
        }

        if (canWrite) {
            FloatingActionButton(
                onClick = { creating = true },
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(16.dp),
            ) {
                Icon(Icons.Filled.Add, contentDescription = "Create key")
            }
        }
    }

    if (creating) {
        var description by remember { mutableStateOf("") }
        AlertDialog(
            onDismissRequest = { creating = false },
            title = { Text("Create encryption key") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("A new key starts inactive. Activating it re-encrypts existing images.")
                    OutlinedTextField(
                        value = description,
                        onValueChange = { description = it },
                        label = { Text("Description") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth(),
                    )
                }
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        viewModel.createKey(description, onMessage)
                        creating = false
                    },
                ) {
                    Text("Create")
                }
            },
            dismissButton = { TextButton(onClick = { creating = false }) { Text("Cancel") } },
        )
    }

    activating?.let { key ->
        AlertDialog(
            onDismissRequest = { activating = null },
            title = { Text("Activate key ${key.version}?") },
            text = {
                Text(
                    "New images will use this key. Re-encrypting the existing ones can take a " +
                        "while and runs in the background on the server.",
                )
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        viewModel.activate(key, autoRotate = true, onDone = onMessage)
                        activating = null
                    },
                ) {
                    Text("Activate and re-encrypt")
                }
            },
            dismissButton = {
                Row {
                    TextButton(
                        onClick = {
                            viewModel.activate(key, autoRotate = false, onDone = onMessage)
                            activating = null
                        },
                    ) {
                        Text("Activate only")
                    }
                    TextButton(onClick = { activating = null }) { Text("Cancel") }
                }
            },
        )
    }
}

@Composable
private fun KeyCard(
    key: EncryptionKey,
    stats: EncryptionKeyStats?,
    canWrite: Boolean,
    onActivate: () -> Unit,
    onRetire: () -> Unit,
) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    "Version ${key.version}",
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.weight(1f),
                )
                AssistChip(onClick = {}, label = { Text(key.status.name) })
            }
            key.description?.takeIf(String::isNotBlank)?.let {
                Text(it, style = MaterialTheme.typography.bodySmall)
            }
            Text(
                key.algorithm,
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            stats?.let {
                Text(
                    "${it.imageCount} images — ${it.totalSizeBytes / 1_000_000} MB",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }

            if (canWrite) {
                OverflowMenu(
                    contentDescription = "Actions for key version ${key.version}",
                    actions = buildList {
                        if (key.status == EncryptionKeyStatus.Created) {
                            add(MenuAction("Activate", onActivate))
                        }
                        // Retiring is only safe once nothing is encrypted under the key.
                        if (key.status == EncryptionKeyStatus.Deprecated && (stats?.imageCount ?: 0) == 0) {
                            add(MenuAction("Retire", onRetire, destructive = true))
                        }
                    },
                )
            }
        }
    }
}
