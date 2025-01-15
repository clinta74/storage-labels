namespace StorageLabelsApi.Models.Settings;

public record Auth0Settings
{
    public string Domain { get; init; } = default!;
    public string Audience { get; init;} = default!;
    public string ClientId { get; init;} = default!;
    public string ApiClientId { get; init;} = default!;
    public string ClientSecret { get; init;} = default!;
    public string DomainUrl => $"https://{Domain}/";

    public void Validate()
    {
        if (Domain is null) throw new ArgumentNullException(nameof(Domain));
        if (Audience is null) throw new ArgumentNullException(nameof(Audience));
        if (ClientId is null) throw new ArgumentNullException(nameof(ClientId));
        if (ClientSecret is null) throw new ArgumentNullException(nameof(ClientSecret));
        if (ApiClientId is null) throw new ArgumentNullException(nameof(ApiClientId));
    }
}