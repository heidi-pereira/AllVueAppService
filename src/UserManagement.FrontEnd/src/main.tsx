import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import { ThemeProvider } from '@mui/material/styles';
import { InfoThemes } from './theme';
import { Toaster } from 'mui-sonner';

let res;
try {
    res = await fetch('/usermanagement/api/usercontext');
    if (res.ok) {
        const data = await res.json();
        ReactDOM.createRoot(document.getElementById('root')!).render(
            <ThemeProvider theme={InfoThemes}>
                <App userContext={data} />
                <Toaster />
            </ThemeProvider>
        );
    }
    else if (res.status === 401) {
        const redirectUrl = encodeURIComponent(window.location.href);
        console.log(`Login ${redirectUrl}`);
        window.location.href = `/usermanagement/login?redirectUrl=${redirectUrl}`;
    } else if (!res.ok) {
        console.error(`Unexpected response status: ${res.status}`);
    }
} catch (error) {
    console.error('Failed to fetch logged-in user data:', error);
}

