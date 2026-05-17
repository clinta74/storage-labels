using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.CommonLocation;

namespace StorageLabelsApi.Endpoints.CommonLocations;

internal partial class CommonLocationEndpoints
{
    private static async IAsyncEnumerable<CommonLocationResponse> GetCommonLocations([FromServices] StorageLabelsDbContext dbContext, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var commonLocations = dbContext.CommonLocations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .AsAsyncEnumerable();

        await foreach (var commonLocation in commonLocations)
        {
            if (cancellationToken.IsCancellationRequested) break;
            yield return new CommonLocationResponse(commonLocation);
        }
    }
}
