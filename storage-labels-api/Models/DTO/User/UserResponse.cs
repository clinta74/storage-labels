using System.Text.Json;
using System.Text.Json.Serialization;
using UserModel = StorageLabelsApi.DataLayer.Models.User;

namespace StorageLabelsApi.Models.DTO.User;

[method: JsonConstructor]
public record UserResponse(
    string UserId, 
    string FirstName, 
    string LastName, 
    string EmailAddress, 
    DateTimeOffset created, 
    UserPreferencesResponse? Preferences)
{
    public UserResponse(UserModel user) : this(
        user.UserId, 
        user.FirstName, 
        user.LastName, 
        user.EmailAddress, 
        user.Created,
        ParsePreferences(user.Preferences)) 
    { }

    private static UserPreferencesResponse? ParsePreferences(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return new UserPreferencesResponse();
        }

        try
        {
            return JsonSerializer.Deserialize<UserPreferencesResponse>(preferencesJson) ?? new UserPreferencesResponse();
        }
        catch (JsonException)
        {
            return new UserPreferencesResponse();
        }
    }
}
