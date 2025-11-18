import React, { createContext, PropsWithChildren, useEffect, useState } from 'react';
import { useAuth } from '../../auth/auth-provider';
import { jwtDecode, JwtPayload } from "jwt-decode";


type HasPermissionHandler = (permissions: string | string[]) => boolean;

interface UserPermissionContext {
    permissions: string[];
    hasPermission: HasPermissionHandler;
}

type Auth0JwtPayload = JwtPayload & {
  permission: string[];
};

export const UserPermissionContext = createContext<UserPermissionContext | null>(null);

export const UserPermissionProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const { isAuthenticated, getAccessToken, authMode, user } = useAuth();
    const [permissions, setPermissions] = useState<string[]>([]);

    useEffect(() => {
        if (authMode === 'None') {
            // In NoAuth mode, grant all permissions (will be handled server-side)
            setPermissions([]);
            return;
        }

        if (isAuthenticated) {
            getAccessToken()
                .then(token => {
                    if (token) {
                        const jwt = jwtDecode<Auth0JwtPayload>(token);
                        setPermissions(jwt.permission || []);
                    }
                })
                .catch(console.error);
        }
    }, [isAuthenticated, authMode, user]);

    const hasPermission: HasPermissionHandler = (permissionList) => {
        // In NoAuth mode, always return true
        if (authMode === 'None') {
            return true;
        }

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