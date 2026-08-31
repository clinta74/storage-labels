package net.pollyspeople.storagelabels

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.hilt.lifecycle.viewmodel.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import dagger.hilt.android.AndroidEntryPoint
import net.pollyspeople.storagelabels.core.ui.StorageLabelsTheme
import net.pollyspeople.storagelabels.feature.SessionGate
import net.pollyspeople.storagelabels.feature.SessionViewModel

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            val sessionViewModel: SessionViewModel = hiltViewModel()
            val preferences by sessionViewModel.preferences.collectAsStateWithLifecycle()

            // Theme follows the server-side preference once loaded, as the web app does,
            // and the system setting until then.
            StorageLabelsTheme(darkTheme = preferences?.isDark ?: isSystemInDarkTheme()) {
                Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
                    SessionGate(
                        modifier = Modifier.padding(innerPadding),
                        viewModel = sessionViewModel,
                    )
                }
            }
        }
    }
}
