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

    public NoAuthenticationService(JwtTokenService jwtTokenService, ILogger<NoAuthenticationService> logger)
    {
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // In no-auth mode, any login succeeds with anonymous user
        _logger.LogInformation("No-auth mode: Login attempt for {Username}", request.UsernameOrEmail);

        var userId = "anonymous";
        var username = request.UsernameOrEmail;
        var email = $"{username}@localhost";
        var roles = new[] { "Admin" };
        var permissions = Policies.Permissions;

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
        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<AuthenticationResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // In no-auth mode, registration is not needed - just login
        _logger.LogInformation("No-auth mode: Registration attempt for {Username} - redirecting to login", request.Username);
        
        var loginRequest = new LoginRequest(request.Username, request.Password);
        return LoginAsync(loginRequest, cancellationToken);
    }

    public Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        // No-auth mode doesn't track sessions
        _logger.LogInformation("No-auth mode: Logout for {UserId}", userId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<UserInfoResponse>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Return anonymous user with all permissions
        var userInfo = new UserInfoResponse(
            "anonymous",
            "Anonymous",
            "anonymous@localhost",
            "Anonymous User",
            null,
            new[] { "Admin" },
            Policies.Permissions,
            true
        );

        return Task.FromResult(Result.Success(userInfo));
    }

    public Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        // In NoAuth mode, all users have all permissions
        return Task.FromResult(Policies.Permissions);
    }

    public Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        // No password management in NoAuth mode
        return Task.FromResult(Result.Error("Password management is not available in NoAuth mode"));
    }

    public Task<Result> AdminResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        // No password management in NoAuth mode
        return Task.FromResult(Result.Error("Password management is not available in NoAuth mode"));
    }
}
