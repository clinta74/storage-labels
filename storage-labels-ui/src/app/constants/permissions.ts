export const Permissions = {
    Write_User: "write:user",
    Read_User: "read:user",
    Write_CommonLocations: "write:common-locations",
    Read_CommonLocations: "read:common-locations",
    Write_EncryptionKeys: "write:encryption-keys",
    Read_EncryptionKeys: "read:encryption-keys",
} as const;

export type Permission = typeof Permissions[keyof typeof Permissions];
