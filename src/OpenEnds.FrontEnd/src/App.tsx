import CssBaseline from '@mui/material/CssBaseline';
import 'dayjs/locale/en-gb';
import 'react-toastify/dist/ReactToastify.css';
import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import 'react-tooltip/dist/react-tooltip.css';
import { Tooltip } from 'react-tooltip';
import useGlobalDetailsStore from '@model/globalDetailsStore.ts';
import { ToastContainer } from 'react-toastify';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { ConfirmProvider, ConfirmOptions } from 'material-ui-confirm';
import HeaderContent from './Template/HeaderContent.tsx';
import Router from './Router/Router.tsx';
import './App.scss';
import { ThemeProvider } from '@mui/material';
import theme from './Theme/theme.ts';
import { useEffect } from 'react';
import { Provider } from 'react-redux';
import { store } from './store/store';

// AppContent lives under the Redux Provider so it can use store-connected hooks
const AppContent = () => {

    const confirmOptions: ConfirmOptions = {
        dialogActionsProps: { style: { justifyContent: 'center' } },
        titleProps: { style: { textAlign: 'center', fontSize: 'large' } },
        confirmationButtonProps: { style: { backgroundColor: '#d00', color: '#fff', fontWeight: 400 } },
        cancellationButtonProps: { style: { backgroundColor: '#eee', color: '#444', fontWeight: 400 } },
    };

    return (
        <ThemeProvider theme={theme}>
            <CssBaseline />
            <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale="en-gb">
                <ConfirmProvider defaultOptions={confirmOptions}>
                    <HeaderContent />
                    <Router />
                </ConfirmProvider>
            </LocalizationProvider>
            <Tooltip id='shared' delayShow={300} />
            <ToastContainer
                position="bottom-center"
                autoClose={5000}
                hideProgressBar={false}
                newestOnTop={false}
                closeOnClick
                rtl={false}
                pauseOnFocusLoss
                draggable
                pauseOnHover
                theme="colored"
            />
        </ThemeProvider>
    );
};

const App = () => {

    const { fetchGlobalDetails } = useGlobalDetailsStore();

    useEffect(() => { 
        fetchGlobalDetails();
    }, [fetchGlobalDetails])

    return (
        <>
            <Provider store={store}>
                <AppContent />
            </Provider>
        </>
    );
}

export default App;