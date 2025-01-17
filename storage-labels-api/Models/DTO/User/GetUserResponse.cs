namespace StorageLabelsApi.Models.DTO;

public record GetUserResponse(string UserId, string FirstName, string LastName, string EmailAddress, DateTimeOffset created);
