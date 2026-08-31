package net.pollyspeople.storagelabels.core.network

import android.content.Context
import coil3.ImageLoader
import coil3.map.Mapper
import coil3.network.okhttp.OkHttpNetworkFetcherFactory
import coil3.request.Options
import coil3.request.crossfade
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import okhttp3.OkHttpClient
import javax.inject.Named
import javax.inject.Singleton

/**
 * Turns the relative image paths the API hands out (/api/images/{id}) into absolute URLs on
 * whichever server the user configured. Returning null leaves a URL that is already absolute
 * untouched.
 */
class RelativeUrlMapper(
    private val serverUrlProvider: ServerUrlProvider,
) : Mapper<String, String> {

    override fun map(data: String, options: Options): String? {
        if (data.startsWith("http://") || data.startsWith("https://")) return null
        val base = serverUrlProvider.current()?.trimEnd('/') ?: return null
        return base + "/" + data.trimStart('/')
    }
}

@Module
@InstallIn(SingletonComponent::class)
object ImageLoaderModule {

    @Provides
    @Singleton
    fun provideImageLoader(
        @ApplicationContext context: Context,
        @Named("images") client: OkHttpClient,
        serverUrlProvider: ServerUrlProvider,
    ): ImageLoader = ImageLoader.Builder(context)
        .components {
            add(RelativeUrlMapper(serverUrlProvider))
            add(OkHttpNetworkFetcherFactory(callFactory = { client }))
        }
        .crossfade(true)
        .build()
}
