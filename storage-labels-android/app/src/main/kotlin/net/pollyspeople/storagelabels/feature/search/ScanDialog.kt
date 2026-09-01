package net.pollyspeople.storagelabels.feature.search

import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import net.pollyspeople.storagelabels.core.camera.QrScannerView
import net.pollyspeople.storagelabels.core.camera.rememberCameraPermission

/**
 * Scanning always happens here, over whatever you were doing -- from the search bar or from
 * a box's code field. Camera permission is asked for when the dialog opens rather than up
 * front, so the app only asks at the moment it needs to.
 */
@Composable
internal fun ScanDialog(onDismiss: () -> Unit, onCode: (String) -> Unit) {
    val camera = rememberCameraPermission()

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Scan a label") },
        text = {
            if (camera.granted) {
                QrScannerView(
                    onCode = onCode,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(320.dp),
                )
            } else {
                Text(camera.message)
            }
        },
        confirmButton = {
            if (camera.granted) {
                TextButton(onClick = onDismiss) { Text("Cancel") }
            } else {
                TextButton(onClick = camera.request) { Text(camera.actionLabel) }
            }
        },
        dismissButton = if (camera.granted) {
            null
        } else {
            { TextButton(onClick = onDismiss) { Text("Cancel") } }
        },
    )
}
