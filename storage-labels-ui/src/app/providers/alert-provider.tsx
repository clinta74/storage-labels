import { Alert, AlertTitle, Box, Button, Divider, Portal, Theme } from '@mui/material';
import React, { createContext, PropsWithChildren, useState } from 'react';


interface AlertHandlers {
    addMessage: (alert: unknown) => void;
    clearMessages: () => void;
    messages?: string[];
}

const defaultAlertHandlers = {
    addMessage: () => null,
    clearMessages: () => null,
}

const AlertContext = createContext<AlertHandlers>(defaultAlertHandlers);

export const AlertProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const [messages, setMessages] = useState<string[]>();

    const alertHandlers = {
        addMessage: (alert: unknown) => {
            let message = "An unknown error has occured.";
            if (typeof alert === 'string')
                message = alert;
            else if (typeof alert === 'object')
                message = (alert as { message: string }).message;

            setMessages(_messages => _messages ? [..._messages, message] : [message]);
        },
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
                        <Box boxShadow={2}>
                            <Box p={1} textAlign="right">
                                <Button onClick={value.clearMessages}>Clear</Button>
                            </Box>
                            {
                                value.messages.map((message, idx) =>
                                    <React.Fragment key={idx}>
                                        <Alert severity="error" >
                                            <AlertTitle>Error</AlertTitle>
                                            {message}
                                        </Alert>
                                        <Divider />
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