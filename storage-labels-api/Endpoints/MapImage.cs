using Ardalis.Result.AspNetCore;
using MediatR;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Images;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi.Endpoints;

public static class MapImage
{

    public static void MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("images")
            .WithTags("Images")
            .AddEndpointFilter<UserExistsEndpointFilter>();

        group.MapPost("/", UploadImageHandler)
            .DisableAntiforgery()
            .Produces<Microsoft.AspNetCore.Http.IResult>(StatusCodes.Status201Created)
            .Produces<Microsoft.AspNetCore.Http.IResult>(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetUserImagesHandler)
            .Produces<List<ImageMetadataResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/{imageId:guid}", DeleteImageHandler)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{hashedUserId}/{imageId}", GetImageFileHandlerEndpoint)
            .AddEndpointFilter<ImageAccessFilter>()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> UploadImageHandler(
        IFormFile file,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new UploadImage(file, userId), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetUserImagesHandler(
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new GetUserImages(userId), cancellationToken);
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}/api";
        return result
            .Map(images => images.ConvertAll(img => new ImageMetadataResponse(img, baseUrl)))
            .ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> DeleteImageHandler(
        Guid imageId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new DeleteImage(imageId, userId), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetImageFileHandlerEndpoint(
        string hashedUserId,
        string imageId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        // Decode the Base64URL encoded parameters
        if (!Base64UrlEncoder.TryDecodeString(hashedUserId, out var decodedHashedUserId))
        {
            return Results.BadRequest("Invalid user ID encoding");
        }
        
        if (!Base64UrlEncoder.TryDecodeGuid(imageId, out var decodedImageId))
        {
            return Results.BadRequest("Invalid image ID encoding");
        }

        var userId = context.GetUserId();
        var result = await mediator.Send(new GetImageFile(decodedImageId, decodedHashedUserId, userId), cancellationToken);
        
        if (result.IsSuccess)
        {
            return Results.File(result.Value.StoragePath, result.Value.ContentType, result.Value.FileName);
        }
        
        return result.ToMinimalApiResult();
    }
}
