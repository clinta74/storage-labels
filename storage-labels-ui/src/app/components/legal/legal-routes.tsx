import React from 'react';
import { Route, Routes } from 'react-router';
import { PrivacyPolicy } from './privacy-policy';
import { TermsConditions } from './terms-and-conditions';

export const LegalRoutes: React.FC = () => {
    return (
        <Routes>
            <Route path="privacy" element={< PrivacyPolicy />} />
            <Route path="terms" element={< TermsConditions />} />
        </Routes>
    );
}