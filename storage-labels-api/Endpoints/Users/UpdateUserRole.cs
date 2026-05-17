using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Datalayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Endpoints.Users;

internal partial class UserEndpoints
{
    private static async Task<Results<Ok, NotFound<string>, ValidationProblem, ProblemHttpResult>> UpdateUserRole([FromRoute] string userid, UpdateUserRoleRequest request, [FromServices] UserManager<ApplicationUser> userManager, [FromServices] ILogger<UserEndpoints> logger, CancellationToken cancellationToken)
    {
        var validation = await new UpdateUserRoleValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        try
        {
            var user = await userManager.FindByIdAsync(userid);
            if (user is null)
            {
                logger.UserNotFoundForRoleUpdate(userid);
                return TypedResults.NotFound("User not found");
            }

            var currentRoles = await userManager.GetRolesAsync(user);

            if (currentRoles.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    logger.UserRoleRemovalFailed(userid, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return TypedResults.Problem("Failed to update user role", statusCode: 500);
                }
            }

            var addResult = await userManager.AddToRoleAsync(user, request.Role);
            if (!addResult.Succeeded)
            {
                logger.UserRoleAddFailed(request.Role, userid, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                return TypedResults.Problem("Failed to update user role", statusCode: 500);
            }

            await userManager.UpdateSecurityStampAsync(user);

            logger.UserRoleUpdated(userid, user.Email!, request.Role);

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            logger.UserRoleUpdateFailed(ex, userid);
            return TypedResults.Problem("Failed to update user role", statusCode: 500);
        }
    }

    private sealed class UpdateUserRoleValidator : AbstractValidator<UpdateUserRoleRequest>
    {
        public UpdateUserRoleValidator()
        {
            RuleFor(x => x.Role)
                .NotEmpty()
                .Must(role => role == "Admin" || role == "Auditor" || role == "User")
                .WithMessage("Role must be Admin, Auditor, or User");
        }
    }
}

public record UpdateUserRoleRequest(string Role);
