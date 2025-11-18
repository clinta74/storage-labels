namespace StorageLabelsApi.Models.DTO.Authentication;

/// <summary>
/// Authentication result containing JWT token and user info
/// </summary>
public record AuthenticationResult(string Token, DateTime ExpiresAt, UserInfoResponse User);
