namespace StorageLabelsApi.Models.DTO.User;

public record UserPreferencesResponse
{
    public string Theme { get; init; } = "light";
    public bool ShowImages { get; init; } = true;
    public string CodeColorPattern { get; init; } = "";
}
