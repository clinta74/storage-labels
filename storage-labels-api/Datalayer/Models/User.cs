using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Users")]
public record User
{
    [Key]
    [MaxLength(250)]
    public required string UserId { get; init; }
    [Required]
    public required string FirstName { get; init; }
    [Required]
    public required string LastName { get; init; }
    [EmailAddress]
    public required string EmailAddress { get; init; }
    public required DateTimeOffset Created { get; init; }
    public ICollection<UserLocation> UserLocations { get; } = [];
}
