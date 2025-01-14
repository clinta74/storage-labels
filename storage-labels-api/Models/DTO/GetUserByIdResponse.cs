namespace StorageLabelsApi.Models.DTO;

public record GetUserByIdResponse(string UserId, string FirstName, string LastName, string EmailAddress, DateTimeOffset created);
