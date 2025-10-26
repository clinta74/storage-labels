using Microsoft.AspNetCore.WebUtilities;

namespace StorageLabelsApi.Extensions;

public static class Base64UrlEncoder
{
    public static string EncodeGuid(Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static string EncodeString(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static bool TryDecodeGuid(string encoded, out Guid guid)
    {
        guid = Guid.Empty;
        try
        {
            var bytes = WebEncoders.Base64UrlDecode(encoded);
            if (bytes.Length == 16)
            {
                guid = new Guid(bytes);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryDecodeString(string encoded, out string decoded)
    {
        decoded = string.Empty;
        try
        {
            var bytes = WebEncoders.Base64UrlDecode(encoded);
            decoded = System.Text.Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
