using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi.Handlers.Users;

public record UpdateUserPreferences(string UserId, UserPreferencesDto Preferences) : IRequest<Result<UserPreferencesDto>>;

public class UpdateUserPreferencesHandler(
    StorageLabelsDbContext dbContext,
    ILogger<UpdateUserPreferencesHandler> logger) 
    : IRequestHandler<UpdateUserPreferences, Result<UserPreferencesDto>>
{
    public async Task<Result<UserPreferencesDto>> Handle(UpdateUserPreferences request, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == request.UserId)
            .AnyAsync(cancellationToken);

        if (!userExists)
        {
            return Result<UserPreferencesDto>.NotFound($"User with id {request.UserId} not found.");
        }

        var preferencesJson = JsonSerializer.Serialize(request.Preferences);

        await dbContext.Users
            .Where(u => u.UserId == request.UserId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Preferences, preferencesJson),
                cancellationToken);

        logger.LogInformation("Updated preferences for user {UserId}", request.UserId);

        return Result<UserPreferencesDto>.Success(request.Preferences);
    }
}
