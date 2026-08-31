package net.pollyspeople.storagelabels.core.user

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.data.api.UserApi
import net.pollyspeople.storagelabels.data.dto.UserPreferences
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Server-side user preferences: theme, whether to load images, and the box-code colour
 * pattern. Held here so the theme can react the moment they change, the way the web app's
 * AppThemeProvider does.
 */
@Singleton
class UserRepository @Inject constructor(
    private val userApi: UserApi,
) {
    private val _preferences = MutableStateFlow<UserPreferences?>(null)

    /** Null until loaded, so the theme follows the system setting in the meantime. */
    val preferences: StateFlow<UserPreferences?> = _preferences.asStateFlow()

    suspend fun load(): ApiResult<UserPreferences> =
        apiCall { userApi.getPreferences() }.also { result ->
            if (result is ApiResult.Success) _preferences.value = result.value
        }

    suspend fun update(preferences: UserPreferences): ApiResult<UserPreferences> =
        apiCall { userApi.updatePreferences(preferences) }.also { result ->
            if (result is ApiResult.Success) _preferences.value = result.value
        }

    /** Session ended: drop preferences so the next user doesn't inherit them. */
    fun reset() {
        _preferences.value = null
    }
}
