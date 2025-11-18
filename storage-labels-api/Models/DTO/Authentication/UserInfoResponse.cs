namespace StorageLabelsApi.Models.DTO.Authentication;

/// <summary>
/// User information response
/// </summary>
public record UserInfoResponse(
    string UserId,
    string Username,
    string Email,
    string? FullName,
    string? ProfilePictureUrl,
    string[] Roles,
    string[] Permissions,
    bool IsActive
);
