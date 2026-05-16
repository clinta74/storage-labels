using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Endpoints.Users;

internal static partial class UserEndpoints
{
    private static async Task<Results<Ok<UserResponse>, NotFound<string>>> GetCurrentUser(HttpContext context, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return TypedResults.NotFound($"User Id ({userId}) not found.");

        return TypedResults.Ok(new UserResponse(user));
    }
}
