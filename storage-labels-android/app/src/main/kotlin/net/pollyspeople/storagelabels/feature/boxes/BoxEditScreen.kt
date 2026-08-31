package net.pollyspeople.storagelabels.feature.boxes

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PhotoCamera
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.ui.AuthenticatedImage
import net.pollyspeople.storagelabels.core.ui.LoadingBox

@Composable
fun BoxEditScreen(
    onSaved: (String) -> Unit,
    onCancel: () -> Unit,
    onScanCode: (() -> Unit)? = null,
    onPickImage: (() -> Unit)? = null,
    viewModel: BoxEditViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    if (state.loading) {
        LoadingBox()
        return
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .imePadding()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Text(
            if (state.isNew) "Add box" else "Edit box",
            style = MaterialTheme.typography.headlineSmall,
        )

        if (state.error != null) {
            Text(
                state.error!!,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.error,
            )
        }

        OutlinedTextField(
            value = state.code,
            onValueChange = viewModel::onCodeChange,
            label = { Text("Code") },
            singleLine = true,
            enabled = !state.saving,
            isError = state.fieldErrors.containsKey(BoxEditViewModel.FIELD_CODE),
            supportingText = state.fieldErrors[BoxEditViewModel.FIELD_CODE]?.let { { Text(it) } },
            trailingIcon = onScanCode?.let {
                {
                    IconButton(onClick = it, enabled = !state.saving) {
                        Icon(Icons.Filled.QrCodeScanner, contentDescription = "Scan a label")
                    }
                }
            },
            modifier = Modifier.fillMaxWidth(),
        )

        OutlinedTextField(
            value = state.name,
            onValueChange = viewModel::onNameChange,
            label = { Text("Name") },
            singleLine = true,
            enabled = !state.saving,
            isError = state.fieldErrors.containsKey(BoxEditViewModel.FIELD_NAME),
            supportingText = state.fieldErrors[BoxEditViewModel.FIELD_NAME]?.let { { Text(it) } },
            modifier = Modifier.fillMaxWidth(),
        )

        OutlinedTextField(
            value = state.description,
            onValueChange = viewModel::onDescriptionChange,
            label = { Text("Description") },
            enabled = !state.saving,
            minLines = 2,
            modifier = Modifier.fillMaxWidth(),
        )

        if (!state.imageUrl.isNullOrBlank()) {
            AuthenticatedImage(
                url = state.imageUrl,
                contentDescription = "Selected photo",
                showImages = state.showImages,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(180.dp),
            )
        }

        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            if (onPickImage != null) {
                TextButton(onClick = onPickImage, enabled = !state.saving) {
                    Icon(Icons.Filled.PhotoCamera, contentDescription = null)
                    Text(if (state.imageUrl.isNullOrBlank()) " Add photo" else " Change photo")
                }
            }
            if (!state.imageUrl.isNullOrBlank()) {
                TextButton(
                    onClick = { viewModel.onImageSelected(null, null) },
                    enabled = !state.saving,
                ) {
                    Text("Remove photo")
                }
            }
        }

        Row(
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier.fillMaxWidth(),
        ) {
            OutlinedButton(
                onClick = onCancel,
                enabled = !state.saving,
                modifier = Modifier.weight(1f),
            ) {
                Text("Cancel")
            }
            Button(
                onClick = { viewModel.save(onSaved) },
                enabled = !state.saving,
                modifier = Modifier.weight(1f),
            ) {
                Text(if (state.isNew) "Add box" else "Save")
            }
        }
    }
}
