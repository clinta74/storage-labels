using CommonLocationModel = StorageLabelsApi.DataLayer.Models.CommonLocation;

namespace StorageLabelsApi.Models.DTO.CommonLocation;

public record CommonLocationResponse(
    int CommonLocationId,
    string Name)
{
    public CommonLocationResponse(CommonLocationModel commonLocation) : this(
        commonLocation.CommonLocationId,
        commonLocation.Name)
    { }
}
