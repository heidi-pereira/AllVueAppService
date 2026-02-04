import React from 'react';
import { DraggableProvidedDragHandleProps } from 'react-beautiful-dnd';
import toast from "react-hot-toast";
import {getIVariableGroupingNameErrorMessage} from "../Utils/VariableValidation";
import {VariableGrouping} from "../../../../../BrandVueApi";

export interface IVariableGroupItemProps {
    group: VariableGrouping;
    allGroups: VariableGrouping[]
    updateGroup: (group: VariableGrouping) => void;
    activeGroupIndex: number;
    setActiveGroupIndex: (activeGroupIndex: number) => void;
    copyGroup: () => void;
    deleteGroup: () => void;
    dragHandleProps?: DraggableProvidedDragHandleProps | null;
    groupThatIsEditing: number | undefined;
    setGroupThatIsEditing: (groupThatIsEditing: number | undefined) => void;
}

export const VariableGroupItem = (props: IVariableGroupItemProps) => {
    const [name, setName] = React.useState(props.group.toEntityInstanceName);
    const [hasError, setHasError] = React.useState(false);

    const getGroupItemClassName = `group-item ${props.group.toEntityInstanceId === props.activeGroupIndex ? "selected" : ""}`
    const isEditing = props.groupThatIsEditing === undefined;

    const handleFocus = (event) => event.target.select();

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') {
            saveChanges();
        }

        if (e.key === 'Escape') {
            setName(props.group.toEntityInstanceName)
            setHasError(false)
            props.setGroupThatIsEditing(undefined)
        }
    };

    const validateName = (newName: string) => {
        const errorMessage = getIVariableGroupingNameErrorMessage(newName, props.group, props.allGroups)
        if (errorMessage) {
            toast.error(errorMessage);
            return false;
        }

        toast.dismiss();
        return true;
    }

    const saveChanges = () => {
        if (validateName(name)) {
            props.setGroupThatIsEditing(undefined);
            const clonedGroup: VariableGrouping = new VariableGrouping({...props.group})
            clonedGroup.toEntityInstanceName = name
            props.updateGroup(clonedGroup);
            setHasError(false);
        } else {
            setHasError(true);
        }
    }

    const editText = () => {
        if (isEditing) {
            props.setGroupThatIsEditing(props.group.toEntityInstanceId);
        }
    }

    const getButtons = () => {
        return(
            <div className="hover-content">
                <i className="material-symbols-outlined" title={"Duplicate"} onClick={props.copyGroup}>content_copy</i>
                <i className="material-symbols-outlined" title={"Rename"} onClick={editText}>edit</i>
                <i className="material-symbols-outlined" title={"Delete"} onClick={props.deleteGroup}>close</i>
            </div>
        )
    }

    const updateActiveGroup = (id: number, e: React.MouseEvent) => {
        if (isEditing) {
            if (e.target instanceof Element && e.target.tagName === "I") {
                //If we're clicking an icon we want to do that specific action, not set the active group.
                return;
            }

            props.setActiveGroupIndex(id);
        }
    }

    if (!(props.groupThatIsEditing === props.group.toEntityInstanceId)) {
            return (
                <div key={props.group.toEntityInstanceId} className={getGroupItemClassName} onClick={(e)=> updateActiveGroup(props.group.toEntityInstanceId, e)}>
                    <div className="group-name-container" title={name}>
                        <i className="material-symbols-outlined" {...props.dragHandleProps}>drag_indicator</i>
                        <div className="group-name">{name}</div>
                    </div>
                    {getButtons()}
                </div>
            )
        }

    return (
        <div key={props.group.toEntityInstanceId} className="group-item-editing" {...props.dragHandleProps}>
            <input type="text"
                   className={`base-input ${hasError ? 'error' : ''}`}
                   value={name}
                   onChange={(e) => setName(e.target.value)}
                   onKeyDown={handleKeyDown}
                   onBlur={() => saveChanges()}
                   autoFocus={true}
                   autoComplete="off"
                   onFocus={handleFocus}
            />
        </div>
    )
}