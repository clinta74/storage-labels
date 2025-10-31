namespace StorageLabelsApi.Models.DTO;

public record UserPreferencesDto
{
    public string Theme { get; init; } = "light";
    public bool ShowImages { get; init; } = true;
    public string CodeColorPattern { get; init; } = "";
}
