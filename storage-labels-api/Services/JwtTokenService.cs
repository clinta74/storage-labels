using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StorageLabelsApi.Models.Settings;

namespace StorageLabelsApi.Services;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public class JwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings, TimeProvider timeProvider)
    {
        _jwtSettings = jwtSettings.Value;
        _timeProvider = timeProvider;
        _jwtSettings.Validate();
    }

    /// <summary>
    /// Generate JWT token for a user
    /// </summary>
    public string GenerateToken(string userId, string username, string email, string? fullName, string[] roles, string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName ?? username),
            new("username", username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role)); // Also add lowercase for compatibility
        }

        // Add permissions
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        // Also add as a single "permissions" claim for backward compatibility
        if (permissions.Length > 0)
        {
            claims.Add(new Claim("permissions", string.Join(" ", permissions)));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = _timeProvider.GetUtcNow().DateTime.AddMinutes(_jwtSettings.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Get token expiration time
    /// </summary>
    public DateTime GetTokenExpiration()
    {
        return _timeProvider.GetUtcNow().DateTime.AddMinutes(_jwtSettings.ExpirationMinutes);
    }
}
