import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Typography,
    TextField,
    Button,
    Stack,
    Checkbox,
    FormGroup,
    FormControlLabel
} from "@mui/material";
import DoneOutlinedIcon from '@mui/icons-material/DoneOutlined';
import { useEffect, useState } from "react";
import { plural } from "../TableBuilderUtils";
import { TableItemInstance } from "../TableBuilderTypes";

interface Props {
    open: boolean;
    onClose: () => void;
    nettableTableItemInstances: TableItemInstance[];
    addNet: (label: string, instanceIds: number[]) => void;
}

const AddNetDialog = (props: Props) => {
    const [label, setLabel] = useState<string>("");
    const [selectedIds, setSelectedIds] = useState<number[]>([]);

    useEffect(() => {
        if (props.open) {
            setLabel("");
            setSelectedIds([]);
        }
    }, [props.open]);

    const onDone = () => {
        props.addNet(label, [...selectedIds].sort((a,b) => a-b));
        props.onClose();
    };

    const toggleInstanceId = (id: number) => {
        setSelectedIds(ids =>
            ids.includes(id)
                ? ids.filter(i => i !== id)
                : [...ids, id]
        );
    };

    return (
        <Dialog open={props.open} onClose={props.onClose} maxWidth="sm" fullWidth>
            <DialogTitle>Create New Net</DialogTitle>
            <DialogContent>
                <Stack direction="column" spacing={2}>
                    <Typography variant="subtitle2">
                        Group multiple options together under a single net name.
                    </Typography>
                    <Stack direction="column" spacing={1}>
                        <Typography variant="subtitle2">Net Name</Typography>
                        <TextField
                            fullWidth
                            size="small"
                            value={label}
                            onChange={e => setLabel(e.target.value)}
                            placeholder="e.g. Top 2 Box, Positive Responses"
                        />
                    </Stack>
                    <Stack direction="column" spacing={1}>
                        <Typography variant="subtitle2">Select Options To Include</Typography>
                        <FormGroup sx={{ p: 1, border: '1px solid #ddd' }}>
                            {props.nettableTableItemInstances.map((instance) => {
                                const singleInstanceId = instance.instanceIds[0];
                                return (
                                    <FormControlLabel
                                        key={singleInstanceId}
                                        label={instance.label}
                                        control={
                                            <Checkbox
                                                checked={selectedIds.includes(singleInstanceId)}
                                                onChange={() => toggleInstanceId(singleInstanceId)}
                                            />
                                        }
                                    />
                                );
                            })}
                        </FormGroup>
                        <Typography variant="body2" color="text.secondary">{selectedIds.length} {plural(selectedIds.length, "option", "options")} selected</Typography>
                    </Stack>
                </Stack>
            </DialogContent>
            <DialogActions>
                <Button onClick={props.onClose}>Cancel</Button>
                <Button onClick={onDone} variant="contained" color="primary" disabled={label.trim() === "" || selectedIds.length < 2}>
                    <DoneOutlinedIcon /> Create Net
                </Button>
            </DialogActions>
        </Dialog>
    );
};
export default AddNetDialog;