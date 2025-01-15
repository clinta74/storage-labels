type UserId = string;
interface User {
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
