using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Users")]
public record User(
    [MaxLength(250)] string UserId,
    string FirstName, 
    string LastName, 
    [EmailAddress] string EmailAddress,
    DateTimeOffset Created)
{
    public ICollection<UserLocation> UserLocations { get; } = [];
}
