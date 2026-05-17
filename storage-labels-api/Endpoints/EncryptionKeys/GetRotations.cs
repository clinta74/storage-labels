using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.EncryptionKey;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal partial class EncryptionKeyEndpoints
{
    private static async Task<Ok<List<EncryptionKeyRotationResponse>>> GetRotations([FromServices] IKeyRotationService rotationService, [FromQuery] RotationStatus? status, CancellationToken cancellationToken)
    {
        var rotations = await rotationService.GetRotationsAsync(status, cancellationToken);
        return TypedResults.Ok(rotations.Select(r => new EncryptionKeyRotationResponse(r)).ToList());
    }
}
