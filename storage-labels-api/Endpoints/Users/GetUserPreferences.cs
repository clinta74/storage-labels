using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Endpoints.Users;

internal static partial class UserEndpoints
{
    private static async Task<Results<Ok<UserPreferencesResponse>, NotFound<string>>> GetUserPreferences(HttpContext context, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Preferences })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return TypedResults.NotFound($"User with id {userId} not found.");

        if (string.IsNullOrWhiteSpace(user.Preferences))
            return TypedResults.Ok(new UserPreferencesResponse());

        try
        {
            var preferences = JsonSerializer.Deserialize<UserPreferencesResponse>(user.Preferences);
            return TypedResults.Ok(preferences ?? new UserPreferencesResponse());
        }
        catch (JsonException)
        {
            return TypedResults.Ok(new UserPreferencesResponse());
        }
    }
}
