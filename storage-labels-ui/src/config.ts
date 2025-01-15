export const CONFIG = {
    API_URL: process.env.API_URL,
}

export const AUTH0 = {
    domain: process.env.REACT_APP_AUTH0_DOMAIN || '',
    scope: 'openid token email',
    audience: process.env.REACT_APP_AUTH0_AUDIENCE || '',
    clientId: process.env.REACT_APP_AUTH0_CLIENT_ID || '',
}