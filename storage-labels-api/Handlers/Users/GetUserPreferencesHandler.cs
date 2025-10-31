using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi.Handlers.Users;

public record GetUserPreferences(string UserId) : IRequest<Result<UserPreferencesDto>>;

public class GetUserPreferencesHandler(StorageLabelsDbContext dbContext) 
    : IRequestHandler<GetUserPreferences, Result<UserPreferencesDto>>
{
    public async Task<Result<UserPreferencesDto>> Handle(GetUserPreferences request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == request.UserId)
            .Select(u => new { u.Preferences })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result<UserPreferencesDto>.NotFound($"User with id {request.UserId} not found.");
        }

        // If no preferences stored, return defaults
        if (string.IsNullOrWhiteSpace(user.Preferences))
        {
            return Result<UserPreferencesDto>.Success(new UserPreferencesDto());
        }

        try
        {
            var preferences = JsonSerializer.Deserialize<UserPreferencesDto>(user.Preferences);
            return Result<UserPreferencesDto>.Success(preferences ?? new UserPreferencesDto());
        }
        catch (JsonException)
        {
            // If JSON is invalid, return defaults
            return Result<UserPreferencesDto>.Success(new UserPreferencesDto());
        }
    }
}
