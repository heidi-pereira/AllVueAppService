import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import { useRouteError } from "react-router-dom";

const RouterError = () => {
    const error = useRouteError() as Error;
    console.log(error);
    return <Box sx={{ p: 2 }}>
        <h2>Something went wrong.</h2>
        <Alert severity="error">Error: {error.message}</Alert>
    </Box>
}

export default RouterError;