namespace StorageLabelsApi.Models.DTO.Authentication;

/// <summary>
/// User registration request
/// </summary>
public record RegisterRequest(
    string Email, 
    string Username, 
    string Password, 
    string FirstName,
    string LastName,
    string? FullName = null
);
