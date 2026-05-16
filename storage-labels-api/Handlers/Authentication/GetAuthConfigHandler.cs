using Microsoft.Extensions.Options;
using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Models.Settings;

namespace StorageLabelsApi.Handlers.Authentication;

public record GetAuthConfig : IRequest<Result<AuthConfigResponse>>;

public class GetAuthConfigHandler(IOptions<AuthenticationSettings> authSettings) 
    : IRequestHandler<GetAuthConfig, Result<AuthConfigResponse>>
{
    private readonly AuthenticationSettings _authSettings = authSettings.Value;

    public ValueTask<Result<AuthConfigResponse>> Handle(GetAuthConfig request, CancellationToken cancellationToken)
    {
        var config = new AuthConfigResponse(
            _authSettings.Mode,
            _authSettings.Local.AllowRegistration,
            _authSettings.Local.RequireEmailConfirmation
        );

        return ValueTask.FromResult(Result.Success(config));
    }
}
