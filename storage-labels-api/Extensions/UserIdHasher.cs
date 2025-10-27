using System.Security.Cryptography;
using System.Text;

namespace StorageLabelsApi.Extensions;

public static class UserIdHasher
{
    public static string HashUserId(string userId)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
