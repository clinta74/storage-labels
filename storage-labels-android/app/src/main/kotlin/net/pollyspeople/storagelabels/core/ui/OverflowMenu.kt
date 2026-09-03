package net.pollyspeople.storagelabels.core.ui

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector

/**
 * One action per row is fine on a wide screen; on a phone a row of bare icons is a guessing
 * game and an easy mis-tap next to a destructive one. Edit / move / delete therefore live
 * behind a single overflow menu everywhere, the way the web app's settings menus do.
 */
data class MenuAction(
    val label: String,
    val onClick: () -> Unit,
    val icon: ImageVector? = null,
    /** Drawn in the error colour, and always placed last. */
    val destructive: Boolean = false,
    val enabled: Boolean = true,
)

@Composable
fun OverflowMenu(
    actions: List<MenuAction>,
    modifier: Modifier = Modifier,
    contentDescription: String = "More actions",
) {
    if (actions.isEmpty()) return

    var expanded by remember { mutableStateOf(false) }
    val ordered = remember(actions) { actions.sortedBy { it.destructive } }

    androidx.compose.foundation.layout.Box(modifier) {
        IconButton(onClick = { expanded = true }) {
            Icon(Icons.Filled.MoreVert, contentDescription = contentDescription)
        }

        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            ordered.forEach { action ->
                DropdownMenuItem(
                    enabled = action.enabled,
                    text = {
                        Text(
                            action.label,
                            color = if (action.destructive) {
                                MaterialTheme.colorScheme.error
                            } else {
                                MaterialTheme.colorScheme.onSurface
                            },
                        )
                    },
                    leadingIcon = action.icon?.let {
                        {
                            Icon(
                                it,
                                contentDescription = null,
                                tint = if (action.destructive) {
                                    MaterialTheme.colorScheme.error
                                } else {
                                    MaterialTheme.colorScheme.onSurfaceVariant
                                },
                            )
                        }
                    },
                    onClick = {
                        expanded = false
                        action.onClick()
                    },
                )
            }
        }
    }
}
