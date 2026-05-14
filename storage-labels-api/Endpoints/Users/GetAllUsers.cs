using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Datalayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Endpoints.Users;

internal static partial class UserEndpoints
{
    private static async Task<Results<Ok<List<UserWithRoles>>, ProblemHttpResult>> GetAllUsers([FromServices] UserManager<ApplicationUser> userManager, [FromServices] StorageLabelsDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var users = userManager.Users.ToList();
            var usersWithRoles = new List<UserWithRoles>();

            foreach (var appUser in users)
            {
                var roles = await userManager.GetRolesAsync(appUser);

                var dbUser = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.UserId == appUser.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var fullName = dbUser != null
                    ? $"{dbUser.FirstName} {dbUser.LastName}".Trim()
                    : appUser.FullName ?? "";

                usersWithRoles.Add(new UserWithRoles(
                    appUser.Id,
                    appUser.Email ?? "",
                    appUser.UserName,
                    fullName,
                    appUser.CreatedAt,
                    appUser.IsActive,
                    roles
                ));
            }

            logger.UsersRetrieved(usersWithRoles.Count);
            return TypedResults.Ok(usersWithRoles);
        }
        catch (Exception ex)
        {
            logger.UsersRetrievalFailed(ex);
            return TypedResults.Problem("Failed to retrieve users", statusCode: 500);
        }
    }
}

public record UserWithRoles(string UserId, string Email, string? Username, string FullName, DateTime Created, bool IsActive, IEnumerable<string> Roles);
