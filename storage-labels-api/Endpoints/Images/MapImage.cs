using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Images;

internal static partial class ImageEndpoints
{
    internal static IEndpointRouteBuilder MapImage(this IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("images")
            .WithTags("Images")
            .AddEndpointFilter<UserExistsEndpointFilter>();

        group.MapPost("/", UploadImage)
            .DisableAntiforgery()
            .WithName("Upload Image");

        group.MapGet("/", GetUserImages)
            .WithName("Get User Images");

        group.MapDelete("/{imageId:guid}", DeleteImage)
            .WithName("Delete Image");

        group.MapDelete("/{imageId:guid}/force", ForceDeleteImage)
            .WithName("Force Delete Image");

        group.MapGet("/{imageId:guid}", GetImageFile)
            .AddEndpointFilter<ImageAccessFilter>()
            .WithName("Get Image File");

        return routeBuilder;
    }
}
