package net.pollyspeople.storagelabels.feature.images

import android.Manifest
import android.content.pm.PackageManager
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraSelector
import androidx.camera.view.PreviewView
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Cameraswitch
import androidx.compose.material.icons.filled.FlashOff
import androidx.compose.material.icons.filled.FlashOn
import androidx.compose.material.icons.filled.PhotoLibrary
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Tab
import androidx.compose.material3.PrimaryTabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.camera.CameraPreview
import net.pollyspeople.storagelabels.core.camera.rememberCameraController
import net.pollyspeople.storagelabels.core.ui.AuthenticatedImage
import net.pollyspeople.storagelabels.core.ui.EmptyState

/**
 * Picking a photo for a box or item: either one already uploaded, or a new one from the
 * camera or the device's picker. Mirrors the web app's image selector, which offers the same
 * two choices.
 */
@Composable
fun ImagePickerScreen(
    onPicked: (imageUrl: String, imageId: String) -> Unit,
    onCancel: () -> Unit,
    viewModel: ImagesViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var tab by remember { mutableIntStateOf(0) }

    // Nothing else loads the list here, and the state starts out loading, so without this the
    // "Your photos" tab sits blank forever. On resume, so a photo taken meanwhile shows up.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) { viewModel.refresh() }

    Column(Modifier.fillMaxSize()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 8.dp),
            horizontalArrangement = Arrangement.End,
        ) {
            TextButton(onClick = onCancel) { Text("Cancel") }
        }

        PrimaryTabRow(selectedTabIndex = tab) {
            Tab(selected = tab == 0, onClick = { tab = 0 }, text = { Text("Your photos") })
            Tab(selected = tab == 1, onClick = { tab = 1 }, text = { Text("Take a photo") })
        }

        when (tab) {
            0 -> ExistingImages(
                state = state,
                onSelect = { image -> onPicked(image.resolvedUrl, image.imageId) },
            )

            else -> CaptureTab(
                uploading = state.uploading,
                onCaptured = { file -> viewModel.uploadFile(file) { url, id -> onPicked(url, id) } },
                onPickedFromGallery = { uri, name ->
                    viewModel.uploadUri(uri, name) { url, id -> onPicked(url, id) }
                },
            )
        }
    }
}

@Composable
private fun ExistingImages(
    state: ImagesState,
    onSelect: (net.pollyspeople.storagelabels.data.dto.ImageMetadata) -> Unit,
) {
    if (state.images.isEmpty()) {
        if (state.loading) {
            Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        } else {
            EmptyState(
                title = "No photos yet",
                message = "Take one on the next tab and it'll appear here.",
            )
        }
        return
    }

    LazyVerticalGrid(
        columns = GridCells.Adaptive(minSize = 120.dp),
        contentPadding = androidx.compose.foundation.layout.PaddingValues(12.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp),
        modifier = Modifier.fillMaxSize(),
    ) {
        items(state.images, key = { it.imageId }) { image ->
            Card(onClick = { onSelect(image) }) {
                AuthenticatedImage(
                    url = image.resolvedUrl,
                    contentDescription = image.fileName,
                    showImages = state.showImages,
                    modifier = Modifier
                        .fillMaxWidth()
                        .aspectRatio(1f),
                )
            }
        }
    }
}

@Composable
private fun CaptureTab(
    uploading: Boolean,
    onCaptured: (java.io.File) -> Unit,
    onPickedFromGallery: (android.net.Uri, String) -> Unit,
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val controller = rememberCameraController()
    val scope = androidx.compose.runtime.rememberCoroutineScope()

    var hasPermission by remember {
        mutableStateOf(
            ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) ==
                PackageManager.PERMISSION_GRANTED,
        )
    }
    var lensFacing by remember { mutableIntStateOf(CameraSelector.LENS_FACING_BACK) }
    var torchOn by remember { mutableStateOf(false) }
    var previewView by remember { mutableStateOf<PreviewView?>(null) }
    var error by remember { mutableStateOf<String?>(null) }

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission(),
    ) { granted -> hasPermission = granted }

    val galleryLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.PickVisualMedia(),
    ) { uri ->
        if (uri != null) onPickedFromGallery(uri, "picked-${System.currentTimeMillis()}.jpg")
    }

    LaunchedEffect(Unit) {
        if (!hasPermission) permissionLauncher.launch(Manifest.permission.CAMERA)
    }

    LaunchedEffect(hasPermission, previewView, lensFacing) {
        val view = previewView
        if (hasPermission && view != null) {
            runCatching { controller.bind(lifecycleOwner, view, lensFacing) }
                .onFailure { error = "Couldn't start the camera." }
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        if (!hasPermission) {
            EmptyState(
                title = "Camera access needed",
                message = "Allow the camera to photograph a box, or pick an existing picture instead.",
                actionLabel = "Allow camera",
                onAction = { permissionLauncher.launch(Manifest.permission.CAMERA) },
                modifier = Modifier.height(240.dp),
            )
        } else {
            Box(
                Modifier
                    .fillMaxWidth()
                    .height(320.dp),
            ) {
                CameraPreview(
                    onPreviewReady = { previewView = it },
                    modifier = Modifier.fillMaxSize(),
                )
                if (uploading) {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                }
            }

            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                IconButton(
                    onClick = {
                        lensFacing = if (lensFacing == CameraSelector.LENS_FACING_BACK) {
                            CameraSelector.LENS_FACING_FRONT
                        } else {
                            CameraSelector.LENS_FACING_BACK
                        }
                        torchOn = false
                    },
                ) {
                    Icon(Icons.Filled.Cameraswitch, contentDescription = "Switch camera")
                }
                IconButton(
                    onClick = {
                        torchOn = !torchOn
                        controller.setTorch(torchOn)
                    },
                ) {
                    Icon(
                        if (torchOn) Icons.Filled.FlashOn else Icons.Filled.FlashOff,
                        contentDescription = if (torchOn) "Turn off the light" else "Turn on the light",
                    )
                }
            }

            Button(
                onClick = {
                    scope.launch {
                        runCatching { controller.takePhoto() }
                            .onSuccess(onCaptured)
                            .onFailure { error = "Couldn't take that photo." }
                    }
                },
                enabled = !uploading,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(if (uploading) "Uploading" else "Take photo")
            }
        }

        TextButton(
            onClick = {
                galleryLauncher.launch(
                    PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly),
                )
            },
            enabled = !uploading,
        ) {
            Icon(Icons.Filled.PhotoLibrary, contentDescription = null)
            Text(" Choose an existing picture")
        }

        error?.let {
            Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodyMedium)
        }
    }
}
