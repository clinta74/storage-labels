using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.EncryptionKey;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal static partial class EncryptionKeyEndpoints
{
    private static async Task<Results<Ok<EncryptionKeyResponse>, ProblemHttpResult>> CreateEncryptionKey(CreateEncryptionKeyRequest request, HttpContext context, [FromServices] IImageEncryptionService encryptionService, ILogger logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        try
        {
            var key = await encryptionService.CreateKeyAsync(request.Description, userId, cancellationToken);
            logger.EncryptionKeyCreated(userId, key.Kid, key.Version);

            context.Response.Headers.Location = $"/admin/encryption-keys/{key.Kid}";
            return TypedResults.Ok(new EncryptionKeyResponse(key));
        }
        catch (InvalidOperationException ex)
        {
            logger.EncryptionKeyCreationWarning(ex, request.Description ?? "unnamed");
            return TypedResults.Problem(ex.Message, statusCode: 500);
        }
    }
}
