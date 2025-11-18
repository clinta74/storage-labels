import React, { useEffect, useState } from 'react';
import {
    Container,
    Paper,
    Typography,
    FormControl,
    FormLabel,
    RadioGroup,
    FormControlLabel,
    Radio,
    Switch,
    Button,
    Box,
    CircularProgress,
    Alert,
    TextField,
    Divider,
    Stack,
} from '@mui/material';
import SaveIcon from '@mui/icons-material/Save';
import { useApi } from '../../../api';
import { useUser } from '../../providers/user-provider';
import { useAuth } from '../../../auth/auth-provider';

export const Preferences: React.FC = () => {
    const { Api } = useApi();
    const { user, updateUser } = useUser();
    const { authMode } = useAuth();
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);
    const [preferences, setPreferences] = useState<UserPreferences>({
        theme: 'light',
        showImages: true,
        codeColorPattern: '',
    });

    useEffect(() => {
        if (user?.preferences) {
            setPreferences(user.preferences);
        }
    }, [user]);

    const handleSave = async () => {
        try {
            setSaving(true);
            setError(null);
            setSuccess(false);
            await Api.User.updateUserPreferences(preferences);
            updateUser(); // Refresh user data to get updated preferences
            setSuccess(true);
            setTimeout(() => setSuccess(false), 3000);
        } catch (err: unknown) {
            const errorMessage = err instanceof Error ? err.message : 'Failed to save preferences';
            setError(errorMessage);
        } finally {
            setSaving(false);
        }
    };

    if (!user) {
        return (
            <Container>
                <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
                    <CircularProgress />
                </Box>
            </Container>
        );
    }

    return (
        <Container maxWidth="md">
            <Typography variant="h4" gutterBottom sx={{ mt: 2 }}>
                User Preferences
            </Typography>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
                    {error}
                </Alert>
            )}

            {success && (
                <Alert severity="success" sx={{ mb: 2 }}>
                    Preferences saved successfully!
                </Alert>
            )}

            <Paper sx={{ p: 3 }}>
                <FormControl component="fieldset" fullWidth sx={{ mb: 3 }}>
                    <FormLabel component="legend">Theme</FormLabel>
                    <RadioGroup
                        value={preferences.theme}
                        onChange={(e) => setPreferences({ ...preferences, theme: e.target.value })}
                    >
                        <FormControlLabel value="light" control={<Radio />} label="Light - Off-white background with dark text" />
                        <FormControlLabel value="dark" control={<Radio />} label="Dark - Dark background with light text" />
                    </RadioGroup>
                    <Typography variant="caption" color="text.secondary" sx={{ mt: 1 }}>
                        Theme changes will apply immediately after saving
                    </Typography>
                </FormControl>

                <FormControl fullWidth sx={{ mb: 3 }}>
                    <FormControlLabel
                        control={
                            <Switch
                                checked={preferences.showImages}
                                onChange={(e) =>
                                    setPreferences({ ...preferences, showImages: e.target.checked })
                                }
                            />
                        }
                        label="Show Images"
                    />
                    <Typography variant="caption" color="text.secondary" sx={{ ml: 4 }}>
                        When disabled, image placeholders will be shown instead of loading images (saves bandwidth)
                    </Typography>
                </FormControl>

                <Typography variant="h6" gutterBottom sx={{ mt: 3, mb: 2 }}>
                    Box Code Display
                </Typography>

                <FormControl fullWidth sx={{ mb: 3 }}>
                    <TextField
                        label="Color Pattern"
                        value={preferences.codeColorPattern}
                        onChange={(e) =>
                            setPreferences({ ...preferences, codeColorPattern: e.target.value })
                        }
                        placeholder="e.g., 3:primary,2:secondary,*,4:error"
                        helperText="Format: length:color,length:color,... Use * for remaining chars. Colors: primary, secondary, error, warning, info, success"
                    />
                    <Typography variant="caption" color="text.secondary" sx={{ mt: 1 }}>
                        Example: &ldquo;3:primary,2:secondary,*,4:error&rdquo; colors first 3 chars primary, next 2 secondary, skip middle, last 4 error
                    </Typography>
                </FormControl>

                <Stack direction="row" spacing={2} padding={2} justifyContent="right">
                    <Button
                        color="primary"
                        onClick={handleSave}
                        loading={saving}
                    >
                        Save Preferences
                    </Button>
                </Stack>
            </Paper>
        </Container>
    );
};
