package net.pollyspeople.storagelabels

import android.app.Application
import android.content.Context
import coil3.ImageLoader
import coil3.PlatformContext
import coil3.SingletonImageLoader
import dagger.hilt.android.HiltAndroidApp
import javax.inject.Inject

@HiltAndroidApp
class StorageLabelsApp : Application(), SingletonImageLoader.Factory {

    /**
     * Coil's singleton loader is built by Hilt so every image request carries the session's
     * bearer token and follows the same refresh rules as the rest of the app.
     */
    @Inject
    lateinit var imageLoader: ImageLoader

    override fun newImageLoader(context: PlatformContext): ImageLoader = imageLoader
}

/** Convenience for composables that need the application context. */
val Context.storageLabelsApp: StorageLabelsApp
    get() = applicationContext as StorageLabelsApp
