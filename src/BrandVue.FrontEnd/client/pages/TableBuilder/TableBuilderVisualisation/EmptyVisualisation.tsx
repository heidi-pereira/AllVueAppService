import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined';
import Box from '@mui/material/Box';

const EmptyVisualisation= () => {
    return (
        <Stack
            direction="column"
            spacing={2}
            sx={{ border: '1px solid #ddd', p: 4, alignItems: 'center' }}
        >
            <Box
                sx={{
                    p: 2,
                    background: "#ececf0",
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center'
                }}
            >
                <SettingsOutlinedIcon sx={{ color: '#717182', fontSize: 40 }} />
            </Box>
            <Stack direction="column" sx={{ alignItems: 'center' }}>
                <Typography variant="h6" sx={{ fontWeight: 600 }}>
                    Start Building Your Table
                </Typography>
                <Typography variant="body2" color="text.secondary" align="center">
                    Select questions from the sidebar to define your table rows and columns
                </Typography>
            </Stack>
        </Stack>
    );
}
export default EmptyVisualisation;