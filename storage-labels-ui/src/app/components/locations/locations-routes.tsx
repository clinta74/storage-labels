import React from 'react';
import { UserProvider } from '../../providers/user-provider';
import { Navigate, Route, Routes } from 'react-router';
import { Locations } from './locations';
import { AddLocation } from './add-location';

export const LocationRoutes: React.FC = () => {
    return (
        <UserProvider>
            <Routes>
                <Route path="add" element={<AddLocation />} />
                <Route index element={<Locations />} />
            </Routes>
        </UserProvider>
    );
}