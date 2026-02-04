import React from 'react';
import { EntityInstance } from "../entity/EntityInstance";
import { EntitySet } from "../entity/EntitySet";

interface IEntitySetDropdownInstanceListProps {
    entitySet: EntitySet,
    sortedInstanceList: EntityInstance[];
    getItemMarkup(name: string): React.ReactNode,
}

export const EntitySetDropdownInstanceList = (props: IEntitySetDropdownInstanceListProps) => {

    const getDropdownHeader = () => {
        if (props.entitySet.type.isBrand) {
            return (
                <div className="entity-set-name">
                    <div>{props.entitySet.name}</div>
                </div>
            );
        }

        return (
            <div className="entity-set-name">{props.entitySet.name}</div>
        )
    };

    return (
        <div className="entity-set-members">
            {getDropdownHeader()}
            <div className="instances">
                {props.sortedInstanceList.map(i => {
                    var instanceColor = props.entitySet.getInstanceColor(i);
                    return <div className="instance" key={i.id}>
                        <div className="legend-color" style={{ background: instanceColor, borderColor: instanceColor }} />
                        {props.getItemMarkup(i.name)}
                    </div>;
                }
                )}
            </div>
        </div>
    );
};