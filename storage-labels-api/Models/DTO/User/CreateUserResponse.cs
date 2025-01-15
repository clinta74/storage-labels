namespace StorageLabelsApi.Models.DTO;

public record CreateUserResponse(
    string UserId,
    string FirstName,
    string LastName,
    string EmailAddress,
    DateTimeOffset CreatedDate
);
