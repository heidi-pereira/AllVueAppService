import React from "react";
import {ModalContent} from "../VariableContentModal";
import {VariableDefinitionCreationService} from "../Utils/VariableDefinitionCreationService";
import {VariableDefinition} from "../../../../../BrandVueApi";


interface ISysAdminVariableModalContentProps {
    isBase?: boolean;
    setContent: (content: ModalContent) => void;
    variableDefinitionCreationService: VariableDefinitionCreationService;
    setVariableDefinition: (variableDefinition: VariableDefinition) => void;
}

const SysAdminVariableModalContent = (props: ISysAdminVariableModalContentProps) => {
    const description = props.isBase ?
        "Add conditions to select the custom base you want to use (eg Generation X, Millennials, etc)" :
        "Add groups to select the responses you want to use (eg Generation X, Millennials, etc)"
    
    const createGroupedVariableHandler = () => {
        props.setVariableDefinition(props.variableDefinitionCreationService.createGroupedVariableDefinition(props.isBase))
        props.setContent(ModalContent.Grouped)
    }
    
    const createFieldExpressionHandler = () => {
        props.setVariableDefinition(props.variableDefinitionCreationService.createFieldExpressionVariableDefinition(props.isBase))
        props.setContent(ModalContent.FieldExpression)
    }
    
    return (
        <div className="initial-stage">
            <div className="variable-page-label">
                {description}
            </div>
            <button id="add-group" className="hollow-button add-group-btn" onClick={createGroupedVariableHandler}>
                <i className="material-symbols-outlined">add</i>
                <div className="add-group-button-text">Add {props.isBase ? "condition" : "group"}</div>
            </button>
            <button className="hollow-button" onClick={createFieldExpressionHandler}>
                <i className="material-symbols-outlined">add</i>
                <div className="add-group-button-text">Add {props.isBase ? "base" : "field"} expression</div>
            </button>
        </div>
    );
}

export default SysAdminVariableModalContent