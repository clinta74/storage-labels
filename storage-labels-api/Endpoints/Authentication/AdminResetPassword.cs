using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Services.Authentication;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints.Authentication;

internal partial class AuthenticationEndpoints
{
    private static async Task<IResult> AdminResetPassword(
        [FromBody] AdminResetPasswordRequest request,
        [FromServices] IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.AdminResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
        return result.ToMinimalApiResult();
    }
}
