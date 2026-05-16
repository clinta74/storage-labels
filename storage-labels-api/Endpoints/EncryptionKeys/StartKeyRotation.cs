using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.EncryptionKey;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal static partial class EncryptionKeyEndpoints
{
    private static async Task<Results<Accepted<Guid>, ProblemHttpResult>> StartKeyRotation(StartRotationRequest request, HttpContext context, [FromServices] IKeyRotationService rotationService, [FromServices] ILogger logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        try
        {
            var rotation = await rotationService.StartRotationAsync(
                new RotationOptions(
                    FromKeyId: request.FromKeyId,
                    ToKeyId: request.ToKeyId,
                    BatchSize: request.BatchSize,
                    InitiatedBy: userId,
                    IsAutomatic: false
                ),
                cancellationToken);

            logger.ManualRotationStarted(userId, rotation.Id, request.FromKeyId, request.ToKeyId);

            return TypedResults.Accepted($"/admin/encryption-keys/rotations/{rotation.Id}", rotation.Id);
        }
        catch (InvalidOperationException ex)
        {
            logger.KeyRotationStartFailed(ex);
            return TypedResults.Problem(ex.Message, statusCode: 500);
        }
    }
}
