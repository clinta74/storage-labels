package net.pollyspeople.storagelabels.feature.admin

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.LockReset
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Card
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
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
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.ui.ConfirmDeleteDialog
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.core.ui.MenuAction
import net.pollyspeople.storagelabels.core.ui.OverflowMenu
import net.pollyspeople.storagelabels.data.dto.UserWithRoles

private val Roles = listOf("Admin", "Auditor", "User")

@Composable
fun UserManagementScreen(
    currentUserEmail: String,
    onMessage: (String) -> Unit,
    viewModel: UserManagementViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    // Loads on first show and again whenever the screen comes back to the front, so a
    // box or item added on a pushed screen is there when you return.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }
    var resetting by remember { mutableStateOf<UserWithRoles?>(null) }
    var deleting by remember { mutableStateOf<UserWithRoles?>(null) }

    if (state.loading) {
        LoadingBox()
        return
    }

    Column(Modifier.fillMaxSize()) {
        state.error?.let { ErrorBanner(it, onRetry = viewModel::refresh) }

        LazyColumn(
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
            modifier = Modifier.fillMaxSize(),
        ) {
            items(state.users, key = { it.userId }) { user ->
                val isSelf = user.email.equals(currentUserEmail, ignoreCase = true)
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
                                user.email,
                                style = MaterialTheme.typography.bodySmall,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                            )
                            if (!user.isActive) {
                                Text(
                                    "Inactive",
                                    style = MaterialTheme.typography.labelSmall,
                                    color = MaterialTheme.colorScheme.error,
                                )
                            }
                        }

                        RolePicker(
                            current = user.role,
                            // Changing your own role could lock you out of this screen.
                            enabled = !isSelf && state.busyUserId != user.userId,
                            onSelect = { role -> viewModel.changeRole(user, role, onMessage) },
                        )

                        OverflowMenu(
                            contentDescription = "Actions for ${user.displayName}",
                            actions = buildList {
                                add(
                                    MenuAction(
                                        "Reset password",
                                        { resetting = user },
                                        Icons.Filled.LockReset,
                                    ),
                                )
                                // Deleting your own account from here would end the session
                                // you are using to manage everyone else's.
                                if (!isSelf) {
                                    add(
                                        MenuAction(
                                            "Delete account",
                                            { deleting = user },
                                            Icons.Filled.Delete,
                                            destructive = true,
                                        ),
                                    )
                                }
                            },
                        )
                    }
                }
            }
        }
    }

    resetting?.let { user ->
        var password by remember { mutableStateOf("") }
        AlertDialog(
            onDismissRequest = { resetting = null },
            title = { Text("Reset password for ${user.displayName}") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("They'll be signed out everywhere and will need this new password.")
                    OutlinedTextField(
                        value = password,
                        onValueChange = { password = it },
                        label = { Text("New password") },
                        singleLine = true,
                        visualTransformation = PasswordVisualTransformation(),
                        modifier = Modifier.fillMaxWidth(),
                    )
                }
            },
            confirmButton = {
                TextButton(
                    onClick = {
                        viewModel.resetPassword(user, password, onMessage)
                        resetting = null
                    },
                    enabled = password.isNotBlank(),
                ) {
                    Text("Reset password")
                }
            },
            dismissButton = { TextButton(onClick = { resetting = null }) { Text("Cancel") } },
        )
    }

    deleting?.let { user ->
        ConfirmDeleteDialog(
            title = "Delete ${user.displayName}?",
            message = "Their account is removed. Locations they own go with it.",
            forceLabel = "I understand this can't be undone",
            onConfirm = {
                viewModel.deleteUser(user, onMessage)
                deleting = null
            },
            onDismiss = { deleting = null },
        )
    }
}

@Composable
private fun RolePicker(current: String, enabled: Boolean, onSelect: (String) -> Unit) {
    var expanded by remember { mutableStateOf(false) }

    Column {
        AssistChip(
            onClick = { expanded = true },
            enabled = enabled,
            label = { Text(current) },
        )
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            Roles.forEach { role ->
                DropdownMenuItem(
                    text = { Text(role) },
                    onClick = {
                        expanded = false
                        if (role != current) onSelect(role)
                    },
                )
            }
        }
    }
}
