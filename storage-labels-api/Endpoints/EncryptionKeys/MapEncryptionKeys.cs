using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal static partial class EncryptionKeyEndpoints
{
    internal static IEndpointRouteBuilder MapEncryptionKeys(this IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("admin/encryption-keys")
            .RequireAuthorization()
            .WithTags("Admin - Encryption Keys");

        group.MapPost("/", CreateEncryptionKey)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("Create Encryption Key")
            .WithSummary("Create a new encryption key");

        group.MapGet("/", GetEncryptionKeys)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("GetEncryptionKeys")
            .WithSummary("Get all encryption keys");

        group.MapGet("/{kid:int}/stats", GetEncryptionKeyStats)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("GetEncryptionKeyStats")
            .WithSummary("Get statistics for an encryption key");

        group.MapPut("/{kid:int}/activate", ActivateEncryptionKey)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("ActivateEncryptionKey")
            .WithSummary("Activate an encryption key (optionally auto-rotate images)");

        group.MapPut("/{kid:int}/retire", RetireEncryptionKey)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("RetireEncryptionKey")
            .WithSummary("Retire an encryption key");

        group.MapPost("/rotate", StartKeyRotation)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("StartKeyRotation")
            .WithSummary("Start a manual key rotation");

        group.MapGet("/rotations", GetRotations)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("GetRotations")
            .WithSummary("Get all key rotation operations");

        group.MapGet("/rotations/{rotationId:guid}", GetRotationProgress)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("GetRotationProgress")
            .WithSummary("Get progress of a rotation operation");

        group.MapDelete("/rotations/{rotationId:guid}", CancelRotation)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("CancelRotation")
            .WithSummary("Cancel an in-progress rotation");

        group.MapGet("/rotations/{rotationId:guid}/stream", StreamRotationProgress)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("StreamRotationProgress")
            .WithSummary("Stream real-time progress updates for a rotation operation");

        return routeBuilder;
    }
}
