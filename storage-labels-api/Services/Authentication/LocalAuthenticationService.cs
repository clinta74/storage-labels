using Microsoft.AspNetCore.Identity;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Datalayer.Models;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO.Authentication;

namespace StorageLabelsApi.Services.Authentication;

/// <summary>
/// Local authentication service using ASP.NET Core Identity
/// </summary>
public class LocalAuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly StorageLabelsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LocalAuthenticationService> _logger;

    public LocalAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        StorageLabelsDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<LocalAuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LoginAttempt(request.UsernameOrEmail);

        // Find user by username or email
        var user = await _userManager.FindByNameAsync(request.UsernameOrEmail)
                   ?? await _userManager.FindByEmailAsync(request.UsernameOrEmail);

        if (user == null)
        {
            _logger.LoginFailed(request.UsernameOrEmail);
            return Result.Unauthorized();
        }

        // Check if account is locked
        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.AccountLocked(user.Id);
            return Result.Forbidden();
        }

        // Check if account is active
        if (!user.IsActive)
        {
            _logger.AccountInactive(user.Id);
            return Result.Forbidden();
        }

        // Verify password
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        
        if (!result.Succeeded)
        {
            _logger.LoginFailed(request.UsernameOrEmail);
            
            if (result.IsLockedOut)
            {
                _logger.AccountLocked(user.Id);
                return Result.Forbidden();
            }
            
            return Result.Unauthorized();
        }

        _logger.LoginSucceeded(user.UserName ?? user.Email ?? user.Id);

        // Get user roles and permissions
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user.Id, cancellationToken);

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(
            user.Id,
            user.UserName ?? user.Email!,
            user.Email!,
            user.FullName,
            roles.ToArray(),
            permissions
        );

        var expiresAt = _jwtTokenService.GetTokenExpiration();

        var userInfo = new UserInfoResponse(
            user.Id,
            user.UserName ?? user.Email!,
            user.Email!,
            user.FullName,
            user.ProfilePictureUrl,
            roles.ToArray(),
            permissions,
            user.IsActive
        );

        return Result.Success(new AuthenticationResult(token, expiresAt, userInfo));
    }

    public async Task<Result<AuthenticationResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        _logger.RegistrationAttempt(request.Username);

        // Check if username already exists
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
        {
            _logger.RegistrationFailed(request.Username, "Username already exists");
            return Result.Error("Username already exists");
        }

        // Check if email already exists
        existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.RegistrationFailed(request.Username, "Email already exists");
            return Result.Error("Email already exists");
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.RegistrationFailed(request.Username, errors);
            return Result.Error(errors);
        }

        // Check if this is the first user - if so, make them an admin
        var userCount = _userManager.Users.Count();
        var roleToAssign = userCount == 1 ? "Admin" : "User";
        
        await _userManager.AddToRoleAsync(user, roleToAssign);
        
        if (roleToAssign == "Admin")
        {
            _logger.LogInformation("First user registered - assigned Admin role to {Username}", user.UserName);
        }

        // Create User record in database (legacy users table)
        var appUser = new User(
            UserId: user.Id,
            FirstName: request.FirstName,
            LastName: request.LastName,
            EmailAddress: user.Email!,
            Created: _timeProvider.GetUtcNow()
        );
        
        await _dbContext.Users.AddAsync(appUser, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.RegistrationSucceeded(request.Username);

        // Auto-login after registration
        var loginRequest = new LoginRequest(request.Username, request.Password);
        return await LoginAsync(loginRequest, cancellationToken);
    }

    public async Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.NotFound();
        }

        // Update security stamp to invalidate existing tokens
        await _userManager.UpdateSecurityStampAsync(user);
        
        _logger.UserLoggedOut(userId);
        
        return Result.Success();
    }

    public async Task<Result<UserInfoResponse>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);

        var userInfo = new UserInfoResponse(
            user.Id,
            user.UserName ?? user.Email!,
            user.Email!,
            user.FullName,
            user.ProfilePictureUrl,
            roles.ToArray(),
            permissions,
            user.IsActive
        );

        return Result.Success(userInfo);
    }

    public async Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return [];
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = new List<string>();

        // Map roles to permissions
        foreach (var role in roles)
        {
            var rolePermissions = GetPermissionsForRole(role);
            permissions.AddRange(rolePermissions);
        }

        return permissions.Distinct().ToArray();
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, errors);
            return Result.Error(errors);
        }

        _logger.LogInformation("Password changed successfully for user {UserId}", userId);
        return Result.Success();
    }

    public async Task<Result> AdminResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.NotFound();
        }

        // Remove current password and set new one
        await _userManager.RemovePasswordAsync(user);
        var result = await _userManager.AddPasswordAsync(user, newPassword);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Admin password reset failed for user {UserId}: {Errors}", userId, errors);
            return Result.Error(errors);
        }

        // Update security stamp to invalidate existing tokens
        await _userManager.UpdateSecurityStampAsync(user);
        
        _logger.LogWarning("Password reset by admin for user {UserId}", userId);
        return Result.Success();
    }

    private static string[] GetPermissionsForRole(string role)
    {
        return role.ToLower() switch
        {
            "admin" => Policies.Permissions,
            "auditor" => [
                Policies.Read_User,
                Policies.Read_CommonLocations,
                Policies.Read_EncryptionKeys
            ],
            "user" => [], // Standard users only need authentication, not specific permissions
            _ => []
        };
    }
}
