package net.pollyspeople.storagelabels.feature.labels

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Print
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
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
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.labels.LabelPrinter
import net.pollyspeople.storagelabels.core.ui.ConfirmDeleteDialog
import net.pollyspeople.storagelabels.core.ui.ActionErrorEffect
import net.pollyspeople.storagelabels.core.ui.EmptyState
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.core.ui.FormattedCode
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.core.ui.MenuAction
import net.pollyspeople.storagelabels.core.ui.OverflowMenu
import net.pollyspeople.storagelabels.data.dto.LabelIncrementAlgorithm
import net.pollyspeople.storagelabels.data.dto.LabelPrintJob

@Composable
fun LabelJobsScreen(
    onOpenJob: (String) -> Unit,
    onCreateJob: () -> Unit,
    onMessage: (String) -> Unit,
    viewModel: LabelsViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var deleting by remember { mutableStateOf<LabelPrintJob?>(null) }

    // Loads on first show and again whenever the screen comes back to the front, so a
    // box or item added on a pushed screen is there when you return.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }

    ActionErrorEffect(
        error = state.error,
        bannerVisible = state.jobs.isEmpty(),
        onMessage = onMessage,
        onClear = viewModel::clearError,
    )

    Box(Modifier.fillMaxSize()) {
        when {
            state.loading -> LoadingBox()

            state.error != null && state.jobs.isEmpty() ->
                ErrorBanner(state.error.orEmpty(), onRetry = viewModel::refresh)

            state.jobs.isEmpty() -> EmptyState(
                title = "No label runs yet",
                message = "A label run generates sequential codes you print onto Avery 94107 sheets.",
                actionLabel = "Create a label run",
                onAction = onCreateJob,
            )

            else -> LazyColumn(
                contentPadding = PaddingValues(start = 16.dp, end = 16.dp, top = 8.dp, bottom = 88.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.fillMaxSize(),
            ) {
                items(state.jobs, key = { it.id }) { job ->
                    Card(modifier = Modifier.fillMaxWidth().clickable { onOpenJob(job.id) }) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(start = 16.dp, top = 12.dp, bottom = 12.dp, end = 4.dp),
                            verticalAlignment = Alignment.CenterVertically,
                        ) {
                            Column(Modifier.weight(1f)) {
                                Text(job.name, style = MaterialTheme.typography.titleMedium)
                                Text(
                                    "${job.totalLabelsGenerated} printed so far",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                )
                            }
                            OverflowMenu(
                                contentDescription = "Actions for ${job.name}",
                                actions = listOf(
                                    MenuAction("Open", { onOpenJob(job.id) }),
                                    MenuAction(
                                        "Delete",
                                        { deleting = job },
                                        Icons.Filled.Delete,
                                        destructive = true,
                                    ),
                                ),
                            )
                        }
                    }
                }
            }
        }

        FloatingActionButton(
            onClick = onCreateJob,
            modifier = Modifier
                .align(Alignment.BottomEnd)
                .padding(16.dp),
        ) {
            Icon(Icons.Filled.Add, contentDescription = "Create label run")
        }
    }

    deleting?.let { job ->
        ConfirmDeleteDialog(
            title = "Delete ${job.name}?",
            message = "The codes already printed stay on your boxes; only the run is removed.",
            onConfirm = {
                viewModel.delete(job, onMessage)
                deleting = null
            },
            onDismiss = { deleting = null },
        )
    }
}

