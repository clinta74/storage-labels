using System.Text.Json.Serialization;

namespace StorageLabelsApi.Models.DTO.Authentication;

/// <summary>
/// Authentication result containing JWT token and user info
/// </summary>
public record AuthenticationResult(string Token, DateTime ExpiresAt, UserInfoResponse User)
{
	/// <summary>
	/// Refresh token value (only used server-side for cookie issuance).
	/// </summary>
	[JsonIgnore]
	public string? RefreshToken { get; init; }

	/// <summary>
	/// Refresh token expiration (for cookie lifetime calculations).
	/// </summary>
	[JsonIgnore]
	public DateTime? RefreshTokenExpiresAt { get; init; }
}
