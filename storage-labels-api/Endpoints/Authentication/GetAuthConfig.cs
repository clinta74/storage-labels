using Ardalis.Result.AspNetCore;
using Microsoft.Extensions.Options;
using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Models.Settings;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints.Authentication;

internal partial class AuthenticationEndpoints
{
    private static IResult GetAuthConfig(
        IOptions<AuthenticationSettings> authSettings)
    {
        var s = authSettings.Value;
        var config = new AuthConfigResponse(
            s.Mode,
            s.Local.AllowRegistration,
            s.Local.RequireEmailConfirmation
        );
        return Result.Success(config).ToMinimalApiResult();
    }
}
