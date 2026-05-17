using System.Text.Json.Serialization;

namespace StorageLabelsApi.DataLayer.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccessLevels
{
    None = 0,
    View = 1,
    Edit = 2,
    Owner = 3
}
