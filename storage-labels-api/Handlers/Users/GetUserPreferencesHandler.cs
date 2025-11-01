using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Handlers.Users;

public record GetUserPreferences(string UserId) : IRequest<Result<UserPreferencesResponse>>;

public class GetUserPreferencesHandler(StorageLabelsDbContext dbContext) 
    : IRequestHandler<GetUserPreferences, Result<UserPreferencesResponse>>
{
    public async Task<Result<UserPreferencesResponse>> Handle(GetUserPreferences request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == request.UserId)
            .Select(u => new { u.Preferences })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result<UserPreferencesResponse>.NotFound($"User with id {request.UserId} not found.");
        }

        // If no preferences stored, return defaults
        if (string.IsNullOrWhiteSpace(user.Preferences))
        {
            return Result<UserPreferencesResponse>.Success(new UserPreferencesResponse());
        }

        try
        {
            var preferences = JsonSerializer.Deserialize<UserPreferencesResponse>(user.Preferences);
            return Result<UserPreferencesResponse>.Success(preferences ?? new UserPreferencesResponse());
        }
        catch (JsonException)
        {
            // If JSON is invalid, return defaults
            return Result<UserPreferencesResponse>.Success(new UserPreferencesResponse());
        }
    }
}
