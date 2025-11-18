using StorageLabelsApi.Models.Settings;

namespace StorageLabelsApi.Models.DTO.Authentication;

/// <summary>
/// Authentication configuration response
/// </summary>
public record AuthConfigResponse(AuthenticationMode Mode, bool AllowRegistration, bool RequireEmailConfirmation);
