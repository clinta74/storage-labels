using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Datalayer.Models;

namespace StorageLabelsApi.Endpoints.Users;

internal static partial class UserEndpoints
{
    private static async Task<Results<Ok, NotFound<string>, ProblemHttpResult>> DeleteUser([FromRoute] string userId, [FromServices] UserManager<ApplicationUser> userManager, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var identityUser = await userManager.FindByIdAsync(userId);
        if (identityUser is null)
            return TypedResults.NotFound($"User with ID '{userId}' not found.");

        var legacyUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        var deleteResult = await userManager.DeleteAsync(identityUser);
        if (!deleteResult.Succeeded)
        {
            var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            return TypedResults.Problem($"Failed to delete user: {errors}", statusCode: 500);
        }

        if (legacyUser != null)
        {
            dbContext.Users.Remove(legacyUser);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok();
    }
}
