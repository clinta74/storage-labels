using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal static partial class EncryptionKeyEndpoints
{
    private static async Task<Results<Ok<bool>, NotFound<string>>> RetireEncryptionKey([FromRoute] int kid, HttpContext context, [FromServices] IImageEncryptionService encryptionService, [FromServices] ILogger logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var success = await encryptionService.RetireKeyAsync(kid, cancellationToken);
        if (!success)
        {
            logger.RetireNonExistentKey(userId, kid);
            return TypedResults.NotFound("Encryption key not found");
        }

        logger.EncryptionKeyRetired(userId, kid);
        return TypedResults.Ok(true);
    }
}
