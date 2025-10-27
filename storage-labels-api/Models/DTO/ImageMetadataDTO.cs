using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO;

public record ImageMetadataResponse(
    Guid ImageId,
    string FileName,
    string ContentType,
    string Url,
    DateTime UploadedAt,
    long SizeInBytes,
    int BoxReferenceCount,
    int ItemReferenceCount
)
{
    public ImageMetadataResponse(ImageMetadata img, string baseUrl) : this(
        img.ImageId,
        img.FileName,
        img.ContentType,
        $"{baseUrl}/images/{img.ImageId}",
        img.UploadedAt,
        img.SizeInBytes,
        img.ReferencedByBoxes?.Count ?? 0,
        img.ReferencedByItems?.Count ?? 0
    ) { }
}
