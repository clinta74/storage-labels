package net.pollyspeople.storagelabels.core.network

import android.content.Context
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat

/** Declared in the manifest; named here because older platforms have no constant for it. */
const val ACCESS_LOCAL_NETWORK = "android.permission.ACCESS_LOCAL_NETWORK"

/**
 * Android 17 puts connections to private addresses — 192.168.x.x, 10.x.x.x, the rest of
 * RFC 1918 — behind a runtime permission. Without it the platform drops the outgoing SYN, so
 * the app sees a plain connect timeout with nothing to say a permission was the reason. A
 * self-hosted server is very often exactly there, and a browser on the same phone reaches it
 * fine, which makes the app look broken.
 *
 * [applies] is false on platforms that do not define the permission, and [granted] then reads
 * true, so callers need no version checks of their own.
 */
data class LocalNetworkPermissionState(
    val applies: Boolean,
    val granted: Boolean,
    /** Asks if needed, then runs [onReady] with whatever access ended up being available. */
    val ensure: (onReady: (Boolean) -> Unit) -> Unit,
)

@Composable
fun rememberLocalNetworkPermission(): LocalNetworkPermissionState {
    val context = LocalContext.current

    // Asking the package manager rather than checking a version number: the permission either
    // exists on this platform or it does not, and that is the thing that actually matters.
    val applies = remember(context) { context.knowsLocalNetworkPermission() }
    var granted by remember(context) {
        mutableStateOf(!applies || context.hasLocalNetworkPermission())
    }
    var pending by remember { mutableStateOf<((Boolean) -> Unit)?>(null) }

    val launcher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission(),
    ) { result ->
        granted = result
        pending?.invoke(result)
        pending = null
    }

    val ensure: (onReady: (Boolean) -> Unit) -> Unit = { onReady ->
        if (granted) {
            onReady(true)
        } else {
            pending = onReady
            launcher.launch(ACCESS_LOCAL_NETWORK)
        }
    }

    return LocalNetworkPermissionState(applies = applies, granted = granted, ensure = ensure)
}

private fun Context.knowsLocalNetworkPermission(): Boolean =
    runCatching { packageManager.getPermissionInfo(ACCESS_LOCAL_NETWORK, 0) }.isSuccess

private fun Context.hasLocalNetworkPermission(): Boolean =
    ContextCompat.checkSelfPermission(this, ACCESS_LOCAL_NETWORK) ==
        PackageManager.PERMISSION_GRANTED
