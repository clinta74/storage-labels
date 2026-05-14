using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.Search;

namespace StorageLabelsApi.Endpoints.Search;

internal static partial class SearchEndpoints
{
    private static async Task<Results<Ok<SearchResultResponse>, NotFound>> SearchByQrCode(HttpContext context, [FromRoute] string code, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var box = await dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.Code == code)
            .Where(b => dbContext.UserLocations
                .Any(ul => ul.LocationId == b.LocationId &&
                           ul.UserId == userId &&
                           ul.AccessLevel != AccessLevels.None))
            .Select(b => new SearchResultResponse(
                "box",
                1.0f,
                b.BoxId.ToString(),
                b.Name,
                b.Code,
                null,
                null,
                null,
                b.LocationId.ToString(),
                b.Location.Name
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (box is not null)
            return TypedResults.Ok(box);

        return TypedResults.NotFound();
    }
}
