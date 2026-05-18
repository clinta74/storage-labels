namespace StorageLabelsApi.Models.DTO.Authentication;

public record AdminResetPasswordRequest(string UserId, string NewPassword);
