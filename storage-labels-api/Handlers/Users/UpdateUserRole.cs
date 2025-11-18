using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using StorageLabelsApi.Datalayer.Models;

namespace StorageLabelsApi.Handlers.Users;

public record UpdateUserRole(string UserId, string Role) : IRequest<Result>;

public class UpdateUserRoleValidator : AbstractValidator<UpdateUserRole>
{
    public UpdateUserRoleValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => role == "Admin" || role == "Auditor" || role == "User")
            .WithMessage("Role must be Admin, Auditor, or User");
    }
}

public class UpdateUserRoleHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<UpdateUserRoleHandler> logger) 
    : IRequestHandler<UpdateUserRole, Result>
{
    public async ValueTask<Result> Handle(UpdateUserRole request, CancellationToken cancellationToken)
    {
        var validation = await new UpdateUserRoleValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Invalid(validation.AsErrors());
        }

        try
        {
            var user = await userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found for role update", request.UserId);
                return Result.NotFound("User not found");
            }

            // Get current roles
            var currentRoles = await userManager.GetRolesAsync(user);

            // Remove all existing roles
            if (currentRoles.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    logger.LogError("Failed to remove roles from user {UserId}: {Errors}", 
                        request.UserId, 
                        string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return Result.Error("Failed to update user role");
                }
            }

            // Add new role
            var addResult = await userManager.AddToRoleAsync(user, request.Role);
            if (!addResult.Succeeded)
            {
                logger.LogError("Failed to add role {Role} to user {UserId}: {Errors}", 
                    request.Role,
                    request.UserId, 
                    string.Join(", ", addResult.Errors.Select(e => e.Description)));
                return Result.Error("Failed to update user role");
            }

            // Update security stamp to invalidate existing tokens
            await userManager.UpdateSecurityStampAsync(user);

            logger.LogInformation("Updated user {UserId} ({Email}) to role {Role}", 
                request.UserId, 
                user.Email, 
                request.Role);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating role for user {UserId}", request.UserId);
            return Result.Error("Failed to update user role");
        }
    }
}
