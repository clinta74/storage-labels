package net.pollyspeople.storagelabels.navigation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material3.DrawerValue
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalDrawerSheet
import androidx.compose.material3.ModalNavigationDrawer
import androidx.compose.material3.NavigationDrawerItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.rememberDrawerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation.NavDestination.Companion.hasRoute
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.toRoute
import kotlinx.coroutines.launch
import net.pollyspeople.storagelabels.core.permissions.LocalPermissions
import net.pollyspeople.storagelabels.data.dto.AuthMode
import net.pollyspeople.storagelabels.feature.auth.ChangePasswordScreen
import net.pollyspeople.storagelabels.feature.boxes.BoxDetailScreen
import net.pollyspeople.storagelabels.feature.boxes.BoxEditScreen
import net.pollyspeople.storagelabels.feature.items.ItemEditScreen
import net.pollyspeople.storagelabels.feature.locations.LocationDetailScreen
import net.pollyspeople.storagelabels.feature.locations.LocationUsersScreen
import net.pollyspeople.storagelabels.feature.locations.LocationsScreen
import net.pollyspeople.storagelabels.feature.preferences.PreferencesScreen
import kotlin.reflect.KClass

/**
 * The signed-in shell: drawer, title bar and snackbars, standing in for the web app's
 * navigation bar. Screens that arrive in later phases are placeholders that say so, rather
 * than dead entries.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppShell(
    accountName: String,
    authMode: AuthMode,
    onSignOut: () -> Unit,
    modifier: Modifier = Modifier,
    navController: NavHostController = rememberNavController(),
) {
    val permissions = LocalPermissions.current
    val drawerState = rememberDrawerState(DrawerValue.Closed)
    val snackbarHostState = remember { SnackbarHostState() }
    val scope = rememberCoroutineScope()

    val primary = remember(permissions) { PrimaryNavEntries.visibleTo(permissions) }
    val backStackEntry by navController.currentBackStackEntryAsState()
    val currentRoute = backStackEntry?.destination

    val showMessage: (String) -> Unit = { message ->
        scope.launch { snackbarHostState.showSnackbar(message) }
    }

    val navigateTo: (Route) -> Unit = { route ->
        scope.launch { drawerState.close() }
        navController.navigate(route) {
            popUpTo(Route.Locations) { inclusive = route == Route.Locations }
            launchSingleTop = true
        }
    }

    ModalNavigationDrawer(
        drawerState = drawerState,
        modifier = modifier,
        drawerContent = {
            ModalDrawerSheet {
                Text(
                    "Storage Labels",
                    style = MaterialTheme.typography.titleLarge,
                    modifier = Modifier.padding(16.dp),
                )
                Text(
                    accountName,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(start = 16.dp, bottom = 8.dp),
                )
                HorizontalDivider()

                primary.forEach { entry ->
                    NavigationDrawerItem(
                        label = { Text(entry.label) },
                        selected = currentRoute.matches(entry.route),
                        onClick = { navigateTo(entry.route) },
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 2.dp),
                    )
                }

                HorizontalDivider(Modifier.padding(vertical = 8.dp))

                AccountNavEntries.forEach { entry ->
                    NavigationDrawerItem(
                        label = { Text(entry.label) },
                        selected = currentRoute.matches(entry.route),
                        onClick = { navigateTo(entry.route) },
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 2.dp),
                    )
                }

                // In "None" mode there is no session to end.
                if (authMode == AuthMode.Local) {
                    NavigationDrawerItem(
                        label = { Text("Sign out") },
                        selected = false,
                        onClick = {
                            scope.launch { drawerState.close() }
                            onSignOut()
                        },
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 2.dp),
                    )
                }
            }
        },
    ) {
        Scaffold(
            topBar = {
                TopAppBar(
                    title = { Text(currentRoute.titleOrDefault()) },
                    navigationIcon = {
                        IconButton(onClick = { scope.launch { drawerState.open() } }) {
                            Icon(Icons.Filled.Menu, contentDescription = "Open navigation")
                        }
                    },
                )
            },
            snackbarHost = { SnackbarHost(snackbarHostState) },
        ) { padding ->
            Column(Modifier.padding(padding)) {
                if (authMode == AuthMode.None) {
                    NoAuthBanner()
                }
                NavHost(navController = navController, startDestination = Route.Locations) {
                    composable<Route.Locations> {
                        LocationsScreen(
                            onOpenLocation = { navController.navigate(Route.LocationDetail(it)) },
                            onManageUsers = { navController.navigate(Route.LocationUsers(it)) },
                            onMessage = showMessage,
                        )
                    }
                    composable<Route.LocationDetail> { entry ->
                        val route = entry.toRoute<Route.LocationDetail>()
                        LocationDetailScreen(
                            onOpenBox = { boxId ->
                                navController.navigate(Route.BoxDetail(route.locationId, boxId))
                            },
                            onAddBox = { navController.navigate(Route.BoxEdit(route.locationId)) },
                            onMessage = showMessage,
                        )
                    }
                    composable<Route.LocationUsers> {
                        LocationUsersScreen(onMessage = showMessage)
                    }
                    composable<Route.BoxEdit> {
                        BoxEditScreen(
                            onSaved = { message ->
                                showMessage(message)
                                navController.popBackStack()
                            },
                        )
                    }
                    composable<Route.BoxDetail> { entry ->
                        val route = entry.toRoute<Route.BoxDetail>()
                        BoxDetailScreen(
                            onEditBox = {
                                navController.navigate(Route.BoxEdit(route.locationId, route.boxId))
                            },
                            onAddItem = {
                                navController.navigate(Route.ItemEdit(route.locationId, route.boxId))
                            },
                            onEditItem = { itemId ->
                                navController.navigate(
                                    Route.ItemEdit(route.locationId, route.boxId, itemId),
                                )
                            },
                            onDeleted = { navController.popBackStack() },
                            onMoved = { destination ->
                                // The box now lives elsewhere, so go to where it went rather
                                // than back to a list it has left.
                                navController.popBackStack()
                                navController.navigate(Route.LocationDetail(destination))
                            },
                            onMessage = showMessage,
                        )
                    }
                    composable<Route.ItemEdit> {
                        ItemEditScreen(
                            onSaved = { message ->
                                showMessage(message)
                                navController.popBackStack()
                            },
                        )
                    }
                    composable<Route.Images> { ComingSoon("Images", "Phase 3") }
                    composable<Route.Labels> { ComingSoon("Labels", "Phase 4") }
                    composable<Route.CommonLocations> { ComingSoon("Common locations", "Phase 5") }
                    composable<Route.EncryptionKeys> { ComingSoon("Encryption keys", "Phase 5") }
                    composable<Route.Users> { ComingSoon("Users", "Phase 5") }
                    composable<Route.Preferences> { PreferencesScreen(onSaved = showMessage) }
                    composable<Route.ChangePassword> {
                        ChangePasswordScreen(onChanged = showMessage)
                    }
                }
            }
        }
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
private fun ComingSoon(title: String, phase: String) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(8.dp, Alignment.CenterVertically),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text(title, style = MaterialTheme.typography.headlineSmall)
        Text(
            "Arrives in $phase.",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}

private fun androidx.navigation.NavDestination?.matches(route: Route): Boolean {
    val kClass: KClass<out Route> = route::class
    return this?.hierarchy?.any { it.hasRoute(kClass) } == true
}

private val androidx.navigation.NavDestination.hierarchy: Sequence<androidx.navigation.NavDestination>
    get() = generateSequence(this) { it.parent }

private fun androidx.navigation.NavDestination?.titleOrDefault(): String = when {
    this == null -> "Storage Labels"
    hasRoute(Route.Images::class) -> "Images"
    hasRoute(Route.Labels::class) -> "Labels"
    hasRoute(Route.CommonLocations::class) -> "Common locations"
    hasRoute(Route.EncryptionKeys::class) -> "Encryption keys"
    hasRoute(Route.Users::class) -> "Users"
    hasRoute(Route.Preferences::class) -> "Preferences"
    hasRoute(Route.ChangePassword::class) -> "Change password"
    else -> "Locations"
}
