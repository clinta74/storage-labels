using Ardalis.Result;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Datalayer.Models;

namespace StorageLabelsApi.Handlers.Users;

public record DeleteUser(string UserId) : IRequest<Result>;

public class DeleteUserValidator : AbstractValidator<DeleteUser>
{
    public DeleteUserValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}

public class DeleteUserHandler(
    UserManager<ApplicationUser> userManager,
    StorageLabelsDbContext dbContext)
    : IRequestHandler<DeleteUser, Result>
{
    public async ValueTask<Result> Handle(DeleteUser request, CancellationToken cancellationToken)
    {
        // Find the user in Identity
        var identityUser = await userManager.FindByIdAsync(request.UserId);
        if (identityUser == null)
        {
            return Result.NotFound($"User with ID '{request.UserId}' not found.");
        }

        // Check if user exists in legacy users table
        var legacyUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

        // Delete from Identity (this will cascade delete roles, claims, etc.)
        var deleteResult = await userManager.DeleteAsync(identityUser);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            return Result.Error($"Failed to delete user: {errors}");
        }

        // Delete from legacy users table if exists
        if (legacyUser != null)
        {
            dbContext.Users.Remove(legacyUser);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
