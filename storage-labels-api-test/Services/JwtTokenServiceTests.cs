using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using StorageLabelsApi.Models.Settings;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _jwtSettings = new()
    {
        Secret = "this-is-a-very-long-secret-key-for-testing-purposes-at-least-32-chars",
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpirationMinutes = 2
    };

    private JwtTokenService CreateService(TimeProvider? timeProvider = null)
    {
        var options = Options.Create(_jwtSettings);
        var provider = timeProvider ?? TimeProvider.System;
        return new JwtTokenService(options, provider);
    }

    [Fact]
    public void GenerateToken_ProducesTokenWithCorrectExpiration()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var mockTimeProvider = new MockTimeProvider(now);
        var service = CreateService(mockTimeProvider);

        // Act
        var token = service.GenerateToken("user123", "testuser", "test@example.com", "Test User", ["Admin"], ["read", "write"]);

        // Assert
        token.ShouldNotBeNullOrEmpty();

        // Decode the token to check the exp claim
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ShouldNotBeNull();
        jwtToken.Issuer.ShouldBe("test-issuer");
        jwtToken.Audiences.ShouldContain("test-audience");

        // Get the exp claim (Unix timestamp)
        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
        expClaim.ShouldNotBeNull();

        // Convert Unix timestamp to DateTime
        var expUnix = long.Parse(expClaim!.Value);
        var expDateTime = UnixTimeStampToDateTime(expUnix);

        // The expiration should be approximately 2 minutes from now
        var expectedExpiration = now.AddMinutes(_jwtSettings.ExpirationMinutes);
        var timeDifference = Math.Abs((expDateTime - expectedExpiration).TotalSeconds);

        // Allow a small tolerance (1 second) for execution time
        timeDifference.ShouldBeLessThan(1);
    }

    [Fact]
    public void GenerateToken_IncludesAllClaims()
    {
        // Arrange
        var service = CreateService();
        var roles = new[] { "Admin", "User" };
        var permissions = new[] { "read", "write", "delete" };

        // Act
        var token = service.GenerateToken("user123", "testuser", "test@example.com", "Test User", roles, permissions);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value.ShouldBe("user123");
        jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value.ShouldBe("test@example.com");
        jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value.ShouldBe("Test User");
        jwtToken.Claims.FirstOrDefault(c => c.Type == "username")?.Value.ShouldBe("testuser");

        // Check roles
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "role").ToList();
        roleClaims.Count.ShouldBe(roles.Length);
        foreach (var role in roles)
        {
            roleClaims.Any(c => c.Value == role).ShouldBeTrue();
        }

        // Check permissions
        var permissionClaims = jwtToken.Claims.Where(c => c.Type == "permission").ToList();
        permissionClaims.Count.ShouldBe(permissions.Length);
        foreach (var permission in permissions)
        {
            permissionClaims.Any(c => c.Value == permission).ShouldBeTrue();
        }

        // Check jti claim exists
        jwtToken.Claims.FirstOrDefault(c => c.Type == "jti")?.Value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GetTokenExpiration_ReturnsCorrectUtcDateTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var mockTimeProvider = new MockTimeProvider(now);
        var service = CreateService(mockTimeProvider);

        // Act
        var expiration = service.GetTokenExpiration();

        // Assert
        expiration.Kind.ShouldBe(DateTimeKind.Utc);

        var expectedExpiration = now.AddMinutes(_jwtSettings.ExpirationMinutes);
        var timeDifference = Math.Abs((expiration - expectedExpiration).TotalSeconds);

        // Allow a small tolerance (1 second) for execution time
        timeDifference.ShouldBeLessThan(1);
    }

    [Fact]
    public void GenerateToken_ExpirationIsInUtcNotLocal()
    {
        // This test ensures the bug is fixed: the exp claim should be computed
        // in UTC (Kind = Utc), not Unspecified, to avoid re-interpretation as local time
        // Arrange
        var now = DateTime.UtcNow;
        var mockTimeProvider = new MockTimeProvider(now);
        var service = CreateService(mockTimeProvider);

        // Act
        var token = service.GenerateToken("user123", "testuser", "test@example.com", "Test User", [], []);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
        expClaim.ShouldNotBeNull();

        var expUnix = long.Parse(expClaim!.Value);
        var expDateTime = UnixTimeStampToDateTime(expUnix);

        // Verify that the expiration is NOT shifted by a timezone offset.
        // The expected expiration is now + 2 minutes in UTC
        var expectedExpiration = now.AddMinutes(2);

        // If the bug existed (treating Unspecified as local time on a UTC-4 machine),
        // the exp would be shifted forward by ~4 hours (14400 seconds).
        // We check that it's within 1 second of expected (not off by hours).
        var differenceSeconds = (expDateTime - expectedExpiration).TotalSeconds;
        differenceSeconds.ShouldBeLessThan(1);
        differenceSeconds.ShouldBeGreaterThan(-1);
    }

    [Fact]
    public void GenerateToken_WithDifferentExpirations_ProducesCorrectClaims()
    {
        // Arrange
        var expirationMinutes = 30;
        var customSettings = new JwtSettings
        {
            Secret = _jwtSettings.Secret,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = expirationMinutes
        };
        var options = Options.Create(customSettings);
        var now = DateTime.UtcNow;
        var mockTimeProvider = new MockTimeProvider(now);
        var service = new JwtTokenService(options, mockTimeProvider);

        // Act
        var token = service.GenerateToken("user123", "testuser", "test@example.com", "Test User", [], []);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
        expClaim.ShouldNotBeNull();

        var expUnix = long.Parse(expClaim!.Value);
        var expDateTime = UnixTimeStampToDateTime(expUnix);
        var expectedExpiration = now.AddMinutes(expirationMinutes);
        var timeDifference = Math.Abs((expDateTime - expectedExpiration).TotalSeconds);

        timeDifference.ShouldBeLessThan(1);
    }

    // Helper to convert Unix timestamp to DateTime
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }

    // Mock TimeProvider for testing
    private class MockTimeProvider : TimeProvider
    {
        private readonly DateTime _now;

        public MockTimeProvider(DateTime now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => new(_now);
    }
}
