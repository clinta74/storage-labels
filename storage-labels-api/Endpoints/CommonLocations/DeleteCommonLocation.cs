using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Endpoints.CommonLocations;

internal partial class CommonLocationEndpoints
{
    private static async Task<Results<Ok, NotFound<string>>> DeleteCommonLocation([FromRoute] int commonLocationId, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var exists = await dbContext.CommonLocations
            .AsNoTracking()
            .AnyAsync(cl => cl.CommonLocationId == commonLocationId, cancellationToken);

        if (!exists)
            return TypedResults.NotFound($"Common location id {commonLocationId} not found.");

        await dbContext.CommonLocations
            .Where(cl => cl.CommonLocationId == commonLocationId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
