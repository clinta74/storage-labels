using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.CommonLocation;

namespace StorageLabelsApi.Endpoints.CommonLocations;

internal partial class CommonLocationEndpoints
{
    private static async Task<Results<Created<CommonLocationResponse>, ValidationProblem>> CreateCommonLocation(CommonLocationRequest request, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Name))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(CommonLocation)] = ["Name is required."] });
        }

        var commonLocation = dbContext.CommonLocations.Add(new(
            CommonLocationId: 0,
            Name: request.Name
        ));

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created((string?)null, new CommonLocationResponse(commonLocation.Entity));
    }
}
