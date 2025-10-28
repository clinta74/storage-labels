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

interface Box {
        boxId: string;
        code: string;
        name: string;
        description: string;
        imageUrl: string;
        imageMetadataId?: string;
        locationId: number;
        created: string;
        updated: string;
        lastAccessed: string;
}

interface BoxRequest {
    code: string;
    name: string;
    locationId: number;
    description: string;
    imageUrl: string;
    imageMetadataId?: string;
}

interface CommonLocation {
    commonLocationId: number;
    name: string;
}

interface CommonLocationRequest {
    name: string;
}

interface UserLocationResponse {
    userId: string;
    firstName: string;
    lastName: string;
    emailAddress: string;
    accessLevel: AccessLevels;
}

interface AddUserLocationRequest {
    emailAddress: string;
    accessLevel: AccessLevels;
}

interface UpdateUserLocationRequest {
    accessLevel: AccessLevels;
}

interface ItemRequest {
    boxId: string;
    name: string;
    description?: string;
    imageUrl?: string;
    imageMetadataId?: string;
}

interface ItemResponse {
    itemId: string;
    boxId: string;
    name: string;
    description?: string;
    imageUrl?: string;
    imageMetadataId?: string;
    created: string;
    updated: string;
}

interface ImageMetadataResponse {
    imageId: string;
    fileName: string;
    contentType: string;
    url: string;
    uploadedAt: string;
    sizeInBytes: number;
    boxReferenceCount: number;
    itemReferenceCount: number;
}
interface SearchResultResponse {
    type: 'box' | 'item';
    boxId?: string;
    boxName?: string;
    boxCode?: string;
    itemId?: string;
    itemName?: string;
    itemCode?: string;
    locationId: string;
    locationName: string;
}

interface SearchResultsResponse {
    results: SearchResultResponse[];
}
