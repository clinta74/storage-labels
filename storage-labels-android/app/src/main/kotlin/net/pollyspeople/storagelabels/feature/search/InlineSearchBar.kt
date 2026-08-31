package net.pollyspeople.storagelabels.feature.search

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
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
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.data.dto.SearchResult

/**
 * Search where you already are, the way the web app does it: a field at the top of Locations
 * and Box, with results floating over the content rather than replacing it. Clearing the
 * field puts the page back exactly as it was.
 */
@Composable
fun InlineSearchBar(
    onOpenBox: (locationId: Long, boxId: String) -> Unit,
    modifier: Modifier = Modifier,
    placeholder: String = "Search boxes and items",
    viewModel: SearchViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var scanning by remember { mutableStateOf(false) }

    val openResult: (SearchResult) -> Unit = { result ->
        val locationId = result.locationIdOrNull
        val boxId = result.boxId
        if (locationId != null && boxId != null) {
            viewModel.clear()
            onOpenBox(locationId, boxId)
        }
    }

    Column(modifier.fillMaxWidth()) {
        OutlinedTextField(
            value = state.query,
            onValueChange = viewModel::onQueryChange,
            placeholder = { Text(placeholder) },
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
                .padding(horizontal = 16.dp, vertical = 8.dp),
        )

        if (state.query.isNotBlank()) {
            Card(
                elevation = CardDefaults.elevatedCardElevation(defaultElevation = 6.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp),
            ) {
                when {
                    state.searching -> Box(
                        Modifier
                            .fillMaxWidth()
                            .padding(24.dp),
                        contentAlignment = Alignment.Center,
                    ) {
                        CircularProgressIndicator()
                    }

                    state.error != null -> Text(
                        state.error!!,
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.error,
                        modifier = Modifier.padding(16.dp),
                    )

                    state.results.isEmpty() -> Text(
                        "Nothing matched \"${state.query}\".",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.padding(16.dp),
                    )

                    else -> LazyColumn(modifier = Modifier.heightIn(max = 320.dp)) {
                        item {
                            Text(
                                "${state.totalCount} result${if (state.totalCount == 1) "" else "s"}",
                                style = MaterialTheme.typography.labelMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(start = 16.dp, top = 12.dp),
                            )
                        }
                        items(state.results, key = { "${it.type}-${it.itemId ?: it.boxId}" }) { result ->
                            ResultRow(result) { openResult(result) }
                            HorizontalDivider()
                        }
                        if (state.hasMore) {
                            item {
                                Text(
                                    "Showing ${state.results.size} of ${state.totalCount} — keep typing to narrow it down.",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    modifier = Modifier.padding(16.dp),
                                )
                            }
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
                viewModel.onCodeScanned(code) { locationId, boxId ->
                    viewModel.clear()
                    onOpenBox(locationId, boxId)
                }
            },
        )
    }

    state.scanMiss?.let { message ->
        androidx.compose.material3.AlertDialog(
            onDismissRequest = viewModel::clearScanMiss,
            title = { Text("Label not recognised") },
            text = { Text(message) },
            confirmButton = {
                androidx.compose.material3.TextButton(onClick = viewModel::clearScanMiss) {
                    Text("OK")
                }
            },
        )
    }
}

@Composable
private fun ResultRow(result: SearchResult, onClick: () -> Unit) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .padding(horizontal = 16.dp, vertical = 12.dp),
        verticalArrangement = Arrangement.spacedBy(2.dp),
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(
                result.title.ifBlank { "Untitled" },
                style = MaterialTheme.typography.bodyLarge,
                modifier = Modifier.weight(1f),
            )
            Text(
                if (result.isItem) "Item" else "Box",
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.primary,
            )
        }
        val subtitle = buildString {
            if (result.isItem && !result.boxName.isNullOrBlank()) append("in ${result.boxName} · ")
            append(result.locationName)
        }
        Text(
            subtitle,
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}
