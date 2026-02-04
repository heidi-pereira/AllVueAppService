import React from 'react';
import SearchInput from "./SearchInput";
import { EntityInstance, sortEntityInstances } from "../entity/EntityInstance";
import {IEntityType} from "../BrandVueApi";
import { MixPanel } from './mixpanel/MixPanel';

interface IEntityInstancesSelectList {
    availableEntityInstances: EntityInstance[];
    selectedEntityInstances: EntityInstance[];
    updateSelectedEntityInstances(selected: EntityInstance[]): void;
    entityInstanceIdentifierKey: string
    entityType: IEntityType
}

const EntityInstancesSelectList = (props: IEntityInstancesSelectList) => {
    const [searchText, setSearchText] = React.useState<string>("");

    const getEntityInstancesToShow = () => {
        const textToSearch = searchText.trim().toLowerCase();
        return props.availableEntityInstances.filter(entity =>
            entity.name.toLowerCase().includes(textToSearch)).sort(sortEntityInstances);
    }

    const entityInstancesToShow = getEntityInstancesToShow();

    const toggleEntity = (entityInstance: EntityInstance) => {
        isEntitySelected(entityInstance) 
            ? MixPanel.track("entitiesRemoved")
            : MixPanel.track("entitiesAdded");
        const index = props.selectedEntityInstances.findIndex(entity => entity.name === entityInstance.name);
        if (index >= 0) {
            const newEntityInstances = [...props.selectedEntityInstances];
            newEntityInstances.splice(index, 1);
            props.updateSelectedEntityInstances(newEntityInstances);
        } else {
            props.updateSelectedEntityInstances([
                ...props.selectedEntityInstances,
                entityInstance
            ]);
        }
    }

    const isEntitySelected = (entityInstance: EntityInstance) => {
        return props.selectedEntityInstances.some(entity => entity.name === entityInstance.name);
    }

    const noResults = entityInstancesToShow.length === 0;
    
    const numberOfSelected = () => {
        return props.selectedEntityInstances.filter(e => props.availableEntityInstances.some(ent => e.name === ent.name)).length
    }

    return (
        <div className="metric-select-list">
            <div className="search-container">
                <SearchInput id="search-input-component" onChange={(text) => setSearchText(text)} className="flat-search" text={searchText} />
            </div>
            <div className="entity-set-selector-buttons">
                <button className="modal-button secondary-button" onClick={() => {props.updateSelectedEntityInstances(props.availableEntityInstances)}}>Select All</button>
                <button className="modal-button secondary-button" onClick={() => {props.updateSelectedEntityInstances([])}}>Clear All</button>
                <div className="entity-set-selector-buttons-text"><b>{numberOfSelected()}</b> {numberOfSelected() === 1 ? props.entityType.displayNameSingular.toLocaleLowerCase() : props.entityType.displayNamePlural.toLocaleLowerCase()} selected</div>
            </div>
            <div className="valid-metrics">
                {entityInstancesToShow.length > 0 &&
                    <div className="variables">
                        {entityInstancesToShow.map((entity, i) => {
                            const key = props.entityInstanceIdentifierKey + "-entity-" + entity.id;
                            return <div className="metric" key={key}>
                                <input type="checkbox" className="checkbox" id={key} checked={isEntitySelected(entity)} onChange={() => toggleEntity(entity)} />
                                <label className="instance-label" htmlFor={key}>{entity.name}</label>
                            </div>
                        })}
                    </div>
                }
                {noResults &&
                    <div className="no-results">No results</div>
                }
            </div>
        </div>
    );
};

export default EntityInstancesSelectList;