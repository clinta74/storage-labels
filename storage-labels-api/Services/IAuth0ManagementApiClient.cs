using Auth0.ManagementApi;

namespace StorageLabelsApi.Services;
public interface IAuth0ManagementApiClient
{
    ManagementApiClient? Client { get; }
}
