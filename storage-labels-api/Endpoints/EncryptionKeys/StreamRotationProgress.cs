using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal partial class EncryptionKeyEndpoints
{
    private static async Task StreamRotationProgress([FromRoute] Guid rotationId, HttpContext context, [FromServices] IRotationProgressNotifier progressNotifier, CancellationToken cancellationToken)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        await progressNotifier.StreamProgressAsync(rotationId, context.Response.Body, cancellationToken);
    }
}
