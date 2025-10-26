import React from 'react';
import { Route, Routes } from 'react-router';
import { AddItem } from './add-item';
import { EditItem } from './edit-item';

export const ItemRoutes: React.FC = () => {
    return (
        <Routes>
            <Route path="add" element={<AddItem />} />
            <Route path=":itemId/edit" element={<EditItem />} />
        </Routes>
    );
};
