using System.Security.Cryptography;
using System.Text;

namespace StorageLabelsApi.Extensions;

public static class UserIdHasher
{
    public static string HashUserId(string userId)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
