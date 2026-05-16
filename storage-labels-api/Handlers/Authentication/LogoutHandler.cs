using StorageLabelsApi.Services.Authentication;

namespace StorageLabelsApi.Handlers.Authentication;

public record Logout(string UserId) : IRequest<Result>;

public class LogoutHandler(IAuthenticationService authService) 
    : IRequestHandler<Logout, Result>
{
    public async ValueTask<Result> Handle(Logout request, CancellationToken cancellationToken)
    {
        return await authService.LogoutAsync(request.UserId, cancellationToken);
    }
}
