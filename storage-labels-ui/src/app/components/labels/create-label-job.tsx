import React, { useState, useEffect } from 'react';
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
import { Link, useNavigate } from 'react-router';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { useUser } from '../../providers/user-provider';

export const CreateLabelJobPage: React.FC = () => {
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const { user } = useUser();

    const [name, setName] = useState('');
    const [labelFormat, setLabelFormat] = useState<LabelFormat>('Avery94107');
    const [incrementAlgorithm, setIncrementAlgorithm] = useState<LabelIncrementAlgorithm>('Base36Suffix');
    const [algorithmPrefix, setAlgorithmPrefix] = useState('');
    const [algorithmSuffixLength, setAlgorithmSuffixLength] = useState(4);
    const [startIndex, setStartIndex] = useState(0);
    const [codeColorPattern, setCodeColorPattern] = useState(user?.preferences?.codeColorPattern ?? '');
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [submitting, setSubmitting] = useState(false);

    useEffect(() => {
        setCodeColorPattern(user?.preferences?.codeColorPattern ?? '');
    }, [user]);

    const validate = (): boolean => {
        const next: Record<string, string> = {};
        if (!name.trim()) next.name = 'Name is required.';
        if (algorithmSuffixLength < 1 || algorithmSuffixLength > 10) next.algorithmSuffixLength = 'Suffix length must be between 1 and 10.';
        if (startIndex < 0) next.startIndex = 'Start index must be 0 or greater.';
        if (algorithmPrefix && algorithmPrefix.length > 50) next.algorithmPrefix = 'Prefix cannot exceed 50 characters.';
        setErrors(next);
        return Object.keys(next).length === 0;
    };

    const handleCreate = () => {
        if (!validate()) return;
        setSubmitting(true);
        const request: CreateLabelPrintJobRequest = {
            name: name.trim(),
            labelFormat,
            incrementAlgorithm,
            algorithmPrefix: algorithmPrefix.trim() || undefined,
            algorithmSuffixLength,
            startIndex,
            codeColorPattern,
        };
        Api.Label.createLabelJob(request)
            .then(() => {
                alert.addMessage(`Label job "${name.trim()}" created.`);
                navigate('..');
            })
            .catch(() => alert.addMessage('Failed to create label job.'))
            .finally(() => setSubmitting(false));
    };

    return (
        <React.Fragment>
            <Box>
                <Paper>
                    <Box sx={{ margin: 1, textAlign: 'center' }}>
                        <Typography variant="h4">Create Label Job</Typography>
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
                                    disabled={submitting}
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
                                    disabled={submitting}
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
                                    disabled={submitting}
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
                                    disabled={submitting}
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
                                    disabled={submitting}
                                    slotProps={{ htmlInput: { min: 1, max: 10 } }}
                                />
                            </FormControl>
                            <FormControl fullWidth>
                                <TextField
                                    variant="standard"
                                    label="Start Index"
                                    type="number"
                                    value={startIndex}
                                    onChange={e => setStartIndex(parseInt(e.target.value) || 0)}
                                    error={!!errors.startIndex}
                                    helperText={errors.startIndex ?? 'The index to begin counting from'}
                                    disabled={submitting}
                                    slotProps={{ htmlInput: { min: 0 } }}
                                />
                            </FormControl>
                            <FormControl fullWidth>
                                <TextField
                                    variant="standard"
                                    label="Code Color Pattern"
                                    value={codeColorPattern}
                                    onChange={e => setCodeColorPattern(e.target.value)}
                                    helperText="Pattern used to color-highlight the code on labels (e.g. 3:primary,4:warning,*)"
                                    disabled={submitting}
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
                        <Button color="primary" onClick={handleCreate} disabled={submitting}>
                            Create
                        </Button>
                        <Button color="secondary" component={Link} to="..">
                            Cancel
                        </Button>
                    </Stack>
                </Paper>
            </Box>
        </React.Fragment>
    );
};
