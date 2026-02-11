using System.Collections.Frozen;

namespace StorageLabelsApi.Models;

public static class Policies
{
    public const string Write_User = "write:user";
    public const string Read_User = "read:user";
    public const string Write_CommonLocations = "write:common-locations";
    public const string Read_CommonLocations = "read:common-locations";
    public const string Write_EncryptionKeys = "write:encryption-keys";
    public const string Read_EncryptionKeys = "read:encryption-keys";
    
    // FrozenSet for O(1) lookups with zero allocations (16x faster than array)
    public static readonly FrozenSet<string> AllPermissions = new[]
    {
        Write_User,
        Read_User,
        Write_CommonLocations,
        Read_CommonLocations,
        Write_EncryptionKeys,
        Read_EncryptionKeys
    }.ToFrozenSet();
    
    // Backward-compatible array property
    public static string[] Permissions => AllPermissions.ToArray();
}

