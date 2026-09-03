package net.pollyspeople.storagelabels.feature

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawing
import androidx.compose.foundation.layout.windowInsetsPadding
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.auth.SessionState
import net.pollyspeople.storagelabels.core.permissions.LocalPermissions
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.feature.auth.LoginScreen
import net.pollyspeople.storagelabels.feature.auth.RegisterScreen
import net.pollyspeople.storagelabels.feature.server.ServerSetupScreen
import net.pollyspeople.storagelabels.navigation.AppShell

/**
 * Decides which of the app's top-level states the user is in, the way the web client's
 * AppRoutes does: setup, sign-in, or the app proper.
 */
@Composable
fun SessionGate(
    modifier: Modifier = Modifier,
    viewModel: SessionViewModel = hiltViewModel(),
) {
    val session by viewModel.state.collectAsStateWithLifecycle()

    // Sign-in and registration are the only screens outside the shell, so they swap here
    // rather than through the navigation graph, which only exists once signed in.
    var showRegister by remember { mutableStateOf(false) }

    Box(modifier = modifier.fillMaxSize()) {
        when (val current = session) {
            SessionState.Loading -> OutsideShell { LoadingScreen() }

            SessionState.NoServer -> OutsideShell { ServerSetupScreen() }

            is SessionState.ServerUnreachable -> OutsideShell {
                ServerUnreachableScreen(
                    address = current.baseUrl,
                    message = current.error.userMessage(),
                    onRetry = viewModel::retry,
                    onChangeServer = viewModel::changeServer,
                )
            }

            is SessionState.SignedOut -> OutsideShell {
                if (showRegister && current.config.allowRegistration) {
                    RegisterScreen(onBackToSignIn = { showRegister = false })
                } else {
                    LoginScreen(
                        serverAddress = viewModel.serverAddress(),
                        allowRegistration = current.config.allowRegistration,
                        notice = current.notice,
                        onRegisterClick = { showRegister = true },
                    )
                }
            }

            is SessionState.SignedIn -> CompositionLocalProvider(
                LocalPermissions provides current.permissions,
            ) {
                AppShell(
                    accountName = current.user.fullName?.takeIf(String::isNotBlank)
                        ?: current.user.username.takeIf(String::isNotBlank)
                        ?: current.user.email,
                    accountEmail = current.user.email,
                    authMode = current.authMode,
                    onSignOut = viewModel::signOut,
                )
            }
        }
    }
}

/**
 * Sign-in, registration and server setup have no app bar of their own, so they clear the
 * status and navigation bars here. Inside the shell that is the Scaffold's job.
 */
@Composable
private fun OutsideShell(content: @Composable () -> Unit) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .windowInsetsPadding(WindowInsets.safeDrawing),
    ) {
        content()
    }
}

@Composable
private fun LoadingScreen() {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        CircularProgressIndicator()
    }
}

@Composable
private fun ServerUnreachableScreen(
    address: String,
    message: String,
    onRetry: () -> Unit,
    onChangeServer: () -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp, Alignment.CenterVertically),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text("Can't reach the server", style = MaterialTheme.typography.headlineSmall)
        Text(address, style = MaterialTheme.typography.bodySmall)
        Text(
            message,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        Button(onClick = onRetry) { Text("Try again") }
        TextButton(onClick = onChangeServer) { Text("Change server") }
    }
}
