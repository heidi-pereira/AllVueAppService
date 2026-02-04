import { Typography } from "@mui/material";
import Stack from "@mui/material/Stack";
import TipsAndUpdatesOutlinedIcon from '@mui/icons-material/TipsAndUpdatesOutlined';

const TableBuilderTip = (props: { text: string }) => {

    return (
        <Stack direction="row" spacing={1} alignItems="center"
            sx={{
                backgroundColor: '#e3f2fd',
                border: '1px solid #1976d2',
                borderRadius: 1,
                p: 1,
                color: '#1976d2'
            }}
        >
            <TipsAndUpdatesOutlinedIcon color="inherit" sx={{ fontSize: 20 }} />
            <Typography variant="body2">{props.text}</Typography>
        </Stack>
    );
};
export default TableBuilderTip;