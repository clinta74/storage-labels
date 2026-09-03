package net.pollyspeople.storagelabels.data.api

import net.pollyspeople.storagelabels.data.dto.AddUserLocationRequest
import net.pollyspeople.storagelabels.data.dto.Box
import net.pollyspeople.storagelabels.data.dto.BoxRequest
import net.pollyspeople.storagelabels.data.dto.Item
import net.pollyspeople.storagelabels.data.dto.ItemRequest
import net.pollyspeople.storagelabels.data.dto.LocationRequest
import net.pollyspeople.storagelabels.data.dto.LocationUser
import net.pollyspeople.storagelabels.data.dto.MoveBoxRequest
import net.pollyspeople.storagelabels.data.dto.StorageLocation
import net.pollyspeople.storagelabels.data.dto.UpdateUserLocationRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface LocationApi {

    @GET("location")
    suspend fun getLocations(): List<StorageLocation>

    @GET("location/{locationId}")
    suspend fun getLocation(@Path("locationId") locationId: Long): StorageLocation

    @POST("location")
    suspend fun createLocation(@Body request: LocationRequest): StorageLocation

    @PUT("location/{locationId}")
    suspend fun updateLocation(
        @Path("locationId") locationId: Long,
        @Body request: LocationRequest,
    ): StorageLocation

    /** [force] deletes a location that still holds boxes, as the web app's checkbox does. */
    @DELETE("location/{locationId}")
    suspend fun deleteLocation(
        @Path("locationId") locationId: Long,
        @Query("force") force: Boolean? = null,
    ): Response<Unit>

    @GET("location/{locationId}/users")
    suspend fun getLocationUsers(@Path("locationId") locationId: Long): List<LocationUser>

    @POST("location/{locationId}/users")
    suspend fun addUserToLocation(
        @Path("locationId") locationId: Long,
        @Body request: AddUserLocationRequest,
    ): LocationUser

    @PUT("location/{locationId}/users/{userId}")
    suspend fun updateUserAccess(
        @Path("locationId") locationId: Long,
        @Path("userId") userId: String,
        @Body request: UpdateUserLocationRequest,
    ): LocationUser

    @DELETE("location/{locationId}/users/{userId}")
    suspend fun removeUserFromLocation(
        @Path("locationId") locationId: Long,
        @Path("userId") userId: String,
    ): Response<Unit>
}

interface BoxApi {

    @GET("box/location/{locationId}/")
    suspend fun getBoxesByLocation(@Path("locationId") locationId: Long): List<Box>

    @GET("box/{boxId}")
    suspend fun getBox(@Path("boxId") boxId: String): Box

    @POST("box")
    suspend fun createBox(@Body request: BoxRequest): Box

    @PUT("box/{boxId}")
    suspend fun updateBox(@Path("boxId") boxId: String, @Body request: BoxRequest): Box

    @PUT("box/{boxId}/move")
    suspend fun moveBox(@Path("boxId") boxId: String, @Body request: MoveBoxRequest): Box

    @DELETE("box/{boxId}")
    suspend fun deleteBox(
        @Path("boxId") boxId: String,
        @Query("force") force: Boolean? = null,
    ): Response<Unit>
}

interface ItemApi {

    @GET("item/box/{boxId}/")
    suspend fun getItemsByBox(@Path("boxId") boxId: String): List<Item>

    @GET("item/{itemId}")
    suspend fun getItem(@Path("itemId") itemId: String): Item

    @POST("item")
    suspend fun createItem(@Body request: ItemRequest): Item

    @PUT("item/{itemId}")
    suspend fun updateItem(@Path("itemId") itemId: String, @Body request: ItemRequest): Item

    @DELETE("item/{itemId}")
    suspend fun deleteItem(@Path("itemId") itemId: String): Response<Unit>
}
