package net.pollyspeople.storagelabels.feature.items

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PhotoCamera
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.ui.ErrorBanner
import net.pollyspeople.storagelabels.core.ui.LoadingBox
import net.pollyspeople.storagelabels.core.ui.ZoomableAuthenticatedImage

@Composable
fun ItemEditScreen(
    onSaved: (String) -> Unit,
    onCancel: () -> Unit,
    onPickImage: (() -> Unit)? = null,
    viewModel: ItemEditViewModel = hiltViewModel(),
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
            if (state.isNew) "Add item" else "Edit item",
            style = MaterialTheme.typography.headlineSmall,
        )

        state.error?.let {
            ErrorBanner(it, contentPadding = PaddingValues(0.dp))
        }

        OutlinedTextField(
            value = state.name,
            onValueChange = viewModel::onNameChange,
            label = { Text("Name") },
            singleLine = true,
            keyboardOptions = KeyboardOptions(
                capitalization = KeyboardCapitalization.Sentences,
                imeAction = ImeAction.Next,
            ),
            enabled = !state.saving,
            isError = state.nameError != null,
            supportingText = state.nameError?.let { { Text(it) } },
            modifier = Modifier.fillMaxWidth(),
        )

        OutlinedTextField(
            value = state.description,
            onValueChange = viewModel::onDescriptionChange,
            label = { Text("Description") },
            enabled = !state.saving,
            minLines = 2,
            // Multi-line, so Enter stays a newline; only the capitalisation is worth setting.
            keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Sentences),
            supportingText = { Text("Searchable — describe it the way you'd look for it.") },
            modifier = Modifier.fillMaxWidth(),
        )

        if (!state.imageUrl.isNullOrBlank()) {
            ZoomableAuthenticatedImage(
                url = state.imageUrl,
                contentDescription = "Selected photo",
                showImages = state.showImages,
                // You are checking which photo you picked, so show all of it, at a size
                // worth looking at: the frame takes the picture's own shape between bounds
                // rather than cropping a portrait one down to a band. Tap for a closer look.
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(min = 180.dp, max = 320.dp),
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
                Text(if (state.isNew) "Add item" else "Save")
            }
        }
    }
}
