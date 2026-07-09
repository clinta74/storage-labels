using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Endpoints.Labels;

internal partial class LabelEndpoints
{
    private static async Task<Results<Ok, NotFound<string>>> DeleteLabelJob(
        HttpContext context,
        [FromRoute] Guid jobId,
        [FromServices] StorageLabelsDbContext dbContext,
        [FromServices] ILogger<LabelEndpoints> logger,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var deleted = await dbContext.LabelPrintJobs
            .Where(j => j.Id == jobId && j.CreatedBy == userId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
            return TypedResults.NotFound($"Label job {jobId} was not found.");

        logger.LabelJobDeleted(userId, jobId);
        return TypedResults.Ok();
    }
}
