using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Endpoints.Users;

internal static partial class UserEndpoints
{
    private static async Task<Results<Ok<UserResponse>, ProblemHttpResult>> CreateUser(HttpContext context, CreateUserRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? context.User.FindFirst("email")?.Value
                    ?? "unknown@localhost";

        var existingUser = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId, cancellationToken);

        if (existingUser)
            return TypedResults.Problem($"User with the Id {userId} already exists.", statusCode: 409);

        var user = new User(
            UserId: userId,
            FirstName: request.FirstName,
            LastName: request.LastName,
            EmailAddress: email,
            Created: timeProvider.GetUtcNow()
        );

        var result = dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new UserResponse(result.Entity));
    }
}
