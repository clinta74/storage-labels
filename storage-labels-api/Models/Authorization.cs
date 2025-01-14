namespace StorageLabelsApi.Models;

public static class Policies
{
    public const string Write_User = "write:user";
    public const string Read_User = "read:user";
    public const string Write_CommonLocation = "write:common-location";
    public const string Read_CommonLocation = "read:common-location";
    public static string[] Permissions = [ 
        Write_User,
        Read_User,
        Write_CommonLocation,
        Read_CommonLocation
    ];
}

