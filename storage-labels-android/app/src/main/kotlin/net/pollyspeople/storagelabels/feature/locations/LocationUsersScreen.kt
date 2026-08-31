package net.pollyspeople.storagelabels.feature.locations

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
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
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.ui.ConfirmDeleteDialog
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.data.dto.AccessLevel
import net.pollyspeople.storagelabels.data.dto.LocationUser

private val ShareableLevels = listOf(AccessLevel.View, AccessLevel.Edit, AccessLevel.Owner)

@Composable
fun LocationUsersScreen(
    onMessage: (String) -> Unit,
    viewModel: LocationUsersViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var removing by remember { mutableStateOf<LocationUser?>(null) }

    if (state.loading) {
        LoadingBox()
        return
    }

    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .imePadding(),
        contentPadding = PaddingValues(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        item {
            Text("Share this location", style = MaterialTheme.typography.titleMedium)
            Text(
                "People you share with need an account on this server already.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }

        item {
            OutlinedTextField(
                value = state.email,
                onValueChange = viewModel::onEmailChange,
                label = { Text("Email address") },
                singleLine = true,
                enabled = !state.busy,
                isError = state.error != null,
                supportingText = state.error?.let { { Text(it) } },
                keyboardOptions = KeyboardOptions(
                    keyboardType = KeyboardType.Email,
                    imeAction = ImeAction.Done,
                ),
                modifier = Modifier.fillMaxWidth(),
            )
        }

        item {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                modifier = Modifier.fillMaxWidth(),
            ) {
                AccessLevelPicker(
                    current = state.accessLevel,
                    enabled = !state.busy,
                    onSelect = viewModel::onAccessLevelChange,
                )
                Button(
                    onClick = { viewModel.addUser(onMessage) },
                    enabled = state.email.isNotBlank() && !state.busy,
                ) {
                    Text("Share")
                }
            }
        }

        item { HorizontalDivider() }

        items(state.users, key = { it.userId }) { user ->
            Card(Modifier.fillMaxWidth()) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(start = 16.dp, top = 8.dp, bottom = 8.dp, end = 4.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Column(Modifier.weight(1f)) {
                        Text(user.displayName, style = MaterialTheme.typography.bodyLarge)
                        Text(
                            user.emailAddress,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                    AccessLevelPicker(
                        current = user.accessLevel,
                        enabled = true,
                        onSelect = { level -> viewModel.changeAccess(user, level, onMessage) },
                    )
                    IconButton(onClick = { removing = user }) {
                        Icon(Icons.Filled.Delete, contentDescription = "Remove ${user.displayName}")
                    }
                }
            }
        }
    }

    removing?.let { user ->
        ConfirmDeleteDialog(
            title = "Remove ${user.displayName}?",
            message = "They'll lose access to this location and everything in it.",
            confirmLabel = "Remove",
            onConfirm = {
                viewModel.removeUser(user, onMessage)
                removing = null
            },
            onDismiss = { removing = null },
        )
    }
}

@Composable
private fun AccessLevelPicker(
    current: AccessLevel,
    enabled: Boolean,
    onSelect: (AccessLevel) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }

    Column {
        TextButton(onClick = { expanded = true }, enabled = enabled) {
            Text(current.name)
        }
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            ShareableLevels.forEach { level ->
                DropdownMenuItem(
                    text = { Text(level.name) },
                    onClick = {
                        expanded = false
                        if (level != current) onSelect(level)
                    },
                )
            }
        }
    }
}
