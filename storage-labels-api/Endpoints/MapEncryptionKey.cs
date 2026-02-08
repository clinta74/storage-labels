using Ardalis.Result.AspNetCore;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Handlers.EncryptionKeys;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO.EncryptionKey;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    public static void MapEncryptionKeyEndpoints(this IEndpointRouteBuilder app)
    {
        
        var group = app.MapGroup("admin/encryption-keys")
            .RequireAuthorization()
            .WithTags("Admin - Encryption Keys");

        group.MapPost("/", CreateEncryptionKeyEndpoint)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("Create Encryption Key")
            .WithSummary("Create a new encryption key")
            .Produces<EncryptionKeyResponse>(201)
            .ProducesValidationProblem()
            .ProducesProblem(400);

        group.MapGet("/", GetEncryptionKeysEndpoint)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("GetEncryptionKeys")
            .WithSummary("Get all encryption keys")
            .Produces<List<EncryptionKeyResponse>>(200);

        group.MapGet("/{kid:int}/stats", GetEncryptionKeyStatsEndpoint)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("GetEncryptionKeyStats")
            .WithSummary("Get statistics for an encryption key")
            .Produces<Services.EncryptionKeyStats>(200)
            .ProducesProblem(404);

        group.MapPut("/{kid:int}/activate", ActivateEncryptionKeyEndpoint)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("ActivateEncryptionKey")
            .WithSummary("Activate an encryption key (optionally auto-rotate images)")
            .Produces<ActivateEncryptionKeyResult>(200)
            .ProducesProblem(404);

        group.MapPut("/{kid:int}/retire", RetireEncryptionKeyEndpoint)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("RetireEncryptionKey")
            .WithSummary("Retire an encryption key")
            .Produces(200)
            .ProducesProblem(404);

        // Rotation endpoints
        group.MapPost("/rotate", StartRotationEndpoint)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("StartKeyRotation")
            .WithSummary("Start a manual key rotation")
            .Produces<Guid>(202)
            .ProducesValidationProblem()
            .ProducesProblem(400);

        group.MapGet("/rotations", GetRotationsEndpoint)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("GetRotations")
            .WithSummary("Get all key rotation operations")
            .Produces<List<DataLayer.Models.EncryptionKeyRotation>>(200);

        group.MapGet("/rotations/{rotationId:guid}", GetRotationProgressEndpoint)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("GetRotationProgress")
            .WithSummary("Get progress of a rotation operation")
            .Produces<Services.RotationProgress>(200)
            .ProducesProblem(404);

        group.MapDelete("/rotations/{rotationId:guid}", CancelRotationEndpoint)
            .RequireAuthorization(Policies.Write_EncryptionKeys)
            .WithName("CancelRotation")
            .WithSummary("Cancel an in-progress rotation")
            .Produces(200)
            .ProducesProblem(404);

        group.MapGet("/rotations/{rotationId:guid}/stream", StreamRotationProgressEndpoint)
            .RequireAuthorization(Policies.Read_EncryptionKeys)
            .WithName("StreamRotationProgress")
            .WithSummary("Stream real-time progress updates for a rotation operation")
            .Produces(200)
            .ProducesProblem(404);
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> CreateEncryptionKeyEndpoint(
        CreateEncryptionKeyRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(
            new CreateEncryptionKey(request.Description, userId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Created($"/admin/encryption-keys/{result.Value.Kid}", result.Value);
        }

        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetEncryptionKeysEndpoint(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEncryptionKeys(), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetEncryptionKeyStatsEndpoint(
        [FromRoute] int kid,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEncryptionKeyStats(kid), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> ActivateEncryptionKeyEndpoint(
        [FromRoute] int kid,
        IMediator mediator,
        HttpContext context,
        bool autoRotate = true,
        CancellationToken cancellationToken = default)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(
            new ActivateEncryptionKey(kid, userId, autoRotate), 
            cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> RetireEncryptionKeyEndpoint(
        [FromRoute] int kid,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new RetireEncryptionKey(kid, userId), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> StartRotationEndpoint(
        StartRotationRequest request,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(
            new StartKeyRotation(request.FromKeyId, request.ToKeyId, request.BatchSize, userId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Accepted($"/admin/encryption-keys/rotations/{result.Value}", result.Value);
        }

        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetRotationsEndpoint(
        IMediator mediator,
        DataLayer.Models.RotationStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRotations(status), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetRotationProgressEndpoint(
        [FromRoute] Guid rotationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRotationProgress(rotationId), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> CancelRotationEndpoint(
        [FromRoute] Guid rotationId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new CancelRotation(rotationId, userId), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task StreamRotationProgressEndpoint(
        [FromRoute] Guid rotationId,
        HttpContext context,
        Services.IRotationProgressNotifier progressNotifier,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        await progressNotifier.StreamProgressAsync(rotationId, context.Response.Body, cancellationToken);
    }
}

