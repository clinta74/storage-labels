using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.DataLayer.Models;

public class ImageMetadata
{
    public Guid ImageId { get; set; }
    public required string UserId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string StoragePath { get; set; }
    public DateTime UploadedAt { get; set; }
    public long SizeInBytes { get; set; }
    public virtual ICollection<Box> ReferencedByBoxes { get; set; } = new List<Box>();
    public virtual ICollection<Item> ReferencedByItems { get; set; } = new List<Item>();
}
