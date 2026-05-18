namespace StorageLabelsApi.Models.DTO.Authentication;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
