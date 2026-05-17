using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal partial class EncryptionKeyEndpoints
{
    private static async Task<Results<Ok<bool>, NotFound<string>>> CancelRotation([FromRoute] Guid rotationId, HttpContext context, [FromServices] IKeyRotationService rotationService, [FromServices] ILogger<EncryptionKeyEndpoints> logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var success = await rotationService.CancelRotationAsync(rotationId, cancellationToken);
        if (!success)
        {
            logger.CancelNonExistentRotation(userId, rotationId);
            return TypedResults.NotFound("Rotation not found or cannot be cancelled");
        }

        logger.RotationCancelled(userId, rotationId);
        return TypedResults.Ok(true);
    }
}
