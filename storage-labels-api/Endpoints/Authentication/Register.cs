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
    private static async Task<IResult> Register(
        HttpContext context,
        [FromBody] RegisterRequest request,
        [FromServices] IAuthenticationService authService,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var validation = await new RegisterValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthenticationResult>.Invalid(validation.AsErrors()).ToMinimalApiResult();
        }

        var result = await authService.RegisterAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            IssueRefreshTokenCookie(context, refreshTokenOptions.Value, result.Value);
        }
        return result.ToMinimalApiResult();
    }

    private sealed class RegisterValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required").EmailAddress().WithMessage("Valid email address is required");
            RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required").MinimumLength(3).WithMessage("Username must be at least 3 characters");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required").MinimumLength(8).WithMessage("Password must be at least 8 characters");
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required").MinimumLength(2).WithMessage("First name must be at least 2 characters");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required").MinimumLength(2).WithMessage("Last name must be at least 2 characters");
        }
    }
}
