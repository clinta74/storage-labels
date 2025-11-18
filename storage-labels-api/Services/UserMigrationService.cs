using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Datalayer.Models;

namespace StorageLabelsApi.Services;

/// <summary>
/// Service for migrating users from Auth0 to local authentication
/// </summary>
public class UserMigrationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StorageLabelsDbContext _dbContext;
    private readonly ILogger<UserMigrationService> _logger;

    public UserMigrationService(
        UserManager<ApplicationUser> userManager,
        StorageLabelsDbContext dbContext,
        ILogger<UserMigrationService> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get list of users that need migration
    /// </summary>
    public async Task<List<UnmigratedUser>> GetUnmigratedUsersAsync(CancellationToken cancellationToken = default)
    {
        var legacyUsers = await _dbContext.Users.ToListAsync(cancellationToken);
        var unmigratedUsers = new List<UnmigratedUser>();

        foreach (var legacyUser in legacyUsers)
        {
            // Check if user already exists in Identity
            var existingUser = await _userManager.FindByIdAsync(legacyUser.UserId);
            if (existingUser == null)
            {
                unmigratedUsers.Add(new UnmigratedUser
                {
                    UserId = legacyUser.UserId,
                    FirstName = legacyUser.FirstName,
                    LastName = legacyUser.LastName,
                    Email = legacyUser.EmailAddress,
                    SuggestedUsername = GenerateUsername(legacyUser.EmailAddress, legacyUser.UserId),
                    CreatedAt = legacyUser.Created.UtcDateTime
                });
            }
        }

        return unmigratedUsers;
    }

    /// <summary>
    /// Migrate a single user with admin-specified username and password
    /// </summary>
    public async Task<Result<MigratedUserInfo>> MigrateUserAsync(
        string userId,
        string username,
        string password,
        bool requirePasswordChange = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Migrating user {UserId} with username {Username}", userId, username);

        // Get legacy user
        var legacyUser = await _dbContext.Users.FindAsync([userId], cancellationToken);
        if (legacyUser == null)
        {
            return Result.Error("User not found in legacy system");
        }

        // Check if already migrated
        var existingUser = await _userManager.FindByIdAsync(userId);
        if (existingUser != null)
        {
            return Result.Error("User already migrated");
        }

        // Check if username is taken
        var usernameTaken = await _userManager.FindByNameAsync(username);
        if (usernameTaken != null)
        {
            return Result.Error($"Username '{username}' is already taken");
        }

        // Create ApplicationUser
        var applicationUser = new ApplicationUser
        {
            Id = userId, // Use the same ID to maintain relationships
            UserName = username,
            Email = legacyUser.EmailAddress,
            FullName = $"{legacyUser.FirstName} {legacyUser.LastName}".Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = legacyUser.Created.UtcDateTime,
            UpdatedAt = DateTime.UtcNow,
            LockoutEnabled = !requirePasswordChange // Disable lockout if password change required
        };

        var createResult = await _userManager.CreateAsync(applicationUser, password);
        
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to migrate user {UserId}: {Errors}", userId, errors);
            return Result.Error(errors);
        }

        // Assign User role
        await _userManager.AddToRoleAsync(applicationUser, "User");

        // If password change required, set the flag
        if (requirePasswordChange)
        {
            await _userManager.SetLockoutEnabledAsync(applicationUser, false);
            // Note: You may want to add a custom claim or field to track "must change password"
        }
        
        _logger.LogInformation("Successfully migrated user {UserId} as {Username}", userId, username);

        return Result.Success(new MigratedUserInfo
        {
            UserId = userId,
            Username = username,
            Email = legacyUser.EmailAddress,
            FullName = $"{legacyUser.FirstName} {legacyUser.LastName}".Trim(),
            RequirePasswordChange = requirePasswordChange
        });
    }

    private static string GenerateUsername(string email, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var username = email.Split('@')[0];
            // Remove any invalid characters
            return new string(username.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
        }
        
        return $"user_{fallback[..8]}";
    }
}

public class UnmigratedUser
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SuggestedUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class MigratedUserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool RequirePasswordChange { get; set; }
}
