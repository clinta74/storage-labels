using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal static partial class EncryptionKeyEndpoints
{
    private static async Task<Results<Ok<ActivateEncryptionKeyResult>, NotFound<string>>> ActivateEncryptionKey([FromRoute] int kid, HttpContext context, [FromServices] IImageEncryptionService encryptionService, [FromServices] IKeyRotationService rotationService, [FromServices] StorageLabelsDbContext dbContext, ILogger logger, [FromQuery] bool autoRotate = true, CancellationToken cancellationToken = default)
    {
        var userId = context.GetUserId();

        var previousActiveKey = await encryptionService.GetActiveKeyAsync(cancellationToken);

        var success = await encryptionService.ActivateKeyAsync(kid, cancellationToken);
        if (!success)
        {
            logger.ActivateNonExistentKey(userId, kid);
            return TypedResults.NotFound("Encryption key not found");
        }

        logger.EncryptionKeyActivated(userId, kid);

        Guid? rotationId = null;

        if (autoRotate && previousActiveKey is not null && previousActiveKey.Kid != kid)
        {
            var hasImagesToRotate = await dbContext.Images
                .AnyAsync(img => img.IsEncrypted && img.EncryptionKeyId == previousActiveKey.Kid, cancellationToken);

            if (hasImagesToRotate)
            {
                try
                {
                    var rotation = await rotationService.StartRotationAsync(
                        new RotationOptions(
                            FromKeyId: previousActiveKey.Kid,
                            ToKeyId: kid,
                            BatchSize: 100,
                            InitiatedBy: userId,
                            IsAutomatic: true
                        ),
                        cancellationToken);

                    rotationId = rotation.Id;
                    logger.AutoRotationStarted(rotation.Id, previousActiveKey.Kid, kid);
                }
                catch (Exception ex)
                {
                    logger.AutoRotationFailed(ex);
                }
            }
        }

        return TypedResults.Ok(new ActivateEncryptionKeyResult(true, rotationId));
    }
}

public record ActivateEncryptionKeyResult(bool Success, Guid? RotationId = null);
