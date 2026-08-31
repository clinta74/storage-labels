package net.pollyspeople.storagelabels.feature.preferences

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.selection.selectableGroup
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.data.dto.UserPreferences

@Composable
fun PreferencesScreen(
    onSaved: (String) -> Unit,
    viewModel: PreferencesViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    LaunchedEffect(state.savedAt) {
        if (state.savedAt != null) {
            viewModel.acknowledgeSaved()
            onSaved("Preferences saved.")
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .imePadding()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text("Preferences", style = MaterialTheme.typography.headlineSmall)

        if (state.error != null) {
            Text(
                state.error!!,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.error,
            )
        }

        Text("Theme", style = MaterialTheme.typography.titleMedium)
        Column(Modifier.selectableGroup()) {
            ThemeOption(
                label = "Light",
                selected = !state.preferences.isDark,
                enabled = !state.busy,
                onSelect = { viewModel.onThemeChange(UserPreferences.THEME_LIGHT) },
            )
            ThemeOption(
                label = "Dark",
                selected = state.preferences.isDark,
                enabled = !state.busy,
                onSelect = { viewModel.onThemeChange(UserPreferences.THEME_DARK) },
            )
        }

        HorizontalDivider()

        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Switch(
                checked = state.preferences.showImages,
                onCheckedChange = viewModel::onShowImagesChange,
                enabled = !state.busy,
            )
            Column {
                Text("Show images", style = MaterialTheme.typography.bodyMedium)
                Text(
                    "Off shows placeholders instead of downloading photos, which saves data.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }

        HorizontalDivider()

        Text("Box code colours", style = MaterialTheme.typography.titleMedium)
        OutlinedTextField(
            value = state.preferences.codeColorPattern,
            onValueChange = viewModel::onCodeColorPatternChange,
            label = { Text("Colour pattern") },
            placeholder = { Text("3:primary,2:secondary,*,4:error") },
            singleLine = true,
            enabled = !state.busy,
            supportingText = {
                Text(
                    "length:colour pairs, with * to skip the middle. " +
                        "Colours: primary, secondary, error, warning, info, success.",
                )
            },
            modifier = Modifier.fillMaxWidth(),
        )

        Button(
            onClick = viewModel::save,
            enabled = !state.busy,
            modifier = Modifier.fillMaxWidth(),
        ) {
            if (state.busy) {
                CircularProgressIndicator(
                    modifier = Modifier.width(18.dp),
                    strokeWidth = 2.dp,
                    color = MaterialTheme.colorScheme.onPrimary,
                )
                Spacer(Modifier.width(12.dp))
                Text("Saving")
            } else {
                Text("Save preferences")
            }
        }
    }
}

@Composable
private fun ThemeOption(
    label: String,
    selected: Boolean,
    enabled: Boolean,
    onSelect: () -> Unit,
) {
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        RadioButton(selected = selected, onClick = onSelect, enabled = enabled)
        Text(label, style = MaterialTheme.typography.bodyMedium)
    }
}
