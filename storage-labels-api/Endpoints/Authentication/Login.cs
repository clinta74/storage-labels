using Ardalis.Result.AspNetCore;
using Ardalis.Result.FluentValidation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Models.Settings;
using StorageLabelsApi.Services.Authentication;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints.Authentication;

internal partial class AuthenticationEndpoints
{
    private static async Task<IResult> Login(
        HttpContext context,
        [FromBody] LoginRequest request,
        [FromServices] IAuthenticationService authService,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var validation = await new LoginValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthenticationResult>.Invalid(validation.AsErrors()).ToMinimalApiResult();
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            IssueRefreshTokenCookie(context, refreshTokenOptions.Value, result.Value);
        }
        return result.ToMinimalApiResult();
    }

    private sealed class LoginValidator : AbstractValidator<LoginRequest>
    {
        public LoginValidator()
        {
            RuleFor(x => x.UsernameOrEmail).NotEmpty().WithMessage("Username or email is required");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
        }
    }
}
