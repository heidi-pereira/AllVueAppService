import React from 'react';
import { Box, Button, Typography, Divider, Stack } from '@mui/material';
import GroupIcon from '@mui/icons-material/Group';
import { useNavigate } from 'react-router-dom';
import Add from '@mui/icons-material/Add';
interface EmptyUsersProps {
    companyId: string;
}
const EmptyUsers: React.FC<EmptyUsersProps> = ({ companyId }) => {
    const navigate = useNavigate();
    return (
        <Box
            display="flex"
            flexDirection="column"
            alignItems="center"
            justifyContent="center"
            textAlign="center"
            marginTop={20}
            px={2}
        >
            <GroupIcon color="primary" sx={{ fontSize: 40, mb: 2 }} />
            <Typography 
                variant="h6" 
                fontWeight={500} 
                mb={3}>
                Get started with access management
            </Typography>

            <Stack spacing={2} alignItems="center">
                <Typography variant="body1" fontWeight={500}>
                    1. Add users to your company
                </Typography>

                <Button 
                    startIcon={<Add />}
                    variant="contained" 
                    color="primary"
                    sx={{ textTransform: 'none' }}
                    onClick={() => navigate(`/users/add/${companyId}`)}>
                    Add user
                </Button>

                <Typography variant="body2" color="text.secondary">
                    You need at least one user before you can configure project access.
                </Typography>

                <Divider sx={{ width: '100%', my: 2 }} />

                <Typography variant="body1" fontWeight={500}>
                    2. Configure project access and data groups
                </Typography>
            </Stack>
        </Box>
    );
};

export default EmptyUsers;
