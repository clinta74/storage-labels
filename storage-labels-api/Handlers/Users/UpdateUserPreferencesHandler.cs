using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Handlers.Users;

public record UpdateUserPreferences(string UserId, UserPreferencesResponse Preferences) : IRequest<Result<UserPreferencesResponse>>;

public class UpdateUserPreferencesHandler(
    StorageLabelsDbContext dbContext,
    ILogger<UpdateUserPreferencesHandler> logger) 
    : IRequestHandler<UpdateUserPreferences, Result<UserPreferencesResponse>>
{
    public async Task<Result<UserPreferencesResponse>> Handle(UpdateUserPreferences request, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == request.UserId)
            .AnyAsync(cancellationToken);

        if (!userExists)
        {
            return Result<UserPreferencesResponse>.NotFound($"User with id {request.UserId} not found.");
        }

        var preferencesJson = JsonSerializer.Serialize(request.Preferences);

        await dbContext.Users
            .Where(u => u.UserId == request.UserId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Preferences, preferencesJson),
                cancellationToken);

        logger.LogInformation("Updated preferences for user {UserId}", request.UserId);

        return Result<UserPreferencesResponse>.Success(request.Preferences);
    }
}
