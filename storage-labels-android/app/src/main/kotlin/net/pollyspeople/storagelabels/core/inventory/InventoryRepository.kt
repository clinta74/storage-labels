package net.pollyspeople.storagelabels.core.inventory

import net.pollyspeople.storagelabels.core.result.ApiResult
import net.pollyspeople.storagelabels.core.result.apiCall
import net.pollyspeople.storagelabels.data.api.BoxApi
import net.pollyspeople.storagelabels.data.api.ItemApi
import net.pollyspeople.storagelabels.data.api.LocationApi
import net.pollyspeople.storagelabels.data.dto.AccessLevel
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
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class LocationRepository @Inject constructor(private val api: LocationApi) {

    suspend fun list(): ApiResult<List<StorageLocation>> = apiCall { api.getLocations() }

    suspend fun get(locationId: Long): ApiResult<StorageLocation> =
        apiCall { api.getLocation(locationId) }

    suspend fun create(name: String): ApiResult<StorageLocation> =
        apiCall { api.createLocation(LocationRequest(name)) }

    suspend fun rename(locationId: Long, name: String): ApiResult<StorageLocation> =
        apiCall { api.updateLocation(locationId, LocationRequest(name)) }

    suspend fun delete(locationId: Long, force: Boolean): ApiResult<Unit> =
        apiCall { api.deleteLocation(locationId, if (force) true else null) }.map { }

    suspend fun users(locationId: Long): ApiResult<List<LocationUser>> =
        apiCall { api.getLocationUsers(locationId) }

    suspend fun addUser(
        locationId: Long,
        email: String,
        accessLevel: AccessLevel,
    ): ApiResult<LocationUser> =
        apiCall { api.addUserToLocation(locationId, AddUserLocationRequest(email, accessLevel)) }

    suspend fun updateUserAccess(
        locationId: Long,
        userId: String,
        accessLevel: AccessLevel,
    ): ApiResult<LocationUser> =
        apiCall { api.updateUserAccess(locationId, userId, UpdateUserLocationRequest(accessLevel)) }

    suspend fun removeUser(locationId: Long, userId: String): ApiResult<Unit> =
        apiCall { api.removeUserFromLocation(locationId, userId) }.map { }
}

@Singleton
class BoxRepository @Inject constructor(private val api: BoxApi) {

    suspend fun listByLocation(locationId: Long): ApiResult<List<Box>> =
        apiCall { api.getBoxesByLocation(locationId) }

    suspend fun get(boxId: String): ApiResult<Box> = apiCall { api.getBox(boxId) }

    suspend fun create(request: BoxRequest): ApiResult<Box> = apiCall { api.createBox(request) }

    suspend fun update(boxId: String, request: BoxRequest): ApiResult<Box> =
        apiCall { api.updateBox(boxId, request) }

    suspend fun move(boxId: String, destinationLocationId: Long): ApiResult<Box> =
        apiCall { api.moveBox(boxId, MoveBoxRequest(destinationLocationId)) }

    suspend fun delete(boxId: String, force: Boolean): ApiResult<Unit> =
        apiCall { api.deleteBox(boxId, if (force) true else null) }.map { }
}

@Singleton
class ItemRepository @Inject constructor(private val api: ItemApi) {

    suspend fun listByBox(boxId: String): ApiResult<List<Item>> =
        apiCall { api.getItemsByBox(boxId) }

    suspend fun get(itemId: String): ApiResult<Item> = apiCall { api.getItem(itemId) }

    suspend fun create(request: ItemRequest): ApiResult<Item> = apiCall { api.createItem(request) }

    suspend fun update(itemId: String, request: ItemRequest): ApiResult<Item> =
        apiCall { api.updateItem(itemId, request) }

    suspend fun delete(itemId: String): ApiResult<Unit> = apiCall { api.deleteItem(itemId) }.map { }
}

/** Keeps repositories free of Response<Unit> plumbing. */
internal fun <T, R> ApiResult<T>.map(transform: (T) -> R): ApiResult<R> = when (this) {
    is ApiResult.Success -> ApiResult.Success(transform(value))
    is ApiResult.Failure -> this
}
