using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Labels;

namespace StorageLabelsApi.Endpoints.Labels;

internal partial class LabelEndpoints
{
    private const string Base36Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private static async Task<Results<Ok<LabelPageResponse>, NotFound<string>>> GetNextPage(
        HttpContext context,
        [FromRoute] Guid jobId,
        [FromServices] StorageLabelsDbContext dbContext,
        [FromServices] ILogger<LabelEndpoints> logger,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var job = await dbContext.LabelPrintJobs
            .Where(j => j.Id == jobId && j.CreatedBy == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            return TypedResults.NotFound($"Label job {jobId} was not found.");

        var labelsPerPage = GetLabelsPerPage(job.LabelFormat);
        var codes = GenerateCodes(job, labelsPerPage);

        job.LastGeneratedIndex += labelsPerPage;
        job.TotalLabelsGenerated += labelsPerPage;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LabelPageGenerated(userId, job.Id, labelsPerPage);

        return TypedResults.Ok(new LabelPageResponse(
            job.Id,
            job.LabelFormat,
            job.CodeColorPattern,
            codes));
    }

    private static int GetLabelsPerPage(LabelFormat format) => format switch
    {
        LabelFormat.Avery94107 => 12,
        _ => 12
    };

    private static IEnumerable<LabelCodeItem> GenerateCodes(LabelPrintJob job, int count)
    {
        var codes = new List<LabelCodeItem>(count);
        for (var i = 0; i < count; i++)
        {
            var index = job.LastGeneratedIndex + i;
            var suffix = job.IncrementAlgorithm switch
            {
                LabelIncrementAlgorithm.NumericOnly =>
                    index.ToString().PadLeft(job.AlgorithmSuffixLength, '0'),
                LabelIncrementAlgorithm.Base36Suffix =>
                    ToBase36(index, job.AlgorithmSuffixLength),
                _ => index.ToString()
            };

            var code = string.IsNullOrEmpty(job.AlgorithmPrefix)
                ? suffix
                : $"{job.AlgorithmPrefix}{suffix}";

            codes.Add(new LabelCodeItem(code, job.TotalLabelsGenerated + i + 1));
        }
        return codes;
    }

    private static string ToBase36(long value, int length)
    {
        if (value < 0) value = 0;
        var result = new char[length];
        for (var i = length - 1; i >= 0; i--)
        {
            result[i] = Base36Alphabet[(int)(value % 36)];
            value /= 36;
        }
        return new string(result);
    }
}
