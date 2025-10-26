import React from 'react';
import { UserProvider } from '../../providers/user-provider';
import { Route, Routes } from 'react-router';
import { Locations } from './locations';
import { AddLocation } from './add-location';
import { EditLocation } from './edit-location';
import { Location } from './location';
import { BoxRoutes } from '../boxes/box-routes';
import { AddBox } from '../boxes/add-box';

export const LocationRoutes: React.FC = () => {
    return (
        <UserProvider>
            <Routes>
                <Route path="add" element={<AddLocation />} />
                <Route path=":locationId/edit" element={<EditLocation />} />
                <Route path=":locationId" element={<Location />} />
                <Route path=":locationId/box/add" element={<AddBox />} />
                <Route path=":locationId/box/:boxId/*" element={<BoxRoutes />} />
                <Route index element={<Locations />} />
            </Routes>
        </UserProvider>
    );
}