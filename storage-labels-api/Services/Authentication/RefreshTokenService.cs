using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Datalayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.Settings;

namespace StorageLabelsApi.Services.Authentication;

/// <summary>
/// Default implementation for refresh token management.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly StorageLabelsDbContext _dbContext;
    private readonly RefreshTokenSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        StorageLabelsDbContext dbContext,
        IOptions<RefreshTokenSettings> settings,
        TimeProvider timeProvider,
        ILogger<RefreshTokenService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
        _settings = settings.Value;
        _settings.Validate();
    }

    public async Task<RefreshTokenIssueResult> IssueAsync(
        string userId,
        bool persistent,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var (token, plainText) = CreateToken(userId, persistent, ipAddress, userAgent, parentTokenId: null);

        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.RefreshTokenIssued(userId, token.Id);

        return new RefreshTokenIssueResult(token.Id, plainText, token.ExpiresAt, token.IsPersistent);
    }

    public async Task<Result<RefreshTokenRotationResult>> RotateAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Unauthorized();
        }

        var tokenHash = HashToken(refreshToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            _logger.RefreshTokenMissing();
            return Result.Unauthorized();
        }

        if (storedToken.RevokedAt.HasValue)
        {
            _logger.RefreshTokenAlreadyRevoked(storedToken.UserId);
            return Result.Unauthorized();
        }

        if (storedToken.UsedAt.HasValue)
        {
            _logger.RefreshTokenReuseDetected(storedToken.UserId);
            return Result.Unauthorized();
        }

        if (storedToken.ExpiresAt <= now)
        {
            _logger.RefreshTokenExpired(storedToken.UserId);
            return Result.Unauthorized();
        }

        storedToken.UsedAt = now;

        var (replacement, plainText) = CreateToken(
            storedToken.UserId,
            storedToken.IsPersistent,
            ipAddress,
            userAgent,
            storedToken.Id);

        storedToken.ReplacedByTokenId = replacement.Id;

        _dbContext.RefreshTokens.Add(replacement);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.RefreshTokenRotated(storedToken.UserId, replacement.Id);

        return Result.Success(new RefreshTokenRotationResult(
            storedToken.UserId,
            replacement.Id,
            plainText,
            replacement.ExpiresAt,
            replacement.IsPersistent));
    }

    public async Task<int> RevokeUserTokensAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var revoked = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.UsedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);

        if (revoked > 0)
        {
            _logger.RefreshTokensRevoked(userId, revoked);
        }

        return revoked;
    }

    private (RefreshToken Token, string PlainText) CreateToken(
        string userId,
        bool persistent,
        string? ipAddress,
        string? userAgent,
        Guid? parentTokenId)
    {
        var plainText = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(plainText),
            CreatedAt = now,
            ExpiresAt = now.Add(_settings.GetLifetime(persistent)),
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            ParentTokenId = parentTokenId,
            IsPersistent = persistent
        };

        return (token, plainText);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
