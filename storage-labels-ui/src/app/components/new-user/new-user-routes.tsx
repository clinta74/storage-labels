import React from 'react';
import { Route, Routes } from 'react-router';
import { NewUser } from './new-user';

export const NewUserRoutes: React.FC = () => {
    return (
        <Routes>
            <Route path="/" element={<NewUser />} />
        </Routes>
    );
}