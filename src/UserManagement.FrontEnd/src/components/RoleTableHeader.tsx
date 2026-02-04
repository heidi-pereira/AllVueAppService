import React from 'react';
import { Box } from '@mui/material';
import PageTitle from './shared/PageTitle';
import { withBasePath } from '../urlHelper'; 

const RoleTableHeader: React.FC = () => {
    return (
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
            <PageTitle href={withBasePath('/')} title="Manage roles" />
        </Box>
    );
};

export default RoleTableHeader;
