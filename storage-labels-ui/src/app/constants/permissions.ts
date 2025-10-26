export const Permissions = {
    Write_User: "write:user",
    Read_User: "read:user",
    Write_CommonLocations: "write:common-locations",
    Read_CommonLocations: "read:common-locations",
} as const;

export type Permission = typeof Permissions[keyof typeof Permissions];
