using Microsoft.AspNetCore.Identity;

namespace StorageLabelsApi.Datalayer.Models;

/// <summary>
/// Application role extending ASP.NET Core Identity
/// </summary>
public class ApplicationRole : IdentityRole
{
    /// <summary>
    /// Description of the role
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Date the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
