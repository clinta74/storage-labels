export const registerBackgroundWorker = () => {
    if ('serviceWorker' in navigator) {
        // Override the default scope of '/' with './', so that the registration applies
        // to the current directory and everything underneath it.
        const url = new URL('static/service-worker.js', import.meta.url)
        navigator.serviceWorker.register(url, { scope: '/' })
            .then(() => {
                console.log('Service Worker registered successfully.');
            }).catch(error => {
                // Something went wrong during registration. The service-worker.js file
                // might be unavailable or contain a syntax error.
                console.log('Service Worker registration failed:', error);
            });
    } else {
        // The current browser doesn't support service workers.
        console.log('unavailable see: http://www.chromium.org/blink/serviceworker/service-worker-faq');
    }
}