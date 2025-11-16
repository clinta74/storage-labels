namespace StorageLabelsApi.Models;

public static class Policies
{
    public const string Write_User = "write:user";
    public const string Read_User = "read:user";
    public const string Write_CommonLocations = "write:common-locations";
    public const string Read_CommonLocations = "read:common-locations";
    public const string Write_EncryptionKeys = "write:encryption-keys";
    public const string Read_EncryptionKeys = "read:encryption-keys";
    public static string[] Permissions = [ 
        Write_User,
        Read_User,
        Write_CommonLocations,
        Read_CommonLocations,
        Write_EncryptionKeys,
        Read_EncryptionKeys
    ];
}

