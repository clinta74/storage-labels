package net.pollyspeople.storagelabels.core.ui

import net.pollyspeople.storagelabels.core.result.ApiError

/**
 * One place that turns an [ApiError] into something worth reading. Errors say what happened
 * and what to do about it — no apologies, no stack traces in the UI.
 */
fun ApiError.userMessage(): String = when (this) {
    is ApiError.Network ->
        "Couldn't reach the server. Check the address and that you're on the right network."

    ApiError.Unauthorized ->
        "That username or password wasn't accepted."

    ApiError.Forbidden ->
        "Your account doesn't have access to this."

    ApiError.NotFound ->
        "Not found."

    is ApiError.RateLimited -> retryAfterSeconds
        ?.let { "Too many attempts. Try again in $it seconds." }
        ?: "Too many attempts. Try again shortly."

    is ApiError.Validation -> message

    is ApiError.Server ->
        message ?: "The server returned an error (code $code)."

    is ApiError.Unknown ->
        "Something went wrong. Try again."
}
