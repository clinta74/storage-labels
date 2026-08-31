package net.pollyspeople.storagelabels.feature.search

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import net.pollyspeople.storagelabels.core.camera.QrScannerView
import net.pollyspeople.storagelabels.core.ui.EmptyState

/**
 * Full-screen scanner used when filling in a box's code: point at the printed label instead
 * of typing the characters off it.
 */
@Composable
fun CodeScannerScreen(
    onCode: (String) -> Unit,
    onCancel: () -> Unit,
) {
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

    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        if (hasPermission) {
            QrScannerView(
                onCode = onCode,
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f),
            )
            Text(
                "Point the camera at the QR code on the label.",
                style = MaterialTheme.typography.bodyMedium,
                modifier = Modifier.padding(horizontal = 24.dp),
            )
        } else {
            EmptyState(
                title = "Camera access needed",
                message = "Allow the camera to read the code from a label.",
                actionLabel = "Allow camera",
                onAction = { permissionLauncher.launch(Manifest.permission.CAMERA) },
                modifier = Modifier.weight(1f),
            )
        }

        TextButton(onClick = onCancel, modifier = Modifier.padding(bottom = 16.dp)) {
            Text("Cancel")
        }
    }
}
