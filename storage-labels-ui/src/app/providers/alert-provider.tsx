import { Alert, AlertTitle, Box, Button, Divider, Portal } from '@mui/material';
import React, { createContext, PropsWithChildren, useState } from 'react';


interface AlertHandlers {
    addMessage: (alert: unknown) => void;
    addError: (error: unknown) => void;
    clearMessages: () => void;
    messages?: string[];
}

const defaultAlertHandlers = {
    addMessage: () => null,
    addError: () => null,
    clearMessages: () => null,
}

const AlertContext = createContext<AlertHandlers>(defaultAlertHandlers);

export const AlertProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const [messages, setMessages] = useState<string[]>();

    const addError = (error: unknown) => {
        let errorMessages: string[] = [];

        // Check if it's an Axios error with response data
        if (error && typeof error === 'object' && 'response' in error) {
            const axiosError = error as { response?: { data?: unknown } };
            const data = axiosError.response?.data;
            
            if (data && typeof data === 'object') {
                // Format 1: 409 Conflict - { detail: "Next error(s) occurred:* Error message\n" }
                if ('detail' in data && typeof data.detail === 'string') {
                    // Extract error messages from the detail field
                    // Remove "Next error(s) occurred:" prefix and split by newlines
                    const detail = data.detail
                        .replace(/^Next error\(s\) occurred:\s*/i, '')
                        .trim();
                    
                    errorMessages = detail
                        .split('\n')
                        .map(line => line.replace(/^\*\s*/, '').trim())
                        .filter(line => line.length > 0);
                }
                // Format 2: 400 Bad Request - Array of validation errors
                else if (Array.isArray(data)) {
                    errorMessages = data.map(err => {
                        if (typeof err === 'object' && err && 'errorMessage' in err) {
                            return String(err.errorMessage);
                        }
                        return typeof err === 'string' ? err : 'An error occurred';
                    });
                }
                // Format 3: Generic error message
                else if ('message' in data && typeof data.message === 'string') {
                    errorMessages = [data.message];
                }
                // Format 4: Title field (generic error)
                else if ('title' in data && typeof data.title === 'string') {
                    errorMessages = [data.title];
                }
            }
        }
        
        // Fallback to error.message or generic message
        if (errorMessages.length === 0) {
            if (error && typeof error === 'object' && 'message' in error) {
                errorMessages = [String(error.message)];
            } else {
                errorMessages = ['An unknown error has occurred.'];
            }
        }

        setMessages(_messages => _messages ? [..._messages, ...errorMessages] : errorMessages);
    };

    const alertHandlers = {
        addMessage: (alert: unknown) => {
            let message = "An unknown error has occured.";
            if (typeof alert === 'string')
                message = alert;
            else if (typeof alert === 'object')
                message = (alert as { message: string }).message;

            setMessages(_messages => _messages ? [..._messages, message] : [message]);
        },
        addError,
        clearMessages: () =>
            setMessages(undefined),
        messages,
    }

    return <AlertContext.Provider value={alertHandlers}>{children}</AlertContext.Provider>
}

export const AlertMessage: React.FC = () => {

    return (
        <AlertContext.Consumer>
            {
                value => value.messages &&
                    <Portal>
                        <Box 
                            sx={{ 
                                position: 'fixed', 
                                bottom: 0, 
                                left: 0, 
                                right: 0, 
                                zIndex: 9999,
                                maxWidth: '100%',
                                bgcolor: 'background.paper',
                                boxShadow: 3,
                                borderTop: 1,
                                borderColor: 'divider',
                            }}
                        >
                            <Box p={1} textAlign="right">
                                <Button onClick={value.clearMessages} variant="text">Clear</Button>
                            </Box>
                            <Divider />
                            {
                                value.messages.map((message, idx) =>
                                    <React.Fragment key={idx}>
                                        <Alert 
                                            severity="error"
                                            variant="standard"
                                            sx={{ 
                                                borderRadius: 0,
                                                '& .MuiAlert-message': {
                                                    width: '100%'
                                                }
                                            }}
                                        >
                                            <AlertTitle>Error</AlertTitle>
                                            {message}
                                        </Alert>
                                        {value.messages && idx < value.messages.length - 1 && <Divider />}
                                    </React.Fragment>
                                )
                            }
                        </Box>
                    </Portal>
            }
        </AlertContext.Consumer>
    );
}

export const useAlertMessage = () => {
    const context = React.useContext(AlertContext);

    if (context === undefined) {
        throw new Error('useAlertMessage must be used within a AlertProvider');
    }

    return { ...context };
}