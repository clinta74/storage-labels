using Ardalis.Result.AspNetCore;
using Mediator;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Images;
using StorageLabelsApi.Models.DTO.Image;

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
        
        group.MapDelete("/{imageId:guid}/force", ForceDeleteImageHandler)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{imageId:guid}", GetImageFileHandlerEndpoint)
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
        return result
            .Map(images => images.ConvertAll(img => new ImageMetadataResponse(img, string.Empty)))
            .ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> DeleteImageHandler(
        Guid imageId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new DeleteImage(imageId, userId, false), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> ForceDeleteImageHandler(
        Guid imageId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new DeleteImage(imageId, userId, true), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetImageFileHandlerEndpoint(
        Guid imageId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new GetImageFile(imageId, userId), cancellationToken);
        
        if (result.IsSuccess)
        {
            return Results.File(result.Value.StoragePath, result.Value.ContentType, result.Value.FileName);
        }
        
        return result.ToMinimalApiResult();
    }
}
