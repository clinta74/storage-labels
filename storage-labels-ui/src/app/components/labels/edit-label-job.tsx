import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router';
import {
    Box,
    Button,
    FormControl,
    InputLabel,
    MenuItem,
    Paper,
    Select,
    Stack,
    TextField,
    Typography,
} from '@mui/material';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';

type Params = Record<'jobId', string>;

export const EditLabelJobPage: React.FC = () => {
    const params = useParams<Params>();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();

    const [job, setJob] = useState<LabelPrintJobResponse | null>(null);
    const [name, setName] = useState('');
    const [labelFormat, setLabelFormat] = useState<LabelFormat>('Avery94107');
    const [incrementAlgorithm, setIncrementAlgorithm] = useState<LabelIncrementAlgorithm>('Base36Suffix');
    const [algorithmPrefix, setAlgorithmPrefix] = useState('');
    const [algorithmSuffixLength, setAlgorithmSuffixLength] = useState(4);
    const [codeColorPattern, setCodeColorPattern] = useState('');
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        const jobId = params.jobId;
        if (jobId) {
            Api.Label.getLabelJobById(jobId)
                .then(({ data }) => {
                    setJob(data);
                    setName(data.name);
                    setLabelFormat(data.labelFormat);
                    setIncrementAlgorithm(data.incrementAlgorithm);
                    setAlgorithmPrefix(data.algorithmPrefix ?? '');
                    setAlgorithmSuffixLength(data.algorithmSuffixLength);
                    setCodeColorPattern(data.codeColorPattern);
                })
                .catch(() => alert.addMessage('Failed to load label job.'));
        }
    }, [params.jobId]);

    const validate = (): boolean => {
        const next: Record<string, string> = {};
        if (!name.trim()) next.name = 'Name is required.';
        if (algorithmSuffixLength < 1 || algorithmSuffixLength > 10)
            next.algorithmSuffixLength = 'Suffix length must be between 1 and 10.';
        if (algorithmPrefix && algorithmPrefix.length > 50)
            next.algorithmPrefix = 'Prefix cannot exceed 50 characters.';
        setErrors(next);
        return Object.keys(next).length === 0;
    };

    const handleSave = () => {
        if (!validate() || !job) return;
        setSaving(true);
        const request: UpdateLabelPrintJobRequest = {
            name: name.trim(),
            labelFormat,
            incrementAlgorithm,
            algorithmPrefix: algorithmPrefix.trim() || undefined,
            algorithmSuffixLength,
            codeColorPattern,
        };
        Api.Label.updateLabelJob(job.id, request)
            .then(() => {
                alert.addMessage(`Label job "${name.trim()}" updated.`);
                navigate('..');
            })
            .catch(() => alert.addMessage('Failed to update label job.'))
            .finally(() => setSaving(false));
    };

    if (!job) {
        return null;
    }

    return (
        <React.Fragment>
            <Box>
                <Paper>
                    <Box sx={{ margin: 1, textAlign: 'center' }}>
                    <Typography variant="h4">Edit Label Job</Typography>
                </Box>
                <Box sx={{ margin: 2 }}>
                    <Stack spacing={2}>
                        <FormControl fullWidth>
                            <TextField
                                variant="standard"
                                label="Name"
                                value={name}
                                onChange={e => setName(e.target.value)}
                                error={!!errors.name}
                                helperText={errors.name}
                                disabled={saving}
                                required
                                slotProps={{ htmlInput: { maxLength: 200 } }}
                            />
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel>Label Format</InputLabel>
                            <Select
                                variant="standard"
                                label="Label Format"
                                value={labelFormat}
                                onChange={e => setLabelFormat(e.target.value as LabelFormat)}
                                disabled={saving}
                            >
                                <MenuItem value="Avery94107">Avery 94107 (2&quot; × 2&quot;, 12/page)</MenuItem>
                            </Select>
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel>Increment Algorithm</InputLabel>
                            <Select
                                variant="standard"
                                label="Increment Algorithm"
                                value={incrementAlgorithm}
                                onChange={e => setIncrementAlgorithm(e.target.value as LabelIncrementAlgorithm)}
                                disabled={saving}
                            >
                                <MenuItem value="NumericOnly">Numeric Only (0000, 0001, ...)</MenuItem>
                                <MenuItem value="Base36Suffix">Base-36 Suffix (0000, ..., 000Z, 0010, ...)</MenuItem>
                            </Select>
                        </FormControl>
                        <FormControl fullWidth>
                            <TextField
                                variant="standard"
                                label="Code Prefix (optional)"
                                value={algorithmPrefix}
                                onChange={e => setAlgorithmPrefix(e.target.value)}
                                error={!!errors.algorithmPrefix}
                                helperText={errors.algorithmPrefix ?? 'Static text prepended before the suffix (e.g. A75ACC)'}
                                disabled={saving}
                                slotProps={{ htmlInput: { maxLength: 50 } }}
                            />
                        </FormControl>
                        <FormControl fullWidth>
                            <TextField
                                variant="standard"
                                label="Suffix Length"
                                type="number"
                                value={algorithmSuffixLength}
                                onChange={e => setAlgorithmSuffixLength(parseInt(e.target.value) || 4)}
                                error={!!errors.algorithmSuffixLength}
                                helperText={errors.algorithmSuffixLength ?? 'Number of characters in the incrementing suffix'}
                                disabled={saving}
                                slotProps={{ htmlInput: { min: 1, max: 10 } }}
                            />
                        </FormControl>
                        <FormControl fullWidth>
                            <TextField
                                variant="standard"
                                label="Code Color Pattern"
                                value={codeColorPattern}
                                onChange={e => setCodeColorPattern(e.target.value)}
                                helperText="Pattern used to color-highlight the code on labels (e.g. 3:primary,4:warning,*)"
                                disabled={saving}
                                slotProps={{ htmlInput: { maxLength: 200 } }}
                            />
                        </FormControl>
                    </Stack>
                    </Box>
                    <Stack
                        direction="row"
                        spacing={2}
                        sx={{
                            padding: 2,
                            justifyContent: 'flex-end',
                        }}
                    >
                        <Button color="primary" onClick={handleSave} disabled={saving}>
                            Save
                        </Button>
                        <Button color="secondary" onClick={() => navigate('..')} disabled={saving}>
                            Cancel
                        </Button>
                    </Stack>
                </Paper>
            </Box>
        </React.Fragment>
    );
};
