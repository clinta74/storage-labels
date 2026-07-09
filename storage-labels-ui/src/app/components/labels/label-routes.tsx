import React from 'react';
import { Navigate, Route, Routes } from 'react-router';
import { LabelJobsPage } from './label-jobs';
import { LabelJobPage } from './label-job';
import { LabelPrintPage } from './label-print-page';
import { EditLabelJobPage } from './edit-label-job';
import { CreateLabelJobPage } from './create-label-job';

export const LabelRoutes: React.FC = () => {
    return (
        <Routes>
            <Route index element={<LabelJobsPage />} />
            <Route path="create" element={<CreateLabelJobPage />} />
            <Route path=":jobId" element={<LabelJobPage />} />
            <Route path=":jobId/edit" element={<EditLabelJobPage />} />
            <Route path=":jobId/print" element={<LabelPrintPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    );
};
