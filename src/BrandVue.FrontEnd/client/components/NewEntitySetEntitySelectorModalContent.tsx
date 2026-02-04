import React from "react";
import {IModalPage} from "./ModalPage";
import EntityInstancesSelectList from "./EntityInstancesSelectList";
import {EntityInstance} from "../entity/EntityInstance";
import {IEntityType} from "../BrandVueApi";

interface INewEntitySetEntitySelectorModalContent extends IModalPage{
    entitySetType: IEntityType
    availableInstances: EntityInstance[];
    selectedEntityInstances: EntityInstance[];
    setSelectedEntityInstances: (entities: EntityInstance[]) => void;
}

const NewEntitySetEntitySelectorModalContent = (props: INewEntitySetEntitySelectorModalContent) => {

    const selectEntityInstanceHandler = (entities: EntityInstance[]) => {
        props.setSelectedEntityInstances(entities);
        setActionButtonIsDisabledOnEmptyInstanceArray(entities);
    }

    const setActionButtonIsDisabledOnEmptyInstanceArray = (entityInstances: EntityInstance[]) => {
        if (props.setActionButtonIsDisabled) {
            if (entityInstances.length === 0) {
                props.setActionButtonIsDisabled(true)
                return;
            }
            props.setActionButtonIsDisabled(false)
        }
    }

    setActionButtonIsDisabledOnEmptyInstanceArray(props.selectedEntityInstances);
    
    return (
        < div className={`new-entity-set new-entity-set-entities-selector ${props.className ? props.className : ""}`} >
            <EntityInstancesSelectList
                availableEntityInstances={props.availableInstances}
                selectedEntityInstances={props.selectedEntityInstances}
                updateSelectedEntityInstances={selectEntityInstanceHandler}
                entityInstanceIdentifierKey={"new-entity-set-"}
                entityType={props.entitySetType}
            />
        </div>
    );
}

export default NewEntitySetEntitySelectorModalContent