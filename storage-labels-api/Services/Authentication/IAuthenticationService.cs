using StorageLabelsApi.Models.DTO.Authentication;

namespace StorageLabelsApi.Services.Authentication;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Login with username/email and password
    /// </summary>
    Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register a new user
    /// </summary>
    Task<Result<AuthenticationResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout a user
    /// </summary>
    Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current user information including permissions
    /// </summary>
    Task<Result<UserInfoResponse>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user permissions
    /// </summary>
    Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change password for the current user
    /// </summary>
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset password for any user (admin only)
    /// </summary>
    Task<Result> AdminResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);
}
