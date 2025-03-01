type UserId = string;
type AccessLevels = "None" | "View" | "Edit" | "Owner";
interface UserResponse {
    userId: UserId;
    firstName: string;
    lastName: string;
    emailAddress: string;
    created: string;
}
interface NewUser {
    firstName: string;
    lastName: string;
    emailAddress: string;
}

interface LocationRequest {
    name: string;
}

interface StorageLocation {
    locationId: number;
    name: string;
    created: string;
    updated: string;
    accessLevel: AccessLevels; 
}