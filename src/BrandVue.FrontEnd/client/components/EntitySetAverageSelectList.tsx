import React from 'react';
import { EntitySet, sortEntitySets } from "../entity/EntitySet";
import { IEntityType } from "../BrandVueApi";
import SearchInput from './SearchInput';

interface IEntitySetAverageSelectList {
    selectedEntitySet: EntitySet;
    availableEntitySetsForAverage: EntitySet[];
    addEntitySetToAverages(entitySet: EntitySet): void;
    entityType: IEntityType;
}

const EntitySetAverageSelectList = (props: IEntitySetAverageSelectList) => {
    const [searchText, setSearchText] = React.useState<string>("");

    const getEntitySetElement = (entitySet: EntitySet) => {
        const displayText = entitySet.name;
        return (
            <div key={entitySet.name} onClick={() => props.addEntitySetToAverages(entitySet)} title={entitySet.name} className="metric">
                <div className="name-container">
                    <span className='title' title={displayText}>{displayText}</span>
                </div>
            </div>
        );
    }

    const getEntitySetsForAverage = () => {
        const textToSearch = searchText.trim().toLowerCase();
        const matchedEntitySets = props.availableEntitySetsForAverage.filter(entity =>
            entity.name.toLowerCase().includes(textToSearch));

        if (matchedEntitySets.length === 0)
            return <div className="no-results">No results</div>;

        const currentSet = matchedEntitySets.filter(es => es.id === props.selectedEntitySet.id).sort(sortEntitySets);
        const groupSets = matchedEntitySets.filter(es => es.id !== props.selectedEntitySet.id && !es.isSectorSet).sort(sortEntitySets);
        const sectorSets = matchedEntitySets.filter(es => es.id !== props.selectedEntitySet.id && es.isSectorSet).sort(sortEntitySets);

        return (
            <div className="variables">
                {currentSet.length > 0 && <div className="metric title">CURRENT {props.entityType.displayNameSingular.toUpperCase()} GROUP</div>}
                {currentSet.map(entitySet => getEntitySetElement(entitySet))}
                {groupSets.length > 0 && <div className="metric title">MY {props.entityType.displayNameSingular.toUpperCase()} GROUPS</div>}
                {groupSets.map(entitySet => getEntitySetElement(entitySet))}
                {sectorSets.length > 0 && <div className="metric title">SECTOR GROUPS</div>}
                {sectorSets.map(entitySet => getEntitySetElement(entitySet))}
            </div>
        );
    }

    return (
        <div className="metric-select-list">
            <div className="search-container">
                <SearchInput id="search-input-component" onChange={(text) => setSearchText(text)} className="flat-search" text={searchText} />
            </div>
            <div className="valid-metrics">
                {getEntitySetsForAverage()}
            </div>
        </div>
    );
}

export default EntitySetAverageSelectList;