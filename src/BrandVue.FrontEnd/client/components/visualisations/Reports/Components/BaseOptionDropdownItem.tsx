import React from "react";
import {OverlayTrigger, Tooltip} from "react-bootstrap";
import { DropdownItem } from 'reactstrap';
import { VariableConfigurationModel } from "../../../../BrandVueApi";
import {useState} from "react";
import {IVariableModalState} from "./BaseTypeDropdownMenu";

interface IBaseOptionDropdownItemProps {
    displayNameOverride?: string;
    variableConfig: VariableConfigurationModel
    selectBaseVariable: (id: number) => void;
    isDisabled: boolean;
    setVariableModalState: (variableModalState: IVariableModalState) => void;
    canCreateNewBase: boolean | undefined;
}

const BaseOptionDropdownItem = (props: IBaseOptionDropdownItemProps) => {
    const [isHover, setIsHover] = useState<boolean>(false)

    const copyBaseHandler = (e: React.MouseEvent, id: number) => {
        e.stopPropagation()
        props.setVariableModalState(
            {isVariableModalOpen: true,
                baseVariableIdToEdit: undefined,
                baseVariableIdToCopy: id})
    }

    const editBaseHandler = (e: React.MouseEvent, id: number) => {
        e.stopPropagation()
        props.setVariableModalState(
            {isVariableModalOpen: true,
                baseVariableIdToEdit: id,
                baseVariableIdToCopy: undefined})
    }

    const getCopyTooltip = (props, baseVariableId: number) => {
        return (
            <Tooltip id={`base-copy-tooltip-${baseVariableId}`} {...props}>
                Copy as new base
            </Tooltip>
        )
    }

    const getEditTooltip = (props, baseVariableId: number) => {
        return (
            <Tooltip id={`base-edit-tooltip-${baseVariableId}`} {...props}>
                Edit/Delete custom base
            </Tooltip>
        )
    }

    const getBaseActionButtons = (baseVariableId: number) => {
        return (
            <div className="base-action-buttons">
                <OverlayTrigger
                    placement="top"
                    delay={{show: 250, hide: 250}}
                    overlay={p => getCopyTooltip(p, baseVariableId)}
                    key={`base-copy-tooltip-${baseVariableId}`}
                >
                    <i className="material-symbols-outlined menu-icon" id="base-copy" onClick={(e) => copyBaseHandler(e, baseVariableId)}>content_copy</i>
                </OverlayTrigger>
                <OverlayTrigger
                    placement="top"
                    delay={{show: 250, hide: 250}}
                    overlay={p => getEditTooltip(p, baseVariableId)}
                    key={`base-edit-tooltip-${baseVariableId}`}
                >
                    <i className="material-symbols-outlined menu-icon" id="base-edit" onClick={(e) => editBaseHandler(e, baseVariableId)}>edit</i>
                </OverlayTrigger>
            </div>
        );
    }

    const getIncompatibleBaseTooltip = (props, baseVariableId: number) => {
        return (
            <Tooltip id={`base-tooltip-${baseVariableId}`} {...props}>
                This base is not compatible with the selected question.
            </Tooltip>
        )
    }

    const dropdownItemClickHandler = () => {
        if (!props.isDisabled){
            props.selectBaseVariable(props.variableConfig.id)
        }
    }

    const getDropdownItem = () => {
        return (
            <DropdownItem className={props.isDisabled ? "base-drop-down-item-disabled" : ""}
                          onClick={dropdownItemClickHandler}
                          key={props.variableConfig.id}
                          onMouseEnter={() => setIsHover(true)}
                          onMouseLeave={() => setIsHover(false)}
            >
                <div className={`base-type-name ${props.isDisabled ? "disabled" : ""}`}>
                    {props.displayNameOverride ?? props.variableConfig.displayName}
                </div>
                {(props.canCreateNewBase && isHover) && getBaseActionButtons(props.variableConfig.id)}
            </DropdownItem>
        );
    }

    if (props.isDisabled) {
        return (
            <OverlayTrigger
                placement="top"
                delay={{show: 250, hide: 250}}
                overlay={p => getIncompatibleBaseTooltip(p, props.variableConfig.id)}
                key={props.variableConfig.id}
            >
                {getDropdownItem()}
            </OverlayTrigger>
        );
    }
    return getDropdownItem();
}

export default BaseOptionDropdownItem;