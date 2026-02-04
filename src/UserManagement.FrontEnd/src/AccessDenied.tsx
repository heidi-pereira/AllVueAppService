import React from 'react';
import { Box, Typography } from '@mui/material';

export const AccessDenied: React.FC = () => (
    <Box
        display="flex"
        flexDirection="column"
        alignItems="center"
        justifyContent="center"
        textAlign="center"
        marginTop={20}
        px={2}
    >
        <Typography
            variant="h6"
            fontWeight={500}
            mb={3}>
            You do not have permission to view this page
        </Typography>

        <Typography variant="body1" fontWeight={500}>
            Speak to your administrator if you think this is a mistake.
        </Typography>
        
    </Box>
);
