import {EntityInstance} from "../entity/EntityInstance";
import {EntitySet} from "../entity/EntitySet";
import React from "react";
import EntitySetListItem from "./EntitySetListItem";
import { EntitySetAverage } from "../entity/EntitySetAverage";

interface IEntitySetList {
    entitySet: EntitySet;
    getEntitySetName: (entitySetId: number) => string;
    removeEntityInstance: (entity: EntityInstance) => void;
    setEntityInstanceAsActive: (entity: EntityInstance) => void;
    removeAverage: (average: EntitySetAverage) => void;
}

const EntitySetList = (props: IEntitySetList) => {
    const entitySetInstances = props.entitySet.getInstances().getAll();
    const isLastEntityInstance = entitySetInstances.length === 1;

    const getEntitySetTypeLower = () => {
        return props.entitySet.type.displayNameSingular.toLocaleLowerCase();
    }

    const getListItemCross = (average: EntitySetAverage) => {
        return (
            <div className="remove-button" onClick={() => props.removeAverage(average)}>
                <i className="material-symbols-outlined">close</i>
            </div>
        );
    }

    const entitySetAverageListItem = (average: EntitySetAverage) => {
        return (
            <div className="instance" key={average.id}>
                <div className="legend-color average"/>
                <span className="instance-name">{props.getEntitySetName(average.entitySetId)}</span>
                {getListItemCross(average)}
            </div>
        );
    }

    const entitySetAverages = props.entitySet.getAverages().getAll();

    const getAverages = () => {
        return entitySetAverages.map(a => entitySetAverageListItem(a));
    }

    const getDivider = () => {
        return <div className="section-divider"/>;
    }

    return (
        <div className="entity-instance-list">
            {entitySetInstances.length > 0 && <div className="title">{props.entitySet.type.displayNamePlural.toUpperCase()}</div>}
            {entitySetInstances.map(entity => {
                const isActiveInstance = entity.id === props.entitySet.mainInstance?.id;
                return <EntitySetListItem
                    key={entity.id}
                    entity={entity}
                    instanceColor={props.entitySet.getInstanceColor(entity)}
                    isActiveInstance={isActiveInstance}
                    isLastEntityInstance={isLastEntityInstance}
                    entitySetType={getEntitySetTypeLower()}
                    removeEntityInstanceHandler={props.removeEntityInstance}
                    setEntityInstanceAsActiveHandler={props.setEntityInstanceAsActive}
                />;
            })}
            <div className="entity-instance-list-info">The <i className="material-symbols-outlined">star</i> <b>main {getEntitySetTypeLower()}</b> is prioritised on charts and in reports</div>
            {getDivider()}
            {entitySetAverages.length > 0 && <div className="title">AVERAGES</div>}
            {getAverages()}
        </div>
    );
}

export default EntitySetList