package net.pollyspeople.storagelabels.feature.auth

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle

@Composable
fun RegisterScreen(
    onBackToSignIn: () -> Unit,
    viewModel: RegisterViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val errors = state.fieldErrors

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .imePadding()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        Text("Create an account", style = MaterialTheme.typography.headlineSmall)

        if (state.error != null) {
            Text(
                state.error!!,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.error,
            )
        }

        Field(
            value = state.firstName,
            onValueChange = viewModel::onFirstNameChange,
            label = "First name",
            error = errors[RegisterViewModel.FIELD_FIRST_NAME],
            enabled = !state.submitting,
        )
        Field(
            value = state.lastName,
            onValueChange = viewModel::onLastNameChange,
            label = "Last name",
            error = errors[RegisterViewModel.FIELD_LAST_NAME],
            enabled = !state.submitting,
        )
        Field(
            value = state.email,
            onValueChange = viewModel::onEmailChange,
            label = "Email",
            error = errors[RegisterViewModel.FIELD_EMAIL],
            enabled = !state.submitting,
            keyboardType = KeyboardType.Email,
        )
        Field(
            value = state.username,
            onValueChange = viewModel::onUsernameChange,
            label = "Username",
            error = errors[RegisterViewModel.FIELD_USERNAME],
            enabled = !state.submitting,
        )
        Field(
            value = state.password,
            onValueChange = viewModel::onPasswordChange,
            label = "Password",
            error = errors[RegisterViewModel.FIELD_PASSWORD],
            enabled = !state.submitting,
            keyboardType = KeyboardType.Password,
            masked = true,
        )
        Field(
            value = state.confirmPassword,
            onValueChange = viewModel::onConfirmPasswordChange,
            label = "Confirm password",
            error = errors[RegisterViewModel.FIELD_CONFIRM],
            enabled = !state.submitting,
            keyboardType = KeyboardType.Password,
            masked = true,
            imeAction = ImeAction.Go,
        )

        Button(
            onClick = viewModel::submit,
            enabled = !state.submitting,
            modifier = Modifier.fillMaxWidth(),
        ) {
            if (state.submitting) {
                CircularProgressIndicator(
                    modifier = Modifier.width(18.dp),
                    strokeWidth = 2.dp,
                    color = MaterialTheme.colorScheme.onPrimary,
                )
                Spacer(Modifier.width(12.dp))
                Text("Creating account")
            } else {
                Text("Create account")
            }
        }

        TextButton(
            onClick = onBackToSignIn,
            enabled = !state.submitting,
            modifier = Modifier.align(Alignment.CenterHorizontally),
        ) {
            Text("I already have an account")
        }
    }
}

@Composable
private fun Field(
    value: String,
    onValueChange: (String) -> Unit,
    label: String,
    error: String?,
    enabled: Boolean,
    keyboardType: KeyboardType = KeyboardType.Text,
    masked: Boolean = false,
    imeAction: ImeAction = ImeAction.Next,
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        label = { Text(label) },
        singleLine = true,
        enabled = enabled,
        isError = error != null,
        supportingText = error?.let { { Text(it) } },
        visualTransformation = if (masked) PasswordVisualTransformation() else androidx.compose.ui.text.input.VisualTransformation.None,
        keyboardOptions = KeyboardOptions(keyboardType = keyboardType, imeAction = imeAction),
        modifier = Modifier.fillMaxWidth(),
    )
}
