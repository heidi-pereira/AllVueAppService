import {EntityInstance} from "../entity/EntityInstance";
import React from "react";

interface IEntitySetListItem {
    entity: EntityInstance
    instanceColor: string
    isActiveInstance: boolean
    entitySetType: string
    isLastEntityInstance: boolean
    removeEntityInstanceHandler: (entity: EntityInstance) => void
    setEntityInstanceAsActiveHandler: (entity: EntityInstance) => void
}

const EntitySetListItem = (props: IEntitySetListItem) => {
    const [isHoveredOverStar, setIsHoveredOverStar] = React.useState(false);

    const getListItemCross = (entity: EntityInstance) => {
        if (!props.isLastEntityInstance){
            return (
                <div className="remove-button" onClick={() => props.removeEntityInstanceHandler(entity)}>
                    <i className="material-symbols-outlined">close</i>
                </div>
            );
        }
        return (<div className="icon-placeholder"/>)
    }

    const getStarToggle = (isActive: boolean) => {
        if (isActive){
            return <i className="material-symbols-outlined">star</i>;
        }else {
            return <i className="material-symbols-outlined">star_border</i>;
        }
    }

    const getListItemStarToggle = (entity: EntityInstance, isActiveInstance: boolean) => {
        return (
            <div className={`star-toggle${isActiveInstance? "-selected": ""}`} 
                 onClick={() => {props.setEntityInstanceAsActiveHandler(entity)}} 
                 onMouseEnter={() => {setIsHoveredOverStar(true)}}
                 onMouseLeave={() => {setIsHoveredOverStar(false)}}>
                {getStarToggle(isActiveInstance || isHoveredOverStar)}
            </div>
        );
    }
    
    return (
        <div className={`instance ${props.isActiveInstance ? "active" : ""}`} key={props.entity.id}>
            <div className="legend-color" style={{ background: props.instanceColor, borderColor: props.instanceColor }} />
            <span className="instance-name">{props.entity.name}</span>
            {getListItemStarToggle(props.entity, props.isActiveInstance)}
            {getListItemCross(props.entity)}
        </div>
    );
}

export default EntitySetListItem;