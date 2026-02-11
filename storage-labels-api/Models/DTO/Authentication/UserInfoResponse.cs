using StorageLabelsApi.Datalayer.Models;

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
)
{
    public UserInfoResponse(ApplicationUser user, string[] roles, string[] permissions) : this(
        user.Id,
        user.UserName ?? user.Email!,
        user.Email!,
        user.FullName,
        user.ProfilePictureUrl,
        roles,
        permissions,
        user.IsActive
    )
    { }
}
