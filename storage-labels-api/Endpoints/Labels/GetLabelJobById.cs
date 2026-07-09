using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.Labels;

namespace StorageLabelsApi.Endpoints.Labels;

internal partial class LabelEndpoints
{
    private static async Task<Results<Ok<LabelPrintJobResponse>, NotFound<string>>> GetLabelJobById(
        HttpContext context,
        [FromRoute] Guid jobId,
        [FromServices] StorageLabelsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var job = await dbContext.LabelPrintJobs
            .AsNoTracking()
            .Where(j => j.Id == jobId && j.CreatedBy == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            return TypedResults.NotFound($"Label job {jobId} was not found.");

        return TypedResults.Ok(new LabelPrintJobResponse(job));
    }
}
