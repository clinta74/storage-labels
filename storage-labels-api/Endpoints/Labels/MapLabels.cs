using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Labels;

internal partial class LabelEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("labels")
            .WithTags("Labels")
            .AddEndpointFilter<UserExistsEndpointFilter>();

        group.MapPost("/", CreateLabelJob)
            .WithName("Create Label Job");

        group.MapGet("/", GetLabelJobs)
            .WithName("Get Label Jobs");

        group.MapGet("/{jobId:guid}", GetLabelJobById)
            .WithName("Get Label Job By ID");

        group.MapPost("/{jobId:guid}/next-page", GetNextPage)
            .WithName("Get Next Label Page");

        group.MapDelete("/{jobId:guid}", DeleteLabelJob)
            .WithName("Delete Label Job");

        group.MapPut("/{jobId:guid}", UpdateLabelJob)
            .WithName("Update Label Job V1");
    }
}
