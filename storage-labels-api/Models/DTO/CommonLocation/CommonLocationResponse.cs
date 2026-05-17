using System.Text.Json.Serialization;
using CommonLocationModel = StorageLabelsApi.DataLayer.Models.CommonLocation;

namespace StorageLabelsApi.Models.DTO.CommonLocation;

[method: JsonConstructor]
public record CommonLocationResponse(
    int CommonLocationId,
    string Name)
{
    public CommonLocationResponse(CommonLocationModel commonLocation) : this(
        commonLocation.CommonLocationId,
        commonLocation.Name)
    { }
}
