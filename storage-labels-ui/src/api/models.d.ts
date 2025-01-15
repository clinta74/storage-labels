type UserId = string;
interface User {
    userId: UserId;
    fristName: string;
    lastName: string;
    emailAddress: string;
    planId: number;
    created: string;
    userDays?: UserDay[];
    userPlans?: UserPlan[];
}
interface CurrentUser {
    userId: UserId;
    firstName: string;
    lastName: string;
    emailAddress: string;
    lastLogin: string;
}
interface NewUser {
    userId: UserId;
    firstName: string;
    lastName: string;
    emailAddress: string;
}