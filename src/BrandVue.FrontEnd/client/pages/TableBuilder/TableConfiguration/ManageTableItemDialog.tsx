import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import Button from "@mui/material/Button";
import { TableItem, TableItemInstance, TableItemInstances } from "../TableBuilderTypes";
import { useState } from "react";
import { copyTableItem } from "../TableBuilderUtils";
import Checkbox from "@mui/material/Checkbox";
import IconButton from "@mui/material/IconButton";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import DragIndicatorIcon from "@mui/icons-material/DragIndicator";
import { DragDropContext, Droppable, Draggable, DropResult } from "react-beautiful-dnd";
import AddIcon from '@mui/icons-material/Add';
import AddNetDialog from "./AddNetDialog";
import LayersOutlinedIcon from '@mui/icons-material/LayersOutlined';
import CloseIcon from '@mui/icons-material/Close';
import { FormControlLabel, Switch, Tooltip } from "@mui/material";
import { IEntityType } from "../../../BrandVueApi";
import EntityTypeSelector from "./EntityTypeSelector";

interface ManageTableItemDialogProps {
    tableItem: TableItem;
    updateItem: (newItem: TableItem) => void;
    open: boolean;
    onClose: () => void;
}

const ManageTableItemDialog = (props: ManageTableItemDialogProps) => {
    const [editingItem, setEditingItem] = useState<TableItem>(copyTableItem(props.tableItem));
    const [selectedEntityType, setSelectedEntityType] = useState<IEntityType>(props.tableItem.primaryInstances.entityType);
    const [addNetDialogOpen, setAddNetDialogOpen] = useState<boolean>(false);

    const allEntities = [editingItem.primaryInstances, ...editingItem.filterInstances]
        .sort((a, b) => a.entityType.displayNameSingular.localeCompare(b.entityType.displayNameSingular));
    const isSelectedPrimaryEntityType = selectedEntityType.identifier === editingItem.primaryInstances.entityType.identifier;
    const currentEntities = allEntities.find(et => et.entityType.identifier === selectedEntityType.identifier)!;
    const nettableTableItemInstances = currentEntities.instances.filter(i => !i.isNet && i.instanceIds.length === 1);
    const hasInsufficientNettableInstances = nettableTableItemInstances.length < 2;

    const selectNewPrimaryEntityType = () => {
        if (!isSelectedPrimaryEntityType) {
            setEditingItem(prevItem => {
                const newPrimary = prevItem.filterInstances.find(fi => fi.entityType.identifier === selectedEntityType.identifier);
                const newFilterInstances = [
                    ...prevItem.filterInstances.filter(fi => fi.entityType.identifier !== selectedEntityType.identifier),
                    prevItem.primaryInstances
                ];
                return {
                    ...prevItem,
                    primaryInstances: newPrimary!,
                    filterInstances: newFilterInstances
                };
            });
        }
    };

    const onDone = () => {
        props.updateItem(editingItem);
        props.onClose();
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

    const handleCheckboxChange = (idx: number) => {
        updateEntity({
            ...currentEntities,
            instances: currentEntities.instances.map((instance, index) => ({
                ...instance,
                enabled: index === idx ? !instance.enabled : instance.enabled
            }))
        });
    };

    const setAllEnabled = (enabled: boolean) => {
        updateEntity({
            ...currentEntities,
            instances: currentEntities.instances.map(instance => ({
                ...instance,
                enabled
            }))
        });
    };

    const reorderInstance = (startIndex: number, endIndex: number) => {
        if (endIndex >= 0 && endIndex < currentEntities.instances.length) {
            const newInstances = Array.from(currentEntities.instances);
            const [removed] = newInstances.splice(startIndex, 1);
            newInstances.splice(endIndex, 0, removed);
            updateEntity({
                ...currentEntities,
                instances: newInstances
            });
        }
    };

    const handleMoveUp = (idx: number) => reorderInstance(idx, idx - 1);
    const handleMoveDown = (idx: number) => reorderInstance(idx, idx + 1);
    const handleDragEnd = (result: DropResult)  => {
        if (result.destination) {
            reorderInstance(result.source.index, result.destination.index);
        }
    };

    const addNet = (label: string, instanceIds: number[]) => {
        const maxOriginalIndex = Math.max(0, ...currentEntities.instances.map(i => i.originalIndex));
        const newNet: TableItemInstance = {
            instanceIds: instanceIds,
            label: label,
            originalLabel: label,
            enabled: true,
            isNet: true,
            originalIndex: maxOriginalIndex + 1
        };
        updateEntity({
            ...currentEntities,
            instances: [...currentEntities.instances, newNet]
        });
    };

    const removeNet = (index: number) => {
        const netToRemove = currentEntities.instances[index];
        if (netToRemove.isNet) {
            updateEntity({
                ...currentEntities,
                instances: currentEntities.instances.filter((_, idx) => idx !== index)
            });
        }
    };

    const handleEntityTypeChange = (entityTypeIdentifier: string) => {
        const newEntityType = allEntities.find(et => et.entityType.identifier === entityTypeIdentifier)?.entityType;
        if (newEntityType) {
            setSelectedEntityType(newEntityType);
        }
    };

    const getAddNetButtonTooltip = () => {
        if (!isSelectedPrimaryEntityType) {
            return "Nets can only be created for the primary choice set.";
        } else if (hasInsufficientNettableInstances) {
            return "At least two options are required to create a net.";
        }
        return null;
    }

    return (
        <Dialog open={props.open} onClose={props.onClose} maxWidth="sm" fullWidth>
            {addNetDialogOpen &&
                <AddNetDialog open={addNetDialogOpen}
                    onClose={() => setAddNetDialogOpen(false)}
                    nettableTableItemInstances={nettableTableItemInstances}
                    addNet={addNet} />
            }
            <DialogTitle>
                Manage Options
            </DialogTitle>
            <DialogContent>
                <Stack spacing={1}>
                    <Typography variant="subtitle2">
                        Select options, set display order, and create nets for this question.
                    </Typography>
                    <Typography variant="body2">
                        Question: {editingItem.metric.helpText}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                        Select options to include in your table and drag to reorder their display. Only selected options will appear in your table, in the order shown below.
                    </Typography>
                    {allEntities.length > 1 &&
                        <>
                            <EntityTypeSelector
                                selectedEntityType={selectedEntityType}
                                allEntities={allEntities}
                                handleEntityTypeChange={handleEntityTypeChange} />
                            <FormControlLabel
                                label={"Use as primary choice set (table rows)"}
                                control={
                                    <Switch
                                        checked={isSelectedPrimaryEntityType}
                                        disabled={isSelectedPrimaryEntityType}
                                        onChange={() => selectNewPrimaryEntityType()}
                                    />
                                }
                            />
                        </>
                    }
                    <Stack direction="row" spacing={1}>
                        <Button variant="outlined" size="small" onClick={() => setAllEnabled(true)}>Select All</Button>
                        <Button variant="outlined" size="small" onClick={() => setAllEnabled(false)}>Deselect All</Button>
                        <Tooltip title={getAddNetButtonTooltip()} arrow>
                            <span> {/*non-disabled wrapper is needed for tooltip to work on disabled button*/}
                            <Button variant="contained" size="small" onClick={() => setAddNetDialogOpen(true)} disabled={!isSelectedPrimaryEntityType || hasInsufficientNettableInstances}>
                                <AddIcon /> Create Net
                            </Button>
                            </span>
                        </Tooltip>
                    </Stack>
                    <DragDropContext onDragEnd={handleDragEnd}>
                        <Droppable droppableId="instances-droppable">
                            {(provided) => (
                                <div ref={provided.innerRef} {...provided.droppableProps}>
                                    <Stack spacing={1}>
                                        {currentEntities.instances.map((instance, idx) => (
                                            <DraggableInstance
                                                key={instance.originalIndex}
                                                allInstances={currentEntities}
                                                instance={instance}
                                                index={idx}
                                                onCheckboxChange={handleCheckboxChange}
                                                onMoveUp={handleMoveUp}
                                                onMoveDown={handleMoveDown}
                                                removeNet={removeNet}
                                            />
                                        ))}
                                        {provided.placeholder}
                                    </Stack>
                                </div>
                            )}
                        </Droppable>
                    </DragDropContext>
                    <Stack direction="row" justifyContent="space-between" alignItems="center">
                        <Typography variant="body2" color="text.secondary">
                            Selected: {currentEntities.instances.filter(i => i.enabled).length} of {currentEntities.instances.length} options
                        </Typography>
                        {currentEntities.instances.some(i => i.isNet) &&
                            <Typography variant="body2" color="text.secondary">
                                Nets created: {currentEntities.instances.filter(i => i.isNet).length}
                            </Typography>
                        }
                    </Stack>
                </Stack>
            </DialogContent>
            <DialogActions>
                <Button onClick={props.onClose}>Cancel</Button>
                <Button onClick={onDone} variant="contained">Done</Button>
            </DialogActions>
        </Dialog>
    );
};

interface DraggableInstanceProps {
    instance: TableItemInstance;
    allInstances: TableItemInstances;
    index: number;
    onCheckboxChange: (idx: number) => void;
    onMoveUp: (idx: number) => void;
    onMoveDown: (idx: number) => void;
    removeNet: (idx: number) => void;
}

const DraggableInstance = (props: DraggableInstanceProps) => {
    const totalInstances = props.allInstances.instances.length;

    const getLabelDisplay = () => {
        const textLabel = (
            <Typography
                variant="body2"
                color={props.instance.enabled ? undefined : "text.secondary"}
                title={props.instance.label}
                sx={{
                    flex: 1,
                    minWidth: 0,
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                }}
            >
                {props.instance.label}
            </Typography>
        );

        if (props.instance.isNet) {
            const instanceNames = props.instance.instanceIds.map(id => {
                const matched = props.allInstances.instances.find(i =>
                    !i.isNet &&
                    i.instanceIds.length === 1 &&
                    i.instanceIds[0] === id
                );
                return matched?.label ?? "-";
            });
            return (
                <Stack direction="column">
                    <Stack direction="row" spacing={0.5}>
                        <LayersOutlinedIcon sx={{ color: props.instance.enabled ? "#1376CD" : '#999' }} />
                        <Typography variant="body2" color="text.secondary">Net:</Typography>
                        {textLabel}
                    </Stack>
                    <Typography variant="body2" color="text.secondary" sx={{
                        flex: 1,
                        minWidth: 0,
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                    }}>
                        Includes: {instanceNames.join(", ")}
                    </Typography>
                </Stack>
            )
        }
        return textLabel;
    };

    return (
        <Draggable draggableId={props.instance.originalIndex.toString()} index={props.index}>
            {(provided) => (
                <Stack
                    direction="row"
                    alignItems="center"
                    spacing={1}
                    ref={provided.innerRef}
                    {...provided.draggableProps}
                    sx={{
                        background: props.instance.enabled ? "#eee" : "white",
                        border: props.instance.enabled ? "1px solid transparent" : "1px solid #ddd",
                        justifyContent: 'space-between',
                        ...provided.draggableProps.style
                    }}
                >
                    <Checkbox
                        checked={props.instance.enabled}
                        onChange={() => props.onCheckboxChange(props.index)}
                    />
                    <Stack
                        spacing={1}
                        direction="row"
                        alignSelf="stretch"
                        alignItems="center"
                        sx={{ flexGrow: 1, minWidth: 0, cursor: 'pointer' }}
                        onClick={() => props.onCheckboxChange(props.index)}
                    >
                        {getLabelDisplay()}
                    </Stack>
                    <Stack direction="row" sx={{ p: 1 }}>
                        {props.instance.isNet && (
                            <Tooltip title="Remove Net">
                                <IconButton size="small" onClick={() => props.removeNet(props.index)}>
                                    <CloseIcon fontSize="small" />
                                </IconButton>
                            </Tooltip>
                        )}
                        <IconButton size="small" {...provided.dragHandleProps}>
                            <DragIndicatorIcon />
                        </IconButton>
                        <IconButton
                            size="small"
                            onClick={() => props.onMoveUp(props.index)}
                            disabled={props.index === 0}
                        >
                            <ArrowUpwardIcon />
                        </IconButton>
                        <IconButton
                            size="small"
                            onClick={() => props.onMoveDown(props.index)}
                            disabled={props.index === totalInstances - 1}
                        >
                            <ArrowDownwardIcon />
                        </IconButton>
                    </Stack>
                </Stack>
            )}
        </Draggable>
    );
};

export default ManageTableItemDialog;