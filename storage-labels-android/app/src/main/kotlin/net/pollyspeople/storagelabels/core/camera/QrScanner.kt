package net.pollyspeople.storagelabels.core.camera

import android.annotation.SuppressLint
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.ImageProxy
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.google.mlkit.vision.barcode.BarcodeScanner
import com.google.mlkit.vision.barcode.BarcodeScannerOptions
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.barcode.common.Barcode
import com.google.mlkit.vision.common.InputImage
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicBoolean

/**
 * Live QR scanning, replacing the web app's browser scanner. Only QR codes are looked for —
 * that's what Storage Labels prints — and the first hit wins: [onCode] fires once, then the
 * analyzer stops, so a scan can't fire twice while the screen is closing.
 *
 * The camera is bound to the hosting lifecycle, which outlives this view — the scanner opens
 * in a dialog over a screen that stays put. So going away has to hand the camera back by
 * hand: nothing else will, and a camera left bound keeps the lens busy and the privacy
 * indicator lit for as long as the screen behind is open.
 */
@Composable
fun QrScannerView(
    onCode: (String) -> Unit,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val currentOnCode by rememberUpdatedState(onCode)
    val delivered = remember { AtomicBoolean(false) }
    val executor = remember { Executors.newSingleThreadExecutor() }
    val scanner: BarcodeScanner = remember {
        BarcodeScanning.getClient(
            BarcodeScannerOptions.Builder()
                .setBarcodeFormats(Barcode.FORMAT_QR_CODE)
                .build(),
        )
    }
    val previewView = remember {
        PreviewView(context).apply { scaleType = PreviewView.ScaleType.FILL_CENTER }
    }

    DisposableEffect(previewView, lifecycleOwner) {
        val preview = Preview.Builder().build().apply {
            surfaceProvider = previewView.surfaceProvider
        }
        val analysis = ImageAnalysis.Builder()
            .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
            .build()

        analysis.setAnalyzer(executor) { proxy ->
            processFrame(proxy, scanner, delivered) { code ->
                ContextCompat.getMainExecutor(context).execute { currentOnCode(code) }
            }
        }

        // The provider arrives a beat later, by which time a dialog dismissed immediately
        // may already be gone. Binding then would start a camera with no one left to unbind
        // it, so the callback checks before it acts and records the provider for [onDispose].
        var provider: ProcessCameraProvider? = null
        var disposed = false
        val providerFuture = ProcessCameraProvider.getInstance(context)
        providerFuture.addListener({
            if (disposed) return@addListener
            runCatching {
                val ready = providerFuture.get()
                ready.unbindAll()
                ready.bindToLifecycle(
                    lifecycleOwner,
                    CameraSelector.DEFAULT_BACK_CAMERA,
                    preview,
                    analysis,
                )
                provider = ready
            }
        }, ContextCompat.getMainExecutor(context))

        onDispose {
            disposed = true
            // Order matters. Stop frames at the source, and mark the scan spent so anything
            // already queued on the executor returns without touching the scanner, before
            // the two of them are closed underneath it.
            delivered.set(true)
            analysis.clearAnalyzer()
            provider?.unbind(preview, analysis)
            executor.shutdown()
            scanner.close()
        }
    }

    AndroidView(modifier = modifier, factory = { previewView })
}

@SuppressLint("UnsafeOptInUsageError")
private fun processFrame(
    proxy: ImageProxy,
    scanner: BarcodeScanner,
    delivered: AtomicBoolean,
    onCode: (String) -> Unit,
) {
    val mediaImage = proxy.image
    if (mediaImage == null || delivered.get()) {
        proxy.close()
        return
    }

    val image = InputImage.fromMediaImage(mediaImage, proxy.imageInfo.rotationDegrees)
    scanner.process(image)
        .addOnSuccessListener { barcodes ->
            val value = barcodes.firstNotNullOfOrNull { it.rawValue }
            if (!value.isNullOrBlank() && delivered.compareAndSet(false, true)) {
                onCode(value)
            }
        }
        .addOnCompleteListener { proxy.close() }
}
