package net.pollyspeople.storagelabels.core.result

import kotlinx.coroutines.CancellationException
import retrofit2.HttpException
import retrofit2.Response
import java.io.IOException

/**
 * Every network call funnels through here so error handling is decided once rather than
 * per screen — the web client does the same in its axios interceptor.
 */
sealed interface ApiResult<out T> {
    data class Success<T>(val value: T) : ApiResult<T>
    data class Failure(val error: ApiError) : ApiResult<Nothing>
}

sealed interface ApiError {
    /** No server configured, DNS failure, refused connection, TLS problem, timeout. */
    data class Network(val cause: Throwable) : ApiError

    /** Credentials rejected, or the session could not be refreshed. */
    data object Unauthorized : ApiError

    /** Authenticated but lacking the permission the endpoint requires. */
    data object Forbidden : ApiError

    data object NotFound : ApiError

    /** The API's rate limiter pushed back. */
    data class RateLimited(val retryAfterSeconds: Long?) : ApiError

    /** 400 with a validation problem or message body. */
    data class Validation(val message: String) : ApiError

    data class Server(val code: Int, val message: String?) : ApiError

    data class Unknown(val cause: Throwable) : ApiError
}

suspend fun <T> apiCall(block: suspend () -> T): ApiResult<T> =
    try {
        val value = block()
        // Retrofit throws only when the method declares a body type. A method returning
        // Response<T> -- which every delete and every no-content call here does -- hands the
        // failure back as an ordinary value, so an unchecked 4xx would read as success and
        // the app would report "Deleted" for something the server refused to delete.
        if (value is Response<*> && !value.isSuccessful) {
            ApiResult.Failure(value.toApiError())
        } else {
            ApiResult.Success(value)
        }
    } catch (cancellation: CancellationException) {
        throw cancellation
    } catch (io: IOException) {
        ApiResult.Failure(ApiError.Network(io))
    } catch (http: HttpException) {
        ApiResult.Failure(http.toApiError())
    } catch (other: Throwable) {
        ApiResult.Failure(ApiError.Unknown(other))
    }

private fun HttpException.toApiError(): ApiError = errorFor(
    code = code(),
    body = runCatching { response()?.errorBody()?.string() }.getOrNull(),
    retryAfter = response()?.headers()?.get("Retry-After")?.toLongOrNull(),
)

private fun Response<*>.toApiError(): ApiError = errorFor(
    code = code(),
    body = runCatching { errorBody()?.string() }.getOrNull(),
    retryAfter = headers()["Retry-After"]?.toLongOrNull(),
)

private fun errorFor(code: Int, body: String?, retryAfter: Long?): ApiError = when (code) {
    401 -> ApiError.Unauthorized
    403 -> ApiError.Forbidden
    404 -> ApiError.NotFound
    429 -> ApiError.RateLimited(retryAfter)
    400, 422 -> ApiError.Validation(extractMessage(body) ?: "The request was rejected.")
    else -> ApiError.Server(code, extractMessage(body))
}

/**
 * The API returns ProblemDetails ("title"/"detail") from TypedResults.Problem and a plain
 * "message" from some handlers. Pull whichever is present without a full JSON model.
 */
internal fun extractMessage(body: String?): String? {
    if (body.isNullOrBlank()) return null
    for (key in listOf("detail", "message", "title")) {
        val value = readJsonString(body, key)
        if (!value.isNullOrBlank()) return value
    }
    return null
}

private fun readJsonString(json: String, key: String): String? {
    val marker = "\"" + key + "\""
    val keyIndex = json.indexOf(marker)
    if (keyIndex < 0) return null
    val colon = json.indexOf(':', keyIndex + marker.length)
    if (colon < 0) return null
    val open = json.indexOf('"', colon + 1)
    if (open < 0) return null

    val builder = StringBuilder()
    var index = open + 1
    while (index < json.length) {
        val ch = json[index]
        when {
            ch == '\\' && index + 1 < json.length -> {
                val next = json[index + 1]
                builder.append(if (next == 'n') '\n' else next)
                index += 2
            }
            ch == '"' -> return builder.toString()
            else -> {
                builder.append(ch)
                index++
            }
        }
    }
    return null
}
