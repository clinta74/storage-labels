using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.EncryptionKey;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal static partial class EncryptionKeyEndpoints
{
    private static async Task<Results<Ok<EncryptionKeyStatsResponse>, NotFound<string>>> GetEncryptionKeyStats([FromRoute] int kid, [FromServices] IImageEncryptionService encryptionService, [FromServices] ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var stats = await encryptionService.GetKeyStatsAsync(kid, cancellationToken);
            return TypedResults.Ok(new EncryptionKeyStatsResponse(stats));
        }
        catch (InvalidOperationException)
        {
            logger.EncryptionKeyStatsNotFound(kid);
            return TypedResults.NotFound("Encryption key not found");
        }
    }
}
