using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Models.DTO.EncryptionKey;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal static partial class EncryptionKeyEndpoints
{
    private static async Task<Results<Ok<RotationProgressResponse>, NotFound<string>>> GetRotationProgress([FromRoute] Guid rotationId, [FromServices] IKeyRotationService rotationService, CancellationToken cancellationToken)
    {
        var progress = await rotationService.GetRotationProgressAsync(rotationId, cancellationToken);

        if (progress is null)
            return TypedResults.NotFound("Rotation not found");

        return TypedResults.Ok(new RotationProgressResponse(progress));
    }
}
