using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StorageLabelsApi.Tests.TestInfrastructure;

public static class JwtTokenHelper
{
    /// <summary>
    /// Generates a signed JWT matching the app's validation parameters.
    /// Permissions are added as individual "permission" claims, which is what
    /// <see cref="StorageLabelsApi.Authorization.HasScopeHandler"/> checks.
    /// </summary>
    public static string GenerateToken(
        string userId,
        string secret,
        string issuer,
        string audience,
        string[] permissions,
        int expirationMinutes = 60)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
