namespace StorageLabelsApi.Services.Authentication;

/// <summary>
/// Handles refresh token issuance, rotation, and revocation.
/// </summary>
public interface IRefreshTokenService
{
    Task<RefreshTokenIssueResult> IssueAsync(
        string userId,
        bool persistent,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<Result<RefreshTokenRotationResult>> RotateAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<int> RevokeUserTokensAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of issuing a refresh token.
/// </summary>
public sealed record RefreshTokenIssueResult(Guid TokenId, string PlainTextToken, DateTime ExpiresAt, bool IsPersistent);

/// <summary>
/// Result of rotating a refresh token.
/// </summary>
public sealed record RefreshTokenRotationResult(string UserId, Guid TokenId, string PlainTextToken, DateTime ExpiresAt, bool IsPersistent);
