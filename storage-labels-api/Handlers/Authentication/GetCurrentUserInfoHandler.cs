using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Services.Authentication;

namespace StorageLabelsApi.Handlers.Authentication;

public record GetCurrentUserInfo(string UserId) : IRequest<Result<UserInfoResponse>>;

public class GetCurrentUserInfoHandler(IAuthenticationService authService) 
    : IRequestHandler<GetCurrentUserInfo, Result<UserInfoResponse>>
{
    public async ValueTask<Result<UserInfoResponse>> Handle(GetCurrentUserInfo request, CancellationToken cancellationToken)
    {
        return await authService.GetCurrentUserAsync(request.UserId, cancellationToken);
    }
}
