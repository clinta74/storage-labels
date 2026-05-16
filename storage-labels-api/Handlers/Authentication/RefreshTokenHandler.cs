using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Services.Authentication;

namespace StorageLabelsApi.Handlers.Authentication;

public record RefreshAuthentication(string RefreshToken) : IRequest<Result<AuthenticationResult>>;

public class RefreshTokenHandler(IAuthenticationService authService)
    : IRequestHandler<RefreshAuthentication, Result<AuthenticationResult>>
{
    public async ValueTask<Result<AuthenticationResult>> Handle(RefreshAuthentication request, CancellationToken cancellationToken)
    {
        return await authService.RefreshAsync(request.RefreshToken, cancellationToken);
    }
}
