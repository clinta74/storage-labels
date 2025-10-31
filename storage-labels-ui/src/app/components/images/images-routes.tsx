import React from 'react';
import { Route, Routes } from 'react-router';
import { Images } from './images';

export const ImagesRoutes: React.FC = () => {
    return (
        <Routes>
            <Route index element={<Images />} />
        </Routes>
    );
};
