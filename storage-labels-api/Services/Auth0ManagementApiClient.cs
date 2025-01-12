using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;

namespace StorageLabelsApi.Services;

public class Auth0ManagementApiClient : IAuth0ManagementApiClient, IDisposable
{
    private bool disposedValue;

    public ManagementApiClient? Client { get; private set; }
    public Auth0ManagementApiClient(string clientId, string clientSecret, string domain)
    {
        var client = new AuthenticationApiClient(domain);
        var response = client.GetTokenAsync(new ClientCredentialsTokenRequest
        {
            Audience = $"https://{domain}/api/v2/",
            ClientId = clientId,
            ClientSecret = clientSecret,
            SigningAlgorithm = JwtSignatureAlgorithm.RS256,
        }).Result;

        Client = new ManagementApiClient(response.AccessToken, domain); 
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Client?.Dispose();
            }

            Client = null;
            disposedValue = true;
        }
    }

    void IDisposable.Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}