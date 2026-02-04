import React from 'react';
import { Box, TextField, Typography } from '@mui/material';

interface NameEditorProps {
    displayName: string;
    setDisplayName: (name: string) => void;
}

const NameEditor: React.FC<NameEditorProps> = ({ displayName, setDisplayName }) => {
    return (
        <Box>
            <Box mb={1}>
                <Typography fontWeight="500">Display name</Typography>
                <Typography variant='info'>Changing this won't affect the analysis</Typography>
            </Box>
            <TextField
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                variant="outlined"
                required={true}
                slotProps={{
                    htmlInput: { maxLength: 100 }
                }}
                size='small'
                sx={{ width: '500px' }}
            />
        </Box>
    );
};

export default NameEditor;
