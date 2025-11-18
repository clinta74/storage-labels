namespace StorageLabelsApi.Models.DTO.Authentication;

/// <summary>
/// Login request
/// </summary>
public record LoginRequest(string UsernameOrEmail, string Password, bool RememberMe = false);
