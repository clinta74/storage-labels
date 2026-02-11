using StorageLabelsApi.Logging;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO.Authentication;

namespace StorageLabelsApi.Services.Authentication;

/// <summary>
/// No authentication service - grants all permissions (for trusted networks)
/// </summary>
public class NoAuthenticationService : IAuthenticationService
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<NoAuthenticationService> _logger;
    private static readonly string[] Roles = new[] { "Admin" };

    public NoAuthenticationService(JwtTokenService jwtTokenService, ILogger<NoAuthenticationService> logger)
    {
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LoginAttempt(request.UsernameOrEmail);

        var userId = "anonymous";
        var username = request.UsernameOrEmail;
        var email = $"{username}@localhost";
        var roles = new[] { "Admin" };
        var permissions = Policies.AllPermissions.ToArray();

        var token = _jwtTokenService.GenerateToken(
            userId,
            username,
            email,
            "Anonymous User",
            roles,
            permissions
        );

        var expiresAt = _jwtTokenService.GetTokenExpiration();

        var userInfo = new UserInfoResponse(
            userId,
            username,
            email,
            "Anonymous User",
            null,
            roles,
            permissions,
            true
        );

        var result = new AuthenticationResult(token, expiresAt, userInfo);
        _logger.LoginSucceeded(username);
        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<AuthenticationResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        _logger.RegistrationAttempt(request.Username);

        var loginRequest = new LoginRequest(request.Username, request.Password);
        var loginTask = LoginAsync(loginRequest, cancellationToken);

        _logger.RegistrationSucceeded(request.Username);
        return loginTask;
    }

    public Task<Result<AuthenticationResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        _logger.RefreshFailed("anonymous", "NoAuth mode does not issue refresh tokens");
        return Task.FromResult(Result<AuthenticationResult>.Error("Refresh tokens are not available in NoAuth mode"));
    }

    public Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.UserLoggedOut(userId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<UserInfoResponse>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userInfo = new UserInfoResponse(
            "anonymous",
            "Anonymous",
            "anonymous@localhost",
            "Anonymous User",
            null,
            Roles,
            Policies.AllPermissions.ToArray(),
            true
        );

        return Task.FromResult(Result.Success(userInfo));
    }

    public Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Policies.AllPermissions.ToArray());
    }

    public Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Error("Password management is not available in NoAuth mode"));
    }

    public Task<Result> AdminResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Error("Password management is not available in NoAuth mode"));
    }
}
