using Microsoft.AspNetCore.Identity;
using StorageLabelsApi.Datalayer.Models;
using StorageLabelsApi.Models;

namespace StorageLabelsApi.Services;

/// <summary>
/// Service for initializing roles and seeding default data
/// </summary>
public class RoleInitializationService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RoleInitializationService> _logger;

    public RoleInitializationService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<RoleInitializationService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Initialize default roles
    /// </summary>
    public async Task InitializeRolesAsync()
    {
        await CreateRoleIfNotExistsAsync("Admin", "Administrator with full permissions");
        await CreateRoleIfNotExistsAsync("Auditor", "Auditor with read-only access to all resources");
        await CreateRoleIfNotExistsAsync("User", "Standard user with basic access");
    }

    /// <summary>
    /// Create default admin user if configured
    /// </summary>
    public async Task CreateDefaultAdminAsync(string? username, string? password, string? email)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("No default admin credentials configured - skipping admin creation");
            return;
        }

        var existingAdmin = await _userManager.FindByNameAsync(username);
        if (existingAdmin != null)
        {
            _logger.LogInformation("Default admin user '{Username}' already exists", username);
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = username,
            Email = email ?? $"{username}@localhost",
            FullName = "System Administrator",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(adminUser, password);
        
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
            _logger.LogInformation("Default admin user '{Username}' created successfully", username);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create default admin user: {Errors}", errors);
        }
    }

    private async Task CreateRoleIfNotExistsAsync(string roleName, string description)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var role = new ApplicationRole
            {
                Name = roleName,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _roleManager.CreateAsync(role);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Role '{RoleName}' created", roleName);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create role '{RoleName}': {Errors}", roleName, errors);
            }
        }
    }

    /// <summary>
    /// Get permissions for a role
    /// </summary>
    public static string[] GetPermissionsForRole(string role)
    {
        return role.ToLower() switch
        {
            "admin" => Policies.Permissions,
            "auditor" => [
                Policies.Read_User,
                Policies.Read_CommonLocations,
                Policies.Read_EncryptionKeys
            ],
            "user" => [],
            _ => []
        };
    }
}
