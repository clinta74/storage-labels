import { AxiosInstance } from 'axios';

export type LabelApiEndpoints = ReturnType<typeof getLabelEndpoints>;

export const getLabelEndpoints = (client: AxiosInstance) => ({
    createLabelJob: (request: CreateLabelPrintJobRequest) =>
        client.post<LabelPrintJobResponse>('labels', request),

    getLabelJobs: () =>
        client.get<LabelPrintJobResponse[]>('labels'),

    getLabelJobById: (jobId: string) =>
        client.get<LabelPrintJobResponse>(`labels/${jobId}`),

    getNextPage: (jobId: string) =>
        client.post<LabelPageResponse>(`labels/${jobId}/next-page`),

    deleteLabelJob: (jobId: string) =>
        client.delete<never>(`labels/${jobId}`),

    updateLabelJob: (jobId: string, request: UpdateLabelPrintJobRequest) =>
        client.put<LabelPrintJobResponse>(`labels/${jobId}`, request),
});
