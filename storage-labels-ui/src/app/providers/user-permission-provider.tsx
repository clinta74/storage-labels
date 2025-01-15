import React, { createContext, PropsWithChildren, useEffect, useState } from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { jwtDecode, JwtPayload } from "jwt-decode";


type HasPermissionHandler = (permissions: string | string[]) => boolean;

interface UserPermissionContext {
    permissions: string[];
    hasPermission: HasPermissionHandler;
}

type Auth0JwtPayload = JwtPayload & {
  permissions: string[];
};

export const UserPermissionContext = createContext<UserPermissionContext | null>(null);

export const UserPermissionProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const { isAuthenticated, getAccessTokenSilently, user } = useAuth0();
    const [permissions, setPermissions] = useState<string[]>([]);

    useEffect(() => {
        if (isAuthenticated) {
            getAccessTokenSilently()
                .then(token => {
                    const jwt = jwtDecode<Auth0JwtPayload>(token);
                    console.log('JWT: ', jwt);
                    setPermissions(jwt.permissions);
                })
                .catch(console.error);

        }
    }, [isAuthenticated, user]);

    const hasPermission: HasPermissionHandler = (permissionList) => {
        if (Array.isArray(permissionList)) {
            return permissionList.some(p => permissions.includes(p));
        }
        else {
            return permissions.includes(permissionList)
        }
    }

    return (
        <React.Fragment>
            {
                <UserPermissionContext.Provider value={{ permissions, hasPermission }}>{children}</UserPermissionContext.Provider>
            }
        </React.Fragment>
    );
}

export const useUserPermission = () => {
    const context = React.useContext(UserPermissionContext)
    if (context === null) throw new Error('useUserPermission must be used within a UserPermissionProvider');
    return context;
}

interface UserPermissionProps extends PropsWithChildren {
    permissions: string | string[];
}

export const Authorized: React.FC<UserPermissionProps> = ({permissions, children}) => {
    const { hasPermission } = useUserPermission();
    return (
        <React.Fragment>
        {
            hasPermission(permissions) && children
        }
        </React.Fragment>
    );
}