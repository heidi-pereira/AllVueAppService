import { Box, styled } from '@mui/material';

const ThemeIconNameBox = styled(Box)(() => ({
    display: "flex",
    flexWrap: "nowrap",
    alignItems: "center",
    '& > svg': {
        fontSize: 'medium',
        marginRight: '0.25rem',
        marginBottom: '0.25rem'
    },
}));

export default ThemeIconNameBox;