package net.pollyspeople.storagelabels.data.api

import net.pollyspeople.storagelabels.data.dto.UserPreferences
import net.pollyspeople.storagelabels.data.dto.UserResponse
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PUT

interface UserApi {

    @GET("user")
    suspend fun getUser(): UserResponse

    @GET("user/preferences")
    suspend fun getPreferences(): UserPreferences

    @PUT("user/preferences")
    suspend fun updatePreferences(@Body preferences: UserPreferences): UserPreferences
}
