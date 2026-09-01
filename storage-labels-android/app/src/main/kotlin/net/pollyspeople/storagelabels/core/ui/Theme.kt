package net.pollyspeople.storagelabels.core.ui

import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.dynamicDarkColorScheme
import androidx.compose.material3.dynamicLightColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext

private val LightColors = lightColorScheme(
    primary = Color(0xFF0E6355),
    onPrimary = Color.White,
    primaryContainer = Color(0xFFDEEBE6),
    onPrimaryContainer = Color(0xFF04241E),
    secondary = Color(0xFF1F4FA8),
    onSecondary = Color.White,
    error = Color(0xFF8C1D18),
)

private val DarkColors = darkColorScheme(
    primary = Color(0xFF5FD3B8),
    onPrimary = Color(0xFF00382E),
    primaryContainer = Color(0xFF17332C),
    onPrimaryContainer = Color(0xFF8FE6CF),
    secondary = Color(0xFF8FB4FF),
    onSecondary = Color(0xFF0B1F45),
    error = Color(0xFFF2B8B5),
)

/**
 * The web UI stores theme as a server-side user preference ("light" / "dark"), so the app
 * takes [darkTheme] from that preference once the user is loaded and falls back to the
 * system setting before then.
 *
 * [dynamicColor] is off by default: wallpaper colours would replace the palette below on
 * every Android 12+ device, and Storage Labels shares its look with the web app.
 */
@Composable
fun StorageLabelsTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    dynamicColor: Boolean = false,
    content: @Composable () -> Unit,
) {
    val colorScheme = when {
        dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> {
            val context = LocalContext.current
            if (darkTheme) dynamicDarkColorScheme(context) else dynamicLightColorScheme(context)
        }
        darkTheme -> DarkColors
        else -> LightColors
    }

    MaterialTheme(colorScheme = colorScheme, content = content)
}
