import React from 'react';
import { SearchProvider } from '../../providers/search-provider';
import { LocationProvider } from '../../providers/location-provider';
import { Route, Routes } from 'react-router';
import { Locations } from './locations';
import { AddLocation } from './add-location';
import { EditLocation } from './edit-location';
import { ManageLocationUsers } from './manage-location-users';
import { Location } from './location';
import { BoxRoutes } from '../boxes/box-routes';
import { AddBox } from '../boxes/add-box';

export const LocationRoutes: React.FC = () => {
    return (
        <SearchProvider>
            <Routes>
                <Route path="add" element={<AddLocation />} />
                <Route index element={<Locations />} />
                <Route path=":locationId/*" element={
                    <LocationProvider>
                        <Routes>
                            <Route path="edit" element={<EditLocation />} />
                            <Route path="users" element={<ManageLocationUsers />} />
                            <Route path="box/add" element={<AddBox />} />
                            <Route path="box/:boxId/*" element={<BoxRoutes />} />
                            <Route index element={<Location />} />
                        </Routes>
                    </LocationProvider>
                } />
            </Routes>
        </SearchProvider>
    );
}