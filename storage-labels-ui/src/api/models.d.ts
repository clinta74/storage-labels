type UserId = string;
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

interface Location {
    locationId: number;
    name: string;
    created: string;
    updated: string;
}