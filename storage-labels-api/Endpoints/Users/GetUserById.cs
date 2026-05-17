using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Endpoints.Users;

internal partial class UserEndpoints
{
    private static async Task<Results<Ok<UserResponse>, NotFound<string>>> GetUserById([FromRoute] string userid, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userid)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return TypedResults.NotFound($"User Id ({userid}) not found.");

        return TypedResults.Ok(new UserResponse(user));
    }
}
