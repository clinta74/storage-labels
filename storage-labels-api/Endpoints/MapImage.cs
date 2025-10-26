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

        group.MapGet("/{hashedUserId}/{imageId:guid}", GetImageFileHandlerEndpoint)
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
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
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
        Guid imageId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new GetImageFile(imageId, hashedUserId, userId), cancellationToken);
        
        return result.Status switch
        {
            Ardalis.Result.ResultStatus.Ok => Results.File(result.Value.StoragePath, result.Value.ContentType, result.Value.FileName),
            _ => result.ToMinimalApiResult()
        };
    }
}
