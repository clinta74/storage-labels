import React from 'react';
import { Route, Routes } from 'react-router';
import { Images } from './images';
import { UserProvider } from '../../providers/user-provider';

export const ImagesRoutes: React.FC = () => {
    return (
        <UserProvider>
            <Routes>
                <Route index element={<Images />} />
            </Routes>
        </UserProvider>
    );
};
