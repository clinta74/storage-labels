package net.pollyspeople.storagelabels.core.result

import kotlinx.coroutines.test.runTest
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.ResponseBody.Companion.toResponseBody
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import retrofit2.HttpException
import retrofit2.Response
import java.io.IOException

class ApiResultTest {

    @Test
    fun `maps status codes onto errors the ui can act on`() = runTest {
        assertEquals(ApiError.Unauthorized, failureOf(401))
        assertEquals(ApiError.Forbidden, failureOf(403))
        assertEquals(ApiError.NotFound, failureOf(404))
    }

    @Test
    fun `carries the retry-after hint from a rate limited response`() = runTest {
        val result = apiCall<Unit> {
            throw HttpException(
                Response.error<Unit>(
                    "".toResponseBody("application/json".toMediaType()),
                    okhttp3.Response.Builder()
                        .code(429)
                        .message("Too Many Requests")
                        .protocol(okhttp3.Protocol.HTTP_1_1)
                        .header("Retry-After", "30")
                        .request(okhttp3.Request.Builder().url("http://server.invalid/search").build())
                        .build(),
                ),
            )
        }

        assertEquals(ApiError.RateLimited(30L), (result as ApiResult.Failure).error)
    }

    @Test
    fun `surfaces the problem detail from a validation failure`() = runTest {
        val body = """{"title":"One or more validation errors occurred.","detail":"Code is required."}"""

        val result = apiCall<Unit> { throw httpException(400, body) }

        assertEquals(ApiError.Validation("Code is required."), (result as ApiResult.Failure).error)
    }

    @Test
    fun `falls back to the message field some handlers return`() = runTest {
        val result = apiCall<Unit> { throw httpException(400, """{"message":"Password too short."}""") }

        assertEquals(ApiError.Validation("Password too short."), (result as ApiResult.Failure).error)
    }

    @Test
    fun `treats connection problems as network errors`() = runTest {
        val result = apiCall<Unit> { throw IOException("unreachable") }

        assertTrue((result as ApiResult.Failure).error is ApiError.Network)
    }

    @Test
    fun `passes the value through on success`() = runTest {
        assertEquals(ApiResult.Success(7), apiCall { 7 })
    }

    private suspend fun failureOf(code: Int): ApiError =
        (apiCall<Unit> { throw httpException(code, "") } as ApiResult.Failure).error

    private fun httpException(code: Int, body: String) = HttpException(
        Response.error<Unit>(code, body.toResponseBody("application/json".toMediaType())),
    )
}
