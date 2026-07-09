using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Labels;

namespace StorageLabelsApi.Endpoints.Labels;

internal partial class LabelEndpoints
{
    private static async Task<Results<Created<LabelPrintJobResponse>, ValidationProblem>> CreateLabelJob(
        HttpContext context,
        [FromBody] CreateLabelPrintJobRequest request,
        [FromServices] StorageLabelsDbContext dbContext,
        [FromServices] TimeProvider timeProvider,
        [FromServices] ILogger<LabelEndpoints> logger,
        CancellationToken cancellationToken)
    {
        var validation = await new CreateLabelJobValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var userId = context.GetUserId();
        var job = new LabelPrintJob
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            LabelFormat = request.LabelFormat,
            IncrementAlgorithm = request.IncrementAlgorithm,
            AlgorithmPrefix = request.AlgorithmPrefix,
            AlgorithmSuffixLength = request.AlgorithmSuffixLength,
            LastGeneratedIndex = request.StartIndex,
            TotalLabelsGenerated = 0,
            CodeColorPattern = request.CodeColorPattern,
            CreatedAt = timeProvider.GetUtcNow(),
            CreatedBy = userId
        };

        dbContext.LabelPrintJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LabelJobCreated(userId, job.Id);
        return TypedResults.Created($"/api/labels/{job.Id}", new LabelPrintJobResponse(job));
    }

    private sealed class CreateLabelJobValidator : AbstractValidator<CreateLabelPrintJobRequest>
    {
        public CreateLabelJobValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AlgorithmSuffixLength).InclusiveBetween(1, 10);
            RuleFor(x => x.StartIndex).GreaterThanOrEqualTo(0);
            RuleFor(x => x.AlgorithmPrefix)
                .MaximumLength(50)
                .When(x => x.AlgorithmPrefix is not null);
        }
    }
}
