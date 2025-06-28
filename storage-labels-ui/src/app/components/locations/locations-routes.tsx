import React from 'react';
import { UserProvider } from '../../providers/user-provider';
import { Route, Routes } from 'react-router';
import { Locations } from './locations';
import { AddLocation } from './add-location';
import { Location } from './location';

export const LocationRoutes: React.FC = () => {
    return (
        <UserProvider>
            <Routes>
                <Route path="add" element={<AddLocation />} />
                <Route path=":locationId" element={<Location />} />
                <Route index element={<Locations />} />
            </Routes>
        </UserProvider>
    );
}