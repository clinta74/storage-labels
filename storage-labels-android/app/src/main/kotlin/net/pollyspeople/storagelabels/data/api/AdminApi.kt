package net.pollyspeople.storagelabels.data.api

import net.pollyspeople.storagelabels.data.dto.AdminResetPasswordRequest
import net.pollyspeople.storagelabels.data.dto.CommonLocation
import net.pollyspeople.storagelabels.data.dto.CommonLocationRequest
import net.pollyspeople.storagelabels.data.dto.CreateEncryptionKeyRequest
import net.pollyspeople.storagelabels.data.dto.EncryptionKey
import net.pollyspeople.storagelabels.data.dto.EncryptionKeyRotation
import net.pollyspeople.storagelabels.data.dto.EncryptionKeyStats
import net.pollyspeople.storagelabels.data.dto.RotationProgress
import net.pollyspeople.storagelabels.data.dto.StartRotationRequest
import net.pollyspeople.storagelabels.data.dto.UpdateUserRoleRequest
import net.pollyspeople.storagelabels.data.dto.UserWithRoles
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface CommonLocationApi {

    @GET("common-location")
    suspend fun getCommonLocations(): List<CommonLocation>

    @POST("common-location")
    suspend fun createCommonLocation(@Body request: CommonLocationRequest): CommonLocation

    @DELETE("common-location/{commonLocationId}")
    suspend fun deleteCommonLocation(
        @Path("commonLocationId") commonLocationId: Int,
    ): Response<Unit>
}

interface AdminUserApi {

    @GET("user/all")
    suspend fun getAllUsers(): List<UserWithRoles>

    @PUT("user/{userId}/role")
    suspend fun updateRole(
        @Path("userId") userId: String,
        @Body request: UpdateUserRoleRequest,
    ): Response<Unit>

    @POST("auth/admin/reset-password")
    suspend fun resetPassword(@Body request: AdminResetPasswordRequest): Response<Unit>

    @DELETE("user/{userId}")
    suspend fun deleteUser(@Path("userId") userId: String): Response<Unit>
}

interface EncryptionKeyApi {

    @GET("admin/encryption-keys")
    suspend fun getKeys(): List<EncryptionKey>

    @POST("admin/encryption-keys")
    suspend fun createKey(@Body request: CreateEncryptionKeyRequest): EncryptionKey

    @GET("admin/encryption-keys/{kid}/stats")
    suspend fun getStats(@Path("kid") kid: Int): EncryptionKeyStats

    /** Activating with [autoRotate] starts re-encrypting everything held under the old key. */
    @PUT("admin/encryption-keys/{kid}/activate")
    suspend fun activate(
        @Path("kid") kid: Int,
        @Query("autoRotate") autoRotate: Boolean = true,
    ): Response<Unit>

    @PUT("admin/encryption-keys/{kid}/retire")
    suspend fun retire(@Path("kid") kid: Int): Response<Unit>

    @POST("admin/encryption-keys/rotate")
    suspend fun startRotation(@Body request: StartRotationRequest): Response<Unit>

    @GET("admin/encryption-keys/rotations")
    suspend fun getRotations(@Query("status") status: String? = null): List<EncryptionKeyRotation>

    @GET("admin/encryption-keys/rotations/{rotationId}")
    suspend fun getRotationProgress(@Path("rotationId") rotationId: String): RotationProgress

    @DELETE("admin/encryption-keys/rotations/{rotationId}")
    suspend fun cancelRotation(@Path("rotationId") rotationId: String): Response<Unit>
}
