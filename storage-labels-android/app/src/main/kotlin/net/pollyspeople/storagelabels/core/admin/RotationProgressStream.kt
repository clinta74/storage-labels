package net.pollyspeople.storagelabels.core.admin

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flowOn
import kotlinx.serialization.json.Json
import net.pollyspeople.storagelabels.core.network.ServerUrlProvider
import net.pollyspeople.storagelabels.data.dto.RotationProgress
import okhttp3.OkHttpClient
import okhttp3.Request
import javax.inject.Inject
import javax.inject.Named
import javax.inject.Singleton

/**
 * Live progress for a key rotation, over Server-Sent Events.
 *
 * The API streams `data: {json}` lines and stops once the rotation leaves InProgress. There
 * is no SSE client here on purpose: OkHttp can read the body line by line, which is the
 * whole protocol.
 */
@Singleton
class RotationProgressStream @Inject constructor(
    @Named("images") private val client: OkHttpClient,
    private val serverUrlProvider: ServerUrlProvider,
    private val json: Json,
) {

    fun observe(rotationId: String): Flow<RotationProgress> = flow {
        val base = serverUrlProvider.current()?.trimEnd('/') ?: return@flow
        val request = Request.Builder()
            .url("$base/api/admin/encryption-keys/rotations/$rotationId/stream")
            .header("Accept", "text/event-stream")
            .build()

        client.newCall(request).execute().use { response ->
            if (!response.isSuccessful) return@flow
            val source = response.body.source()

            while (!source.exhausted()) {
                val line = source.readUtf8Line() ?: break
                if (!line.startsWith(DATA_PREFIX)) continue

                val payload = line.removePrefix(DATA_PREFIX).trim()
                if (payload.isEmpty()) continue

                val progress = runCatching {
                    json.decodeFromString<RotationProgress>(payload)
                }.getOrNull() ?: continue

                emit(progress)

                // The server closes after the terminal event; stop reading rather than
                // holding the connection open.
                if (progress.status.isFinished) break
            }
        }
    }.flowOn(Dispatchers.IO)

    private companion object {
        const val DATA_PREFIX = "data:"
    }
}
