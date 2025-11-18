using Ardalis.Result.FluentValidation;
using FluentValidation;
using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Services.Authentication;

namespace StorageLabelsApi.Handlers.Authentication;

public record Login(string UsernameOrEmail, string Password, bool RememberMe = false) : IRequest<Result<AuthenticationResult>>;

public class LoginHandler(IAuthenticationService authService) 
    : IRequestHandler<Login, Result<AuthenticationResult>>
{
    public async ValueTask<Result<AuthenticationResult>> Handle(Login request, CancellationToken cancellationToken)
    {
        var validation = await new LoginValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthenticationResult>.Invalid(validation.AsErrors());
        }

        var loginRequest = new LoginRequest(request.UsernameOrEmail, request.Password, request.RememberMe);
        return await authService.LoginAsync(loginRequest, cancellationToken);
    }
}

public class LoginValidator : AbstractValidator<Login>
{
    public LoginValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty()
            .WithMessage("Username or email is required");
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}
