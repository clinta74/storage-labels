package net.pollyspeople.storagelabels

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.ui.Modifier
import dagger.hilt.android.AndroidEntryPoint
import net.pollyspeople.storagelabels.core.ui.StorageLabelsTheme
import net.pollyspeople.storagelabels.feature.SessionGate

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            StorageLabelsTheme {
                Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
                    SessionGate(modifier = Modifier.padding(innerPadding))
                }
            }
        }
    }
}
