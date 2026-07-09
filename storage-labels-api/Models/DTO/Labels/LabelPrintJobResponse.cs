using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO.Labels;

public record LabelPrintJobResponse(
    Guid Id,
    string Name,
    LabelFormat LabelFormat,
    LabelIncrementAlgorithm IncrementAlgorithm,
    string? AlgorithmPrefix,
    int AlgorithmSuffixLength,
    long LastGeneratedIndex,
    int TotalLabelsGenerated,
    string CodeColorPattern,
    DateTimeOffset CreatedAt)
{
    public LabelPrintJobResponse(LabelPrintJob job) : this(
        job.Id,
        job.Name,
        job.LabelFormat,
        job.IncrementAlgorithm,
        job.AlgorithmPrefix,
        job.AlgorithmSuffixLength,
        job.LastGeneratedIndex,
        job.TotalLabelsGenerated,
        job.CodeColorPattern,
        job.CreatedAt)
    { }
}
