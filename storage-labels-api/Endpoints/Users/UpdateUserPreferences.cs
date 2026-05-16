using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Endpoints.Users;

internal static partial class UserEndpoints
{
    private static async Task<Results<Ok<UserPreferencesResponse>, NotFound<string>>> UpdateUserPreferences(HttpContext context, UserPreferencesResponse request, [FromServices] StorageLabelsDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var userExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId, cancellationToken);

        if (!userExists)
            return TypedResults.NotFound($"User with id {userId} not found.");

        var preferencesJson = JsonSerializer.Serialize(request);

        await dbContext.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Preferences, preferencesJson),
                cancellationToken);

        logger.UserPreferencesUpdated(userId);

        return TypedResults.Ok(request);
    }
}
