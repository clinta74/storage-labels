import React, { useState } from 'react';
import {
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    FormControl,
    InputLabel,
    MenuItem,
    Select,
    TextField,
} from '@mui/material';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { useUser } from '../../providers/user-provider';

interface CreateLabelJobDialogProps {
    open: boolean;
    onClose: () => void;
    onCreated: () => void;
}

export const CreateLabelJobDialog: React.FC<CreateLabelJobDialogProps> = ({ open, onClose, onCreated }) => {
    const { Api } = useApi();
    const alert = useAlertMessage();
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

    // Sync codeColorPattern from user preferences when dialog opens
    React.useEffect(() => {
        if (open) {
            setCodeColorPattern(user?.preferences?.codeColorPattern ?? '');
        }
    }, [open, user]);

    const validate = (): boolean => {
        const next: Record<string, string> = {};
        if (!name.trim()) next.name = 'Name is required.';
        if (algorithmSuffixLength < 1 || algorithmSuffixLength > 10) next.algorithmSuffixLength = 'Suffix length must be between 1 and 10.';
        if (startIndex < 0) next.startIndex = 'Start index must be 0 or greater.';
        if (algorithmPrefix && algorithmPrefix.length > 50) next.algorithmPrefix = 'Prefix cannot exceed 50 characters.';
        setErrors(next);
        return Object.keys(next).length === 0;
    };

    const handleSubmit = () => {
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
                alert.addMessage(`Label job "${name}" created.`);
                resetForm();
                onCreated();
            })
            .catch(() => alert.addMessage('Failed to create label job.'))
            .finally(() => setSubmitting(false));
    };

    const resetForm = () => {
        setName('');
        setLabelFormat('Avery94107');
        setIncrementAlgorithm('Base36Suffix');
        setAlgorithmPrefix('');
        setAlgorithmSuffixLength(4);
        setStartIndex(0);
        setErrors({});
    };

    const handleClose = () => {
        resetForm();
        onClose();
    };

    return (
        <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
            <DialogTitle>Create Label Job</DialogTitle>
            <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
                <TextField
                    label="Name"
                    value={name}
                    onChange={e => setName(e.target.value)}
                    error={!!errors.name}
                    helperText={errors.name}
                    fullWidth
                    autoFocus
                    slotProps={{ htmlInput: { maxLength: 200 } }}
                />
                <FormControl fullWidth>
                    <InputLabel>Label Format</InputLabel>
                    <Select
                        variant="standard"
                        label="Label Format"
                        value={labelFormat}
                        onChange={e => setLabelFormat(e.target.value as LabelFormat)}
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
                    >
                        <MenuItem value="NumericOnly">Numeric Only (0000, 0001, ...)</MenuItem>
                        <MenuItem value="Base36Suffix">Base-36 Suffix (0000, ..., 000Z, 0010, ...)</MenuItem>
                    </Select>
                </FormControl>
                <TextField
                    label="Code Prefix (optional)"
                    value={algorithmPrefix}
                    onChange={e => setAlgorithmPrefix(e.target.value)}
                    error={!!errors.algorithmPrefix}
                    helperText={errors.algorithmPrefix ?? 'Static text prepended before the suffix (e.g. A75ACC)'}
                    fullWidth
                    slotProps={{ htmlInput: { maxLength: 50 } }}
                />
                <TextField
                    label="Suffix Length"
                    type="number"
                    value={algorithmSuffixLength}
                    onChange={e => setAlgorithmSuffixLength(parseInt(e.target.value) || 4)}
                    error={!!errors.algorithmSuffixLength}
                    helperText={errors.algorithmSuffixLength ?? 'Number of characters in the incrementing suffix'}
                    fullWidth
                    slotProps={{ htmlInput: { min: 1, max: 10 } }}
                />
                <TextField
                    label="Start Index"
                    type="number"
                    value={startIndex}
                    onChange={e => setStartIndex(parseInt(e.target.value) || 0)}
                    error={!!errors.startIndex}
                    helperText={errors.startIndex ?? 'The index to begin counting from'}
                    fullWidth
                    slotProps={{ htmlInput: { min: 0 } }}
                />
                <TextField
                    label="Code Color Pattern"
                    value={codeColorPattern}
                    onChange={e => setCodeColorPattern(e.target.value)}
                    helperText="Pattern used to color-highlight the code on labels (e.g. 3:primary,4:warning,*)"
                    fullWidth
                    slotProps={{ htmlInput: { maxLength: 200 } }}
                />
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose} disabled={submitting}>Cancel</Button>
                <Button onClick={handleSubmit} variant="contained" disabled={submitting}>Create</Button>
            </DialogActions>
        </Dialog>
    );
};
