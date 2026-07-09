using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO.Labels;

public record LabelPageResponse(
    Guid JobId,
    LabelFormat LabelFormat,
    string CodeColorPattern,
    IEnumerable<LabelCodeItem> Labels);
