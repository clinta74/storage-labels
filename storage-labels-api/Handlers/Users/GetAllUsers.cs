using Ardalis.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Datalayer.Models;

namespace StorageLabelsApi.Handlers.Users;

public record GetAllUsers : IRequest<Result<IEnumerable<UserWithRoles>>>;

public record UserWithRoles(string UserId, string Email, string? Username, string FullName, DateTime Created, bool IsActive, IEnumerable<string> Roles);

public class GetAllUsersHandler(
    UserManager<ApplicationUser> userManager,
    StorageLabelsDbContext dbContext,
    ILogger<GetAllUsersHandler> logger) 
    : IRequestHandler<GetAllUsers, Result<IEnumerable<UserWithRoles>>>
{
    public async ValueTask<Result<IEnumerable<UserWithRoles>>> Handle(GetAllUsers request, CancellationToken cancellationToken)
    {
        try
        {
            var users = userManager.Users.ToList();
            var usersWithRoles = new List<UserWithRoles>();

            foreach (var appUser in users)
            {
                var roles = await userManager.GetRolesAsync(appUser);
                
                // Get the corresponding User record from the database for FirstName/LastName
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

            logger.LogInformation("Retrieved {Count} users", usersWithRoles.Count);
            return Result<IEnumerable<UserWithRoles>>.Success(usersWithRoles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all users");
            return Result<IEnumerable<UserWithRoles>>.Error("Failed to retrieve users");
        }
    }
}
