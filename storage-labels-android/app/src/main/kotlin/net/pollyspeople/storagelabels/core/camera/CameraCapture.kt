package net.pollyspeople.storagelabels.core.camera

import android.content.Context
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageCapture
import androidx.camera.core.ImageCaptureException
import androidx.camera.core.Preview
import androidx.camera.core.UseCase
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.LifecycleOwner
import java.io.File
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException
import kotlin.coroutines.suspendCoroutine

/**
 * A camera preview with the controls the web app's capture dialog offers: front/back switch
 * and a torch. Photos are written as JPEG because the API accepts nothing else.
 */
class CameraController(private val context: Context) {

    private var imageCapture: ImageCapture? = null
    private var camera: androidx.camera.core.Camera? = null
    private var provider: ProcessCameraProvider? = null
    private var bound: List<UseCase> = emptyList()

    var lensFacing: Int = CameraSelector.LENS_FACING_BACK
        private set

    val hasFlashUnit: Boolean get() = camera?.cameraInfo?.hasFlashUnit() == true

    suspend fun bind(lifecycleOwner: LifecycleOwner, previewView: PreviewView, lensFacing: Int) {
        this.lensFacing = lensFacing
        val provider = awaitCameraProvider()

        val preview = Preview.Builder().build().apply {
            surfaceProvider = previewView.surfaceProvider
        }
        val capture = ImageCapture.Builder()
            .setCaptureMode(ImageCapture.CAPTURE_MODE_MINIMIZE_LATENCY)
            .build()

        provider.unbindAll()
        camera = provider.bindToLifecycle(
            lifecycleOwner,
            CameraSelector.Builder().requireLensFacing(lensFacing).build(),
            preview,
            capture,
        )
        imageCapture = capture
        this.provider = provider
        bound = listOf(preview, capture)
    }

    /**
     * Hands the camera back. The binding is to the screen's lifecycle, which outlives this
     * controller, so leaving the picker has to say so explicitly — [rememberCameraController]
     * does it on the caller's behalf. Only what this controller bound is released, so a
     * scanner running elsewhere keeps its own.
     */
    fun unbind() {
        if (bound.isNotEmpty()) provider?.unbind(*bound.toTypedArray())
        bound = emptyList()
        provider = null
        imageCapture = null
        camera = null
    }

    fun setTorch(enabled: Boolean) {
        camera?.takeIf { it.cameraInfo.hasFlashUnit() }?.cameraControl?.enableTorch(enabled)
    }

    /** Takes a photo into app-private storage and returns the file. */
    suspend fun takePhoto(): File = suspendCoroutine { continuation ->
        val capture = imageCapture
        if (capture == null) {
            continuation.resumeWithException(IllegalStateException("Camera is not ready."))
            return@suspendCoroutine
        }

        val file = File(context.cacheDir, "capture-${System.currentTimeMillis()}.jpg")
        capture.takePicture(
            ImageCapture.OutputFileOptions.Builder(file).build(),
            ContextCompat.getMainExecutor(context),
            object : ImageCapture.OnImageSavedCallback {
                override fun onImageSaved(output: ImageCapture.OutputFileResults) {
                    continuation.resume(file)
                }

                override fun onError(exception: ImageCaptureException) {
                    continuation.resumeWithException(exception)
                }
            },
        )
    }

    private suspend fun awaitCameraProvider(): ProcessCameraProvider =
        suspendCoroutine { continuation ->
            val future = ProcessCameraProvider.getInstance(context)
            future.addListener(
                { continuation.resume(future.get()) },
                ContextCompat.getMainExecutor(context),
            )
        }
}

@Composable
fun rememberCameraController(): CameraController {
    val context = LocalContext.current
    val controller = remember(context) { CameraController(context) }
    DisposableEffect(controller) { onDispose { controller.unbind() } }
    return controller
}

@Composable
fun CameraPreview(
    onPreviewReady: (PreviewView) -> Unit,
    modifier: Modifier = Modifier,
) {
    AndroidView(
        modifier = modifier,
        factory = { context ->
            PreviewView(context).apply {
                scaleType = PreviewView.ScaleType.FILL_CENTER
                onPreviewReady(this)
            }
        },
    )
}
