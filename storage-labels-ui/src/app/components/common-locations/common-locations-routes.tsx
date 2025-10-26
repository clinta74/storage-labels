import React from 'react';
import { Route, Routes } from 'react-router';
import { CommonLocations } from './common-locations';
import { AddCommonLocation } from './add-common-location';
import { UserProvider } from '../../providers/user-provider';

export const CommonLocationsRoutes: React.FC = () => {
    return (
        <UserProvider>
            <Routes>
                <Route path="add" element={<AddCommonLocation />} />
                <Route index element={<CommonLocations />} />
            </Routes>
        </UserProvider>
    );
};
