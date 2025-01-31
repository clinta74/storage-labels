import React from 'react';
import { UserProvider } from '../../providers/user-provider';
import { Navigate, Route, Routes } from 'react-router';
import { Locations } from './locations';

export const LocationRoutes: React.FC = () => {
    return (
        <UserProvider>
            <Routes>
                <Route index element={<Locations />} />
            </Routes>
        </UserProvider>
    );
}