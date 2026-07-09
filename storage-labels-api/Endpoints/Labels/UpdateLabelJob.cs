using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Labels;

namespace StorageLabelsApi.Endpoints.Labels;

internal partial class LabelEndpoints
{
    private static async Task<Results<Ok<LabelPrintJobResponse>, NotFound<string>, ValidationProblem>> UpdateLabelJob(
        HttpContext context,
        [FromRoute] Guid jobId,
        [FromBody] UpdateLabelPrintJobRequest request,
        [FromServices] StorageLabelsDbContext dbContext,
        [FromServices] ILogger<LabelEndpoints> logger,
        CancellationToken cancellationToken)
    {
        var validation = await new UpdateLabelJobValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var userId = context.GetUserId();

        var exists = await dbContext.LabelPrintJobs
            .AsNoTracking()
            .Where(j => j.Id == jobId && j.CreatedBy == userId)
            .AnyAsync(cancellationToken);

        if (!exists)
            return TypedResults.NotFound($"Label job {jobId} was not found.");

        await dbContext.LabelPrintJobs
            .Where(j => j.Id == jobId && j.CreatedBy == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.Name, request.Name)
                .SetProperty(j => j.LabelFormat, request.LabelFormat)
                .SetProperty(j => j.IncrementAlgorithm, request.IncrementAlgorithm)
                .SetProperty(j => j.AlgorithmPrefix, request.AlgorithmPrefix)
                .SetProperty(j => j.AlgorithmSuffixLength, request.AlgorithmSuffixLength)
                .SetProperty(j => j.CodeColorPattern, request.CodeColorPattern),
                cancellationToken);

        var updatedJob = await dbContext.LabelPrintJobs
            .AsNoTracking()
            .FirstAsync(j => j.Id == jobId, cancellationToken);

        logger.LabelJobUpdated(userId, jobId);
        return TypedResults.Ok(new LabelPrintJobResponse(updatedJob));
    }

    private sealed class UpdateLabelJobValidator : AbstractValidator<UpdateLabelPrintJobRequest>
    {
        public UpdateLabelJobValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AlgorithmSuffixLength).InclusiveBetween(1, 10);
            RuleFor(x => x.AlgorithmPrefix)
                .MaximumLength(50)
                .When(x => x.AlgorithmPrefix is not null);
        }
    }
}
