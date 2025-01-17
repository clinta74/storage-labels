using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("CommonLocations")]
public record CommonLocation(
    int CommonLocationId,
    string Name);