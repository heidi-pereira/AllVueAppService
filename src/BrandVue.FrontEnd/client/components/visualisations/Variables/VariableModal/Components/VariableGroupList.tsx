import React from 'react';
import {
    DragDropContext,
    Droppable,
    Draggable,
    DropResult,
    DroppableProvided,
    DraggableProvided,
} from 'react-beautiful-dnd';
import {VariableGroupItem} from "./VariableGroupItem";
import {
    DateRangeVariableComponent,
    VariableGrouping
} from "../../../../../BrandVueApi";
import {duplicateComponent} from "../Utils/VariableComponentHelpers";
import { VariableGroupWithSample } from '../Content/GroupedVariableModalContent';
import toast from "react-hot-toast";

export interface IVariableGroupListProps {
    groups: VariableGroupWithSample[];
    setGroups: (groups: VariableGroupWithSample[]) => void;
    activeGroupId: number;
    setActiveGroupId: (activeIndex: number) => void;
    updateGroup: (group: VariableGroupWithSample) => void;
    groupThatIsEditing: number | undefined;
    flattenMultiEntity: boolean;
    setGroupThatIsEditing: (groupThatIsEditing: number | undefined) => void;
    addGroup: () => void;
}

export const VariableGroupList = (props: IVariableGroupListProps) => {
    const reorder = (startIndex: number, endIndex: number) => {
        const clonedGroups = props.groups.map(g => {return {group: new VariableGrouping(g.group), sample: g.sample}});
        const [removed] = clonedGroups.splice(startIndex, 1);
        clonedGroups.splice(endIndex, 0, removed);
        clonedGroups.forEach((g, index) => g.group.toEntityInstanceId = index + 1)
        return clonedGroups;
    };

    const reorderGroups = (sourceIndex: number, destinationIndex: number) => {
        const reorderedGroups: VariableGroupWithSample[] = reorder(
            sourceIndex,
            destinationIndex
        );

        props.setGroups(reorderedGroups);
    }

    const onDragEnd = (result: DropResult) => {
        // dropped outside the list
        if (!result.destination) {
            return;
        }

        reorderGroups(result.source.index, result.destination.index);
    }

    const generateCopyName = (original: VariableGrouping) => {
        if (original.component instanceof DateRangeVariableComponent) {
            const regexpSize = /([0-9]+)/;
            const match = original.toEntityInstanceName.match(regexpSize);
            if (match && match.length == 2) {
                let waveNumber = Number(match[1]);
                let nextName = "";
                do {
                    waveNumber++;
                    if (waveNumber > 10000) {
                        return original.toEntityInstanceName + " - Copy";
                    }
                    nextName = original.toEntityInstanceName.replace(match[1], waveNumber.toString());
                } while (props.groups.find(group => group.group.toEntityInstanceName == nextName) != undefined);
                return nextName;
            }
        }
        return original.toEntityInstanceName + " - Copy";
    }

    const copyGroup = (group: VariableGroupWithSample) => {
        if (props.groupThatIsEditing !== undefined){
            return
        }
        const newId = props.groups.length > 0 ? Math.max(...props.groups.map(g => g.group.toEntityInstanceId)) + 1 : 1;

        const newGroup: VariableGrouping = new VariableGrouping();
        newGroup.toEntityInstanceId = newId;
        newGroup.toEntityInstanceName = generateCopyName(group.group);
        const clonedComponent = duplicateComponent(group.group.component);
        if (clonedComponent){
            newGroup.component = clonedComponent
        }
        const groupsClone = [...props.groups]
        groupsClone.push({
            group: newGroup,
            sample: group.sample
        });
        props.setGroups(groupsClone)
        props.setGroupThatIsEditing(newId)
        props.setActiveGroupId(newId)
    }

    const deleteGroup = (group: VariableGrouping) => {
        if (props.groupThatIsEditing !== undefined){
            return
        }
        if (props.groups.length > 1) {
            const groupsClone = [...props.groups]
            const indexOfGroupToRemove = groupsClone.findIndex(g => g.group.toEntityInstanceId === group.toEntityInstanceId)
            const indexOfActiveGroup = groupsClone.findIndex(g => g.group.toEntityInstanceId === props.activeGroupId)
            if (indexOfGroupToRemove === indexOfActiveGroup){
                if (indexOfGroupToRemove === groupsClone.length - 1){
                    props.setActiveGroupId(groupsClone[indexOfGroupToRemove - 1].group.toEntityInstanceId)
                } else {
                    props.setActiveGroupId(groupsClone[indexOfGroupToRemove + 1].group.toEntityInstanceId)
                }
            }
            const updatedGroups = groupsClone.filter(g => g.group.toEntityInstanceId !== group.toEntityInstanceId)
            props.setGroups(updatedGroups)
        } else {
            toast.error("Cannot delete only group");
        }
    }

    const updateGroup = (originalGroup: VariableGroupWithSample, updatedGroup: VariableGrouping) => {
        props.updateGroup({
            group: updatedGroup,
            sample: originalGroup.sample
        });
    }

    const getGroups = () => {
        if (!props.groups || props.groups.length === 0) {
            return null;
        }

        return (
            <DragDropContext onDragEnd={onDragEnd} onBeforeDragStart={() => props.setGroupThatIsEditing(undefined)}>
                <Droppable droppableId="variableModal">
                    {(droppableProvided: DroppableProvided) => (
                        <div className="group-list" {...droppableProvided.droppableProps} ref={droppableProvided.innerRef}>
                            {props.groups.map((g , index) => {
                                const key = g.group.toEntityInstanceName + g.group.toEntityInstanceId.toString();
                                return <Draggable key={key} draggableId={key} index={index}>
                                    {(draggableProvided: DraggableProvided) => (
                                        <div {...draggableProvided.draggableProps} ref={draggableProvided.innerRef}>
                                            <VariableGroupItem
                                                group={g.group}
                                                allGroups={props.groups.map(gr => gr.group)}
                                                updateGroup={(updatedGroup) => updateGroup(g, updatedGroup)}
                                                activeGroupIndex={props.activeGroupId}
                                                setActiveGroupIndex={props.setActiveGroupId}
                                                copyGroup={() => copyGroup(g)}
                                                deleteGroup={() => deleteGroup(g.group)}
                                                dragHandleProps={draggableProvided.dragHandleProps}
                                                groupThatIsEditing={props.groupThatIsEditing}
                                                setGroupThatIsEditing={props.setGroupThatIsEditing}
                                            />
                                        </div>
                                    )}
                                </Draggable>
                            })}
                            {droppableProvided.placeholder}
                        </div>
                    )}
                </Droppable>
            </DragDropContext>
        )
    }

    return (
        <div className="groups-container">
            <div className="groups">
                <div className="variable-page-label">
                    {props.flattenMultiEntity ? "Variables" : "Groups"}
                </div>
                <div className="button">
                    <button id="add-group" className="hollow-button add-group" onClick={() => props.addGroup()}>
                        <i className="material-symbols-outlined">add</i>
                        <div className="add-group-button-text">{props.flattenMultiEntity ? "Add variable" : "Add group"}</div>
                    </button>
                </div>
            </div>
            {getGroups()}
        </div>
    )
}
