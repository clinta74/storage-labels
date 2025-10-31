using System.Text.Json;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO;

public record UserResponse(
    string UserId, 
    string FirstName, 
    string LastName, 
    string EmailAddress, 
    DateTimeOffset created, 
    UserPreferencesDto? Preferences)
{
    public UserResponse(User user) : this(
        user.UserId, 
        user.FirstName, 
        user.LastName, 
        user.EmailAddress, 
        user.Created,
        ParsePreferences(user.Preferences)) 
    { }

    private static UserPreferencesDto? ParsePreferences(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return new UserPreferencesDto();
        }

        try
        {
            return JsonSerializer.Deserialize<UserPreferencesDto>(preferencesJson) ?? new UserPreferencesDto();
        }
        catch (JsonException)
        {
            return new UserPreferencesDto();
        }
    }
}
