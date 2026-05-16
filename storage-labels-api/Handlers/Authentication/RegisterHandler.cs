using Ardalis.Result.FluentValidation;
using FluentValidation;
using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Services.Authentication;

namespace StorageLabelsApi.Handlers.Authentication;

public record Register(
    string Email, 
    string Username, 
    string Password, 
    string FirstName,
    string LastName,
    string? FullName = null
) : IRequest<Result<AuthenticationResult>>;

public class RegisterHandler(IAuthenticationService authService) 
    : IRequestHandler<Register, Result<AuthenticationResult>>
{
    public async ValueTask<Result<AuthenticationResult>> Handle(Register request, CancellationToken cancellationToken)
    {
        var validation = await new RegisterValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthenticationResult>.Invalid(validation.AsErrors());
        }

        var registerRequest = new RegisterRequest(
            request.Email, 
            request.Username, 
            request.Password, 
            request.FirstName,
            request.LastName,
            request.FullName
        );
        return await authService.RegisterAsync(registerRequest, cancellationToken);
    }
}

public class RegisterValidator : AbstractValidator<Register>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Valid email address is required");
        
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters");
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters");
        
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MinimumLength(2)
            .WithMessage("First name must be at least 2 characters");
        
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MinimumLength(2)
            .WithMessage("Last name must be at least 2 characters");
    }
}
