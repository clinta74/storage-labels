import React from 'react';
import { Navigate, Route, Routes } from 'react-router';
import { EncryptionKeysPage } from './encryption-keys-page';
import { RotationsPage } from './rotations-page';

export const EncryptionKeysRoutes: React.FC = () => {
    return (
        <Routes>
            <Route index element={<EncryptionKeysPage />} />
            <Route path="rotations" element={<RotationsPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    );
};
