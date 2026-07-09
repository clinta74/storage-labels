using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.Labels;

namespace StorageLabelsApi.Endpoints.Labels;

internal partial class LabelEndpoints
{
    private static async Task<Ok<IEnumerable<LabelPrintJobResponse>>> GetLabelJobs(
        HttpContext context,
        [FromServices] StorageLabelsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var jobs = await dbContext.LabelPrintJobs
            .AsNoTracking()
            .Where(j => j.CreatedBy == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<LabelPrintJobResponse>>(jobs.Select(j => new LabelPrintJobResponse(j)));
    }
}
