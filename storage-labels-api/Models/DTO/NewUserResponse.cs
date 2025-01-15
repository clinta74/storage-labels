namespace StorageLabelsApi.Models.DTO;

public record NewUserResponse(
    string UserId,
    string FirstName,
    string LastName,
    string EmailAddress
);