@Composable
fun LabelJobDetailScreen(
    onEdit: () -> Unit,
    onMessage: (String) -> Unit,
    viewModel: LabelJobDetailViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val context = LocalContext.current

    // Loads on first show and again whenever the screen comes back to the front, so a
    // box or item added on a pushed screen is there when you return.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }
    val job = state.job

    if (state.loading && job == null) {
        LoadingBox()
        return
    }
    if (job == null) {
        ErrorBanner(state.error ?: "This label run couldn't be loaded.", onRetry = viewModel::refresh)
        return
    }

    LazyColumn(
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
        modifier = Modifier.fillMaxSize(),
    ) {
        item {
            Column {
                Text(job.name, style = MaterialTheme.typography.headlineSmall)
                Text(
                    "${job.totalLabelsGenerated} labels generated",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }

        item {
            Card(Modifier.fillMaxWidth()) {
                Column(
                    Modifier.padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    DetailRow("Format", "Avery 94107 — 2in, 12 per sheet")
                    DetailRow("Numbering", job.incrementAlgorithm.name)
                    if (!job.algorithmPrefix.isNullOrBlank()) {
                        DetailRow("Prefix", job.algorithmPrefix)
                    }
                    DetailRow("Suffix length", job.algorithmSuffixLength.toString())
                    DetailRow("Next index", job.lastGeneratedIndex.toString())
                }
            }
        }

        item {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(
                    onClick = { viewModel.allocateNextPage { } },
                    enabled = !state.allocating,
                ) {
                    if (state.allocating) {
                        CircularProgressIndicator(
                            modifier = Modifier.padding(end = 8.dp).size(18.dp),
                            strokeWidth = 2.dp,
                        )
                    }
                    Text("Generate next sheet")
                }
                OverflowMenu(
                    contentDescription = "Actions for this label run",
                    actions = listOf(MenuAction("Edit", onEdit, Icons.Filled.Edit)),
                )
            }
        }

        state.error?.let { message ->
            item { ErrorBanner(message) }
        }

        val page = state.page
        if (page != null) {
            item { HorizontalDivider() }
            item {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(
                        "Sheet ready — ${page.labels.size} labels",
                        style = MaterialTheme.typography.titleMedium,
                        modifier = Modifier.weight(1f),
                    )
                    Button(
                        onClick = {
                            LabelPrinter.print(
                                context = context,
                                jobName = job.name,
                                labels = page.labels,
                                codeColorPattern = page.codeColorPattern.ifBlank { job.codeColorPattern },
                            )
                        },
                    ) {
                        Icon(Icons.Filled.Print, contentDescription = null)
                        Text(" Print")
                    }
                }
            }
            item {
                Text(
                    "Load Avery 94107 stock and print at 100% scale — any \"fit to page\" " +
                        "setting will shift the labels off the die cuts.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            item {
                LazyVerticalGrid(
                    columns = GridCells.Fixed(3),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                    modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp),
                    userScrollEnabled = false,
                    // 4 rows of ~72dp previews
                    contentPadding = PaddingValues(0.dp),
                ) {
                    items(page.labels, key = { it.labelNumber }) { label ->
                        Card {
                            Column(
                                Modifier
                                    .fillMaxWidth()
                                    .padding(8.dp),
                                horizontalAlignment = Alignment.CenterHorizontally,
                            ) {
                                FormattedCode(
                                    code = label.code,
                                    pattern = page.codeColorPattern.ifBlank { job.codeColorPattern },
                                    style = MaterialTheme.typography.labelMedium,
                                )
                                Text(
                                    "#${label.labelNumber}",
                                    style = MaterialTheme.typography.labelSmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun DetailRow(label: String, value: String) {
    Row(Modifier.fillMaxWidth()) {
        Text(
            label,
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.weight(1f),
        )
        Text(value, style = MaterialTheme.typography.bodyMedium)
    }
}

@Composable
fun LabelJobEditScreen(
    onSaved: (String) -> Unit,
    onCancel: () -> Unit,
    viewModel: LabelJobEditViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    if (state.loading) {
        LoadingBox()
        return
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .imePadding()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Text(
            if (state.isNew) "New label run" else "Edit label run",
            style = MaterialTheme.typography.headlineSmall,
        )

        state.error?.let {
            ErrorBanner(it, contentPadding = PaddingValues(0.dp))
        }

        OutlinedTextField(
            value = state.name,
            onValueChange = viewModel::onNameChange,
            label = { Text("Name") },
            singleLine = true,
            enabled = !state.saving,
            isError = state.fieldErrors.containsKey(LabelJobEditViewModel.FIELD_NAME),
            supportingText = state.fieldErrors[LabelJobEditViewModel.FIELD_NAME]?.let { { Text(it) } },
            modifier = Modifier.fillMaxWidth(),
        )

        AlgorithmPicker(
            current = state.algorithm,
            enabled = !state.saving,
            onSelect = viewModel::onAlgorithmChange,
        )

        OutlinedTextField(
            value = state.prefix,
            onValueChange = viewModel::onPrefixChange,
            label = { Text("Prefix") },
            singleLine = true,
            enabled = !state.saving,
            isError = state.fieldErrors.containsKey(LabelJobEditViewModel.FIELD_PREFIX),
            supportingText = {
                Text(
                    state.fieldErrors[LabelJobEditViewModel.FIELD_PREFIX]
                        ?: "Optional. Goes in front of every code, e.g. GAR-",
                )
            },
            modifier = Modifier.fillMaxWidth(),
        )

        OutlinedTextField(
            value = state.suffixLength.toString(),
            onValueChange = { viewModel.onSuffixLengthChange(it.toIntOrNull() ?: 0) },
            label = { Text("Suffix length") },
            singleLine = true,
            enabled = !state.saving,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
            isError = state.fieldErrors.containsKey(LabelJobEditViewModel.FIELD_SUFFIX),
            supportingText = state.fieldErrors[LabelJobEditViewModel.FIELD_SUFFIX]?.let { { Text(it) } },
            modifier = Modifier.fillMaxWidth(),
        )

        if (state.isNew) {
            OutlinedTextField(
                value = state.startIndex.toString(),
                onValueChange = { viewModel.onStartIndexChange(it.toLongOrNull() ?: 0) },
                label = { Text("Start index") },
                singleLine = true,
                enabled = !state.saving,
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                isError = state.fieldErrors.containsKey(LabelJobEditViewModel.FIELD_START),
                supportingText = {
                    Text(
                        state.fieldErrors[LabelJobEditViewModel.FIELD_START]
                            ?: "Where the numbering begins. Can't be changed later.",
                    )
                },
                modifier = Modifier.fillMaxWidth(),
            )
        }

        OutlinedTextField(
            value = state.codeColorPattern,
            onValueChange = viewModel::onPatternChange,
            label = { Text("Colour pattern") },
            singleLine = true,
            enabled = !state.saving,
            supportingText = { Text("Applied to the printed code, e.g. 3:primary,*,2:error") },
            modifier = Modifier.fillMaxWidth(),
        )

        Row(
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier.fillMaxWidth(),
        ) {
            OutlinedButton(
                onClick = onCancel,
                enabled = !state.saving,
                modifier = Modifier.weight(1f),
            ) {
                Text("Cancel")
            }
            Button(
                onClick = { viewModel.save(onSaved) },
                enabled = !state.saving,
                modifier = Modifier.weight(1f),
            ) {
                Text(if (state.isNew) "Create" else "Save")
            }
        }
    }
}

@Composable
private fun AlgorithmPicker(
    current: LabelIncrementAlgorithm,
    enabled: Boolean,
    onSelect: (LabelIncrementAlgorithm) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }

    Column {
        Text("Numbering", style = MaterialTheme.typography.bodySmall)
        AssistChip(
            onClick = { expanded = true },
            enabled = enabled,
            label = {
                Text(
                    when (current) {
                        LabelIncrementAlgorithm.NumericOnly -> "Numbers only (0001)"
                        LabelIncrementAlgorithm.Base36Suffix -> "Letters and numbers (000A)"
                    },
                )
            },
        )
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            DropdownMenuItem(
                text = { Text("Numbers only (0001)") },
                onClick = {
                    expanded = false
                    onSelect(LabelIncrementAlgorithm.NumericOnly)
                },
            )
            DropdownMenuItem(
                text = { Text("Letters and numbers (000A)") },
                onClick = {
                    expanded = false
                    onSelect(LabelIncrementAlgorithm.Base36Suffix)
                },
            )
        }
    }
}
