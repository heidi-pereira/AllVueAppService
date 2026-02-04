import { useState } from "react";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import EditIcon from "@mui/icons-material/Edit";
import CloseIcon from "@mui/icons-material/Close";
import ListIcon from "@mui/icons-material/List";
import { TableItem } from "../TableBuilderTypes";
import KeyboardArrowDownIcon from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowRightIcon from "@mui/icons-material/KeyboardArrowRight";
import { Tooltip } from "@mui/material";

interface ConfiguredTableItemProps {
    tableItem: TableItem;
    update: (newItem: TableItem) => void;
    manageOptions: () => void;
    manageLabels: () => void;
    remove: (item: TableItem) => void;
    disableMultiEntity?: boolean;
}

const ConfiguredTableItem = (props: ConfiguredTableItemProps) => {
    const [isExpanded, setIsExpanded] = useState(false);

    const enabledInstances = props.tableItem.primaryInstances.instances.filter(i => i.enabled);
    const isDisabled = props.disableMultiEntity && props.tableItem.metric.entityCombination.length > 1;

    return (
        <Stack direction="column">
            <Tooltip title={isDisabled ? "Multi-dimension questions cannot be used as breaks currently" : null} arrow>
                <Stack
                    direction="row"
                    alignItems="center"
                    spacing={1}
                    color={isDisabled ? 'text.secondary' : 'text.primary'}
                    sx={{ p: 1, background: "#ececf0", justifyContent: 'space-between' }}
                >
                    <IconButton
                        size="small"
                        onClick={() => setIsExpanded(exp => !exp)}
                        aria-label={isExpanded ? "Collapse" : "Expand"}
                        color="inherit"
                    >
                        {isExpanded ? <KeyboardArrowDownIcon /> : <KeyboardArrowRightIcon />}
                    </IconButton>
                    <Typography
                        variant="body2"
                        sx={{
                            width: "54px",
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap"
                        }}
                        title={props.tableItem.metric.displayName}
                    >
                        {props.tableItem.metric.displayName}
                    </Typography>
                    <Box sx={{ py: 0.5, px: 1, border: "1px solid #ddd", fontSize: '0.85rem' }}>
                        {enabledInstances.length}/{props.tableItem.primaryInstances.instances.length}
                    </Box>
                    <IconButton size="small" aria-label="List" onClick={props.manageOptions} color="inherit">
                        <ListIcon/> <Typography variant="body2" ml={1}>Nets</Typography>
                    </IconButton>
                    <IconButton size="small" aria-label="Edit" onClick={props.manageLabels} color="inherit">
                        <EditIcon />
                    </IconButton>
                    <IconButton size="small" aria-label="Delete" onClick={() => props.remove(props.tableItem)} color="inherit">
                        <CloseIcon />
                    </IconButton>
                </Stack>
            </Tooltip>
            {isExpanded && (
                <Stack direction="column" spacing={1} sx={{ p: 1, border: "1px solid #ececf0"}}>
                    <Typography variant="body2" color="text.secondary">
                        Included options:
                    </Typography>
                    <Box
                        sx={{
                            display: 'flex',
                            flexWrap: 'wrap',
                            gap: '8px 8px',
                        }}
                    >
                        {enabledInstances.map(instance => (
                            <Box
                                key={instance.originalIndex}
                                sx={{
                                    py: 0.5,
                                    px: 1,
                                    background: "#ddd",
                                    fontSize: '0.85rem',
                                    maxWidth: 120,
                                    overflow: "hidden",
                                    textOverflow: "ellipsis",
                                    whiteSpace: "nowrap",
                                    flex: "0 1 auto"
                                }}
                                title={instance.label}
                            >
                                {instance.label}
                            </Box>
                        ))}
                        {enabledInstances.length === 0 && (
                            <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic' }}>
                                No options selected
                            </Typography>
                        )}
                    </Box>
                </Stack>
            )}
        </Stack>
    );
};

export default ConfiguredTableItem;