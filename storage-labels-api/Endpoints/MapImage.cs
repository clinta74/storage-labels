using Ardalis.Result.AspNetCore;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Images;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Image;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    public static void MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("images")
            .WithTags("Images")
            .AddEndpointFilter<UserExistsEndpointFilter>();
        

        group.MapPost("/", UploadImageHandler)
            .DisableAntiforgery()
            .Produces<Microsoft.AspNetCore.Http.IResult>(StatusCodes.Status201Created)
            .Produces<Microsoft.AspNetCore.Http.IResult>(StatusCodes.Status400BadRequest)
            .WithName("Upload Image");

        group.MapGet("/", GetUserImagesHandler)
            .Produces<List<ImageMetadataResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Get User Images");

        group.MapDelete("/{imageId:guid}", DeleteImageHandler)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Delete Image");
        
        group.MapDelete("/{imageId:guid}/force", ForceDeleteImageHandler)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Force Delete Image");

        group.MapGet("/{imageId:guid}", GetImageFileHandlerEndpoint)
            .AddEndpointFilter<ImageAccessFilter>()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Get Image File");
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
        [FromRoute] Guid imageId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new DeleteImage(imageId, userId, false), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> ForceDeleteImageHandler(
        [FromRoute] Guid imageId,
        IMediator mediator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new DeleteImage(imageId, userId, true), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> GetImageFileHandlerEndpoint(
        [FromRoute] Guid imageId,
        IMediator mediator,
        HttpContext context,
        IImageEncryptionService encryptionService,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(new GetImageFile(imageId, userId), cancellationToken);
        
        if (result.IsSuccess)
        {
            var metadata = result.Value;
            
            // If image is encrypted, decrypt it before serving
            if (metadata.IsEncrypted)
            {
                try
                {
                    var decryptedStream = await encryptionService.DecryptImageAsync(metadata, cancellationToken);
                    return Results.Stream(decryptedStream, metadata.ContentType, metadata.FileName);
                }
                catch (Exception ex)
                {
                    // Log error and fall back to error response
                    context.RequestServices.GetRequiredService<ILogger<IImageEncryptionService>>()
                        .ImageDecryptionFailed(ex, imageId, userId);
                    return Results.Problem("Failed to decrypt image", statusCode: 500);
                }
            }
            
            // Serve unencrypted image directly from file
            return Results.File(metadata.StoragePath, metadata.ContentType, metadata.FileName);
        }
        
        return result.ToMinimalApiResult();
    }
}
