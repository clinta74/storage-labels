package net.pollyspeople.storagelabels.feature.search

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
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
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
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
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import kotlinx.coroutines.flow.distinctUntilChanged
import net.pollyspeople.storagelabels.core.camera.QrScannerView
import net.pollyspeople.storagelabels.core.ui.EmptyState
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.data.dto.SearchResult

/**
 * Global search across boxes and items, plus the label scanner. This is the screen the app
 * exists for: point the camera at a box and find out what's in it.
 */
@Composable
fun SearchScreen(
    onOpenBox: (locationId: Long, boxId: String) -> Unit,
    viewModel: SearchViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val listState = rememberLazyListState()
    var scanning by remember { mutableStateOf(false) }

    // Infinite scroll: ask for the next page as the end of the list comes into view.
    LaunchedEffect(listState) {
        snapshotFlow {
            val layout = listState.layoutInfo
            val lastVisible = layout.visibleItemsInfo.lastOrNull()?.index ?: 0
            lastVisible >= layout.totalItemsCount - 3
        }
            .distinctUntilChanged()
            .collect { nearEnd -> if (nearEnd) viewModel.loadMore() }
    }

    Column(Modifier.fillMaxSize()) {
        OutlinedTextField(
            value = state.query,
            onValueChange = viewModel::onQueryChange,
            label = { Text("Search boxes and items") },
            singleLine = true,
            leadingIcon = { Icon(Icons.Filled.Search, contentDescription = null) },
            trailingIcon = {
                Row {
                    if (state.query.isNotEmpty()) {
                        IconButton(onClick = viewModel::clear) {
                            Icon(Icons.Filled.Close, contentDescription = "Clear search")
                        }
                    }
                    IconButton(onClick = { scanning = true }) {
                        Icon(Icons.Filled.QrCodeScanner, contentDescription = "Scan a label")
                    }
                }
            },
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
        )

        when {
            state.searching -> Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }

            state.error != null -> ErrorBanner(state.error!!)

            state.query.isBlank() -> EmptyState(
                title = "Find anything you've packed",
                message = "Search by name or description, or scan the label on a box.",
                actionLabel = "Scan a label",
                onAction = { scanning = true },
            )

            state.results.isEmpty() -> EmptyState(
                title = "Nothing matched",
                message = "Try fewer words, or a different spelling.",
            )

            else -> LazyColumn(
                state = listState,
                contentPadding = PaddingValues(start = 16.dp, end = 16.dp, bottom = 24.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.fillMaxSize(),
            ) {
                item {
                    Text(
                        "${state.totalCount} result${if (state.totalCount == 1) "" else "s"}",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }

                items(state.results, key = { "${it.type}-${it.itemId ?: it.boxId}" }) { result ->
                    ResultCard(result) {
                        val locationId = result.locationIdOrNull
                        val boxId = result.boxId
                        if (locationId != null && boxId != null) onOpenBox(locationId, boxId)
                    }
                }

                if (state.loadingMore) {
                    item {
                        Box(
                            Modifier
                                .fillMaxWidth()
                                .height(56.dp),
                            contentAlignment = Alignment.Center,
                        ) {
                            CircularProgressIndicator()
                        }
                    }
                }
            }
        }
    }

    if (scanning) {
        ScanDialog(
            onDismiss = { scanning = false },
            onCode = { code ->
                scanning = false
                viewModel.onCodeScanned(code, onOpenBox)
            },
        )
    }

    state.scanMiss?.let { message ->
        AlertDialog(
            onDismissRequest = viewModel::clearScanMiss,
            title = { Text("Label not recognised") },
            text = { Text(message) },
            confirmButton = {
                TextButton(onClick = viewModel::clearScanMiss) { Text("OK") }
            },
        )
    }
}

@Composable
private fun ResultCard(result: SearchResult, onClick: () -> Unit) {
    Card(modifier = Modifier.fillMaxWidth().clickable(onClick = onClick)) {
        Column(Modifier.padding(16.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    result.title.ifBlank { "Untitled" },
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.weight(1f),
                )
                AssistChip(onClick = onClick, label = { Text(if (result.isItem) "Item" else "Box") })
            }
            if (result.isItem && !result.boxName.isNullOrBlank()) {
                Text(
                    "in ${result.boxName}",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Text(
                result.locationName,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@Composable
internal fun ScanDialog(onDismiss: () -> Unit, onCode: (String) -> Unit) {
    val context = LocalContext.current
    var hasPermission by remember {
        mutableStateOf(
            ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) ==
                PackageManager.PERMISSION_GRANTED,
        )
    }
    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission(),
    ) { granted -> hasPermission = granted }

    LaunchedEffect(Unit) {
        if (!hasPermission) permissionLauncher.launch(Manifest.permission.CAMERA)
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Scan a label") },
        text = {
            if (hasPermission) {
                QrScannerView(
                    onCode = onCode,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(320.dp),
                )
            } else {
                Text("Allow camera access to scan the QR code on a label.")
            }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text("Cancel") } },
    )
}
