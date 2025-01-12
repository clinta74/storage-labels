namespace StorageLabelsApi.Models.Settings;

public record Auth0(
    string Domain,
    string Audience,
    string ClientId,
    string ClientSecret
)
{
    public string DomainUrl => $"https://{Domain}/";
}