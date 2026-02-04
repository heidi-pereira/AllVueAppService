import Stack from "@mui/material/Stack";
import ToggleButton from '@mui/material/ToggleButton';
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup';
import Button from '@mui/material/Button';
import TableChartIcon from '@mui/icons-material/TableChart';
import BarChartIcon from '@mui/icons-material/BarChart';
import PostAddIcon from '@mui/icons-material/PostAdd';
import { VisualisationMode } from "./TableBuilderVisualisation";


const VisualisationControlsHeader = (props: {
    mode: VisualisationMode;
    onModeChange: (mode: VisualisationMode) => void;
}) => {
    return (
        <Stack direction="row" justifyContent="flex-end" alignItems="center" spacing={2} sx={{ width: '100%' }}>
            <ToggleButtonGroup
                value={props.mode}
                exclusive
                onChange={(_, newMode) => {
                    if (newMode !== null) props.onModeChange(newMode);
                }}
                size="small"
            >
                <ToggleButton value={VisualisationMode.Table}>
                    <TableChartIcon sx={{ mr: 1 }} />
                    Table
                </ToggleButton>
                <ToggleButton value={VisualisationMode.Chart}>
                    <BarChartIcon sx={{ mr: 1 }} />
                    Chart
                </ToggleButton>
            </ToggleButtonGroup>
            <Button
                variant="outlined"
                startIcon={<PostAddIcon />}
            >
                Add to report...
            </Button>
        </Stack>
    );
};
export default VisualisationControlsHeader;