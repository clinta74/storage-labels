using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO.Labels;

public record UpdateLabelPrintJobRequest(
    string Name,
    LabelFormat LabelFormat,
    LabelIncrementAlgorithm IncrementAlgorithm,
    string? AlgorithmPrefix,
    int AlgorithmSuffixLength,
    string CodeColorPattern);
