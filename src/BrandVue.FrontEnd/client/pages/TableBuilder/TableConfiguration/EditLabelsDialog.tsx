import { useState } from "react";
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Typography,
    TextField,
    Divider,
    Button,
    Stack,
    Box,
    MenuItem
} from "@mui/material";
import { TableItem, TableItemInstances } from "../TableBuilderTypes";
import { copyTableItem } from "../TableBuilderUtils";
import DoneOutlinedIcon from '@mui/icons-material/DoneOutlined';
import { IEntityType } from "../../../BrandVueApi";
import EntityTypeSelector from "./EntityTypeSelector";

interface Props {
    tableItem: TableItem;
    updateItem: (newItem: TableItem) => void;
    open: boolean;
    onClose: () => void;
}

const EditLabelsDialog = (props: Props) => {
    const [editingItem, setEditingItem] = useState<TableItem>(copyTableItem(props.tableItem));
    const [selectedEntityType, setSelectedEntityType] = useState<IEntityType>(props.tableItem.primaryInstances.entityType);

    const allEntities = [editingItem.primaryInstances, ...editingItem.filterInstances]
        .sort((a, b) => a.entityType.displayNameSingular.localeCompare(b.entityType.displayNameSingular));
    const currentEntities = allEntities.find(et => et.entityType.identifier === selectedEntityType.identifier)!;

    const onDone = () => {
        props.updateItem(editingItem);
        props.onClose();
    };

    const updateQuestionLabel = (newLabel: string) => {
        setEditingItem(prevItem => ({
            ...prevItem,
            label: newLabel
        }));
    };

    const updateEntity = (newEntity: TableItemInstances) => {
        const isPrimary = newEntity.entityType.identifier === editingItem.primaryInstances.entityType.identifier;
        if (isPrimary) {
            setEditingItem(prevItem => ({
                ...prevItem,
                primaryInstances: newEntity
            }));
        } else {
            setEditingItem(prevItem => ({
                ...prevItem,
                filterInstances: prevItem.filterInstances.map(fi =>
                    fi.entityType.identifier === newEntity.entityType.identifier ? newEntity : fi
                )
            }));
        }
    };

    const updateInstanceLabel = (index: number, newLabel: string) => {
        updateEntity({
            ...currentEntities,
            instances: currentEntities.instances.map((instance, idx) => ({
                ...instance,
                label: idx === index ? newLabel : instance.label
            }))
        });
    };

    const handleEntityTypeChange = (entityTypeIdentifier: string) => {
        const newEntityType = allEntities.find(et => et.entityType.identifier === entityTypeIdentifier)?.entityType;
        if (newEntityType) {
            setSelectedEntityType(newEntityType);
        }
    };

    return (
        <Dialog open={props.open} onClose={props.onClose} maxWidth="sm" fullWidth>
            <DialogTitle>Edit Labels</DialogTitle>
            <DialogContent>
                <Stack direction="column" spacing={2}>
                    <Typography variant="subtitle2">
                        Customize the display labels for this question and its answer options to better suit your table presentation.
                    </Typography>
                    <Stack direction="column" spacing={1}>
                        <Typography variant="subtitle2">Question Label</Typography>
                        <TextField
                            fullWidth
                            size="small"
                            value={editingItem.label}
                            onChange={e => updateQuestionLabel(e.target.value)}
                            placeholder="Enter custom question label..."
                        />
                        <Typography variant="caption" color="text.secondary">
                            Original: {editingItem.metric.helpText}
                        </Typography>
                    </Stack>
                    <Divider />
                    {allEntities.length > 1 &&
                        <EntityTypeSelector
                            selectedEntityType={selectedEntityType}
                            allEntities={allEntities}
                            handleEntityTypeChange={handleEntityTypeChange} />
                    }
                    <Stack direction="column" spacing={2}>
                        <Box>
                            <Typography variant="subtitle2">Option Labels</Typography>
                            <Typography variant="caption" color="text.secondary">
                                Customize the labels for each answer option.
                            </Typography>
                        </Box>
                        {currentEntities.instances.map((instance, idx) => (
                            <TextField
                                key={instance.originalIndex}
                                fullWidth
                                label={`${instance.isNet ? 'Net: ' : ''}${instance.originalLabel}`}
                                value={instance.label}
                                onChange={e => updateInstanceLabel(idx, e.target.value)}
                                placeholder="Enter custom option label..."
                            />
                        ))}
                    </Stack>
                </Stack>
            </DialogContent>
            <DialogActions>
                <Button onClick={props.onClose}>Cancel</Button>
                <Button onClick={onDone} variant="contained" color="primary">
                    <DoneOutlinedIcon /> Save Labels
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default EditLabelsDialog;