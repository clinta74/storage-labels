import React from 'react';
import { UserProvider } from '../../providers/user-provider';
import { SearchProvider } from '../../providers/search-provider';
import { LocationProvider } from '../../providers/location-provider';
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
            <SearchProvider>
                <Routes>
                    <Route path="add" element={<AddLocation />} />
                    <Route index element={<Locations />} />
                    <Route path=":locationId/*" element={
                        <LocationProvider>
                            <Routes>
                                <Route path="edit" element={<EditLocation />} />
                                <Route path="box/add" element={<AddBox />} />
                                <Route path="box/:boxId/*" element={<BoxRoutes />} />
                                <Route index element={<Location />} />
                            </Routes>
                        </LocationProvider>
                    } />
                </Routes>
            </SearchProvider>
        </UserProvider>
    );
}