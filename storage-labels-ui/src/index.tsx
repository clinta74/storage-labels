import React from "react";
import { createRoot } from "react-dom/client";
import { App } from "./app/app";
import { registerBackgroundWorker } from './register-background-worker';

const rootNode = document.getElementById("root");
if (rootNode !== null) {
    const root = createRoot(rootNode);

    root.render(
        <React.Fragment>
            <React.StrictMode>
                <App />
            </React.StrictMode>
        </React.Fragment>
    );
}

// registerBackgroundWorker();