import React from 'react';
import { Route, Routes } from 'react-router';
import { BoxComponent } from './box';
import { EditBox } from './edit-box';
import { AddBox } from './add-box';
import { ItemRoutes } from '../items/item-routes';

export const BoxRoutes: React.FC = () => {
    return (
        <Routes>
            <Route path="add" element={<AddBox />} />
            <Route path="edit" element={<EditBox />} />
            <Route path="item/*" element={<ItemRoutes />} />
            <Route index element={<BoxComponent />} />
        </Routes>
    );
};
