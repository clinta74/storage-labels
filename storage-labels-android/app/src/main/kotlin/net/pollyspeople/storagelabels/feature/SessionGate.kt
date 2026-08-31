package net.pollyspeople.storagelabels.feature

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import net.pollyspeople.storagelabels.core.auth.SessionState
import net.pollyspeople.storagelabels.core.permissions.LocalPermissions
import net.pollyspeople.storagelabels.core.ui.userMessage
import net.pollyspeople.storagelabels.data.dto.AuthMode
import net.pollyspeople.storagelabels.feature.auth.LoginScreen
import net.pollyspeople.storagelabels.feature.server.ServerSetupScreen

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

    Box(modifier = modifier.fillMaxSize()) {
    when (val current = session) {
        SessionState.Loading -> LoadingScreen()

        SessionState.NoServer -> ServerSetupScreen()

        is SessionState.ServerUnreachable -> ServerUnreachableScreen(
            address = current.baseUrl,
            message = current.error.userMessage(),
            onRetry = viewModel::retry,
            onChangeServer = viewModel::changeServer,
        )

        is SessionState.SignedOut -> LoginScreen(
            serverAddress = viewModel.serverAddress(),
            allowRegistration = current.config.allowRegistration,
            notice = current.notice,
            onRegisterClick = { /* Registration screen lands with the rest of Phase 1. */ },
        )

        is SessionState.SignedIn -> CompositionLocalProvider(
            LocalPermissions provides current.permissions,
        ) {
            Column(modifier = Modifier.fillMaxSize()) {
                if (current.authMode == AuthMode.None) {
                    NoAuthBanner()
                }
                HomePlaceholder(
                    name = current.user.fullName?.takeIf(String::isNotBlank)
                        ?: current.user.username.takeIf(String::isNotBlank)
                        ?: "there",
                    onSignOut = viewModel::signOut,
                    canSignOut = current.authMode == AuthMode.Local,
                )
            }
        }
    }
    }
}

@Composable
private fun LoadingScreen() {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        CircularProgressIndicator()
    }
}

/** The web UI shows this warning whenever the API runs without authentication. */
@Composable
private fun NoAuthBanner() {
    Surface(
        color = MaterialTheme.colorScheme.errorContainer,
        contentColor = MaterialTheme.colorScheme.onErrorContainer,
        modifier = Modifier.fillMaxWidth(),
    ) {
        Text(
            "Running without authentication — everyone has full access.",
            style = MaterialTheme.typography.bodySmall,
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
        )
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
        Text(message, style = MaterialTheme.typography.bodyMedium)
        Button(onClick = onRetry) { Text("Try again") }
        Button(onClick = onChangeServer) { Text("Change server") }
    }
}

@Composable
private fun HomePlaceholder(name: String, onSignOut: () -> Unit, canSignOut: Boolean) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp, Alignment.CenterVertically),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text("Signed in as $name", style = MaterialTheme.typography.headlineSmall)
        Text(
            "Locations, boxes and items arrive in Phase 2.",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        if (canSignOut) {
            Button(onClick = onSignOut) { Text("Sign out") }
        }
    }
}
