package net.pollyspeople.storagelabels.core.camera

import android.Manifest
import android.app.Activity
import android.content.Context
import android.content.ContextWrapper
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.provider.Settings
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect

/**
 * Whether the camera is available, and the one call that moves things forward.
 *
 * [blocked] is the case worth handling: once Android decides it has asked enough, launching
 * the permission dialog does nothing at all, so a button wired straight to it looks broken
 * forever. When that happens [request] opens the app's settings page instead, which is the
 * only route left, and [actionLabel] says so.
 */
data class CameraPermissionState(
    val granted: Boolean,
    val blocked: Boolean,
    val request: () -> Unit,
) {
    val actionLabel: String get() = if (blocked) "Open settings" else "Allow camera"

    val message: String
        get() = if (blocked) {
            "Camera access is turned off for Storage Labels. Turn it on in Settings to scan " +
                "labels and take photos."
        } else {
            "Allow the camera to read the code on a label and photograph what's in a box."
        }
}

/**
 * Asks for the camera once when the surface appears, and re-checks on resume so returning
 * from Settings takes effect without a restart.
 */
@Composable
fun rememberCameraPermission(): CameraPermissionState {
    val context = LocalContext.current
    var granted by remember { mutableStateOf(context.hasCameraPermission()) }
    var blocked by remember { mutableStateOf(false) }
    var asked by remember { mutableStateOf(false) }

    val launcher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission(),
    ) { result ->
        granted = result
        // No rationale offered after a denial means the system will stop showing the dialog.
        blocked = !result && context.findActivity()
            ?.shouldShowRequestPermissionRationale(Manifest.permission.CAMERA) == false
    }

    val request: () -> Unit = {
        if (blocked) context.openAppSettings() else launcher.launch(Manifest.permission.CAMERA)
    }

    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) {
        granted = context.hasCameraPermission()
        if (granted) blocked = false
        if (!granted && !asked) {
            asked = true
            launcher.launch(Manifest.permission.CAMERA)
        }
    }

    return CameraPermissionState(granted = granted, blocked = blocked, request = request)
}

private fun Context.hasCameraPermission(): Boolean =
    ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) ==
        PackageManager.PERMISSION_GRANTED

private fun Context.openAppSettings() {
    startActivity(
        Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS).apply {
            data = Uri.fromParts("package", packageName, null)
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        },
    )
}

private tailrec fun Context.findActivity(): Activity? = when (this) {
    is Activity -> this
    is ContextWrapper -> baseContext.findActivity()
    else -> null
}
