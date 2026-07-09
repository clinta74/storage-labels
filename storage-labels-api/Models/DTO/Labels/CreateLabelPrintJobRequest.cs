using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO.Labels;

public record CreateLabelPrintJobRequest(
    string Name,
    LabelFormat LabelFormat,
    LabelIncrementAlgorithm IncrementAlgorithm,
    string? AlgorithmPrefix,
    int AlgorithmSuffixLength,
    long StartIndex,
    string CodeColorPattern);
