package net.pollyspeople.storagelabels.data.api

import net.pollyspeople.storagelabels.data.dto.AuthConfigResponse
import net.pollyspeople.storagelabels.data.dto.AuthenticationResult
import net.pollyspeople.storagelabels.data.dto.ChangePasswordRequest
import net.pollyspeople.storagelabels.data.dto.LoginRequest
import net.pollyspeople.storagelabels.data.dto.RegisterRequest
import net.pollyspeople.storagelabels.data.dto.UserInfoResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST

interface AuthApi {

    @GET("auth/config")
    suspend fun getConfig(): AuthConfigResponse

    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): AuthenticationResult

    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): AuthenticationResult

    @GET("auth/me")
    suspend fun getCurrentUser(): UserInfoResponse

    @POST("auth/refresh")
    suspend fun refresh(): AuthenticationResult

    @POST("auth/logout")
    suspend fun logout(): Response<Unit>

    @POST("auth/change-password")
    suspend fun changePassword(@Body request: ChangePasswordRequest): Response<Unit>
}
