import React from "react";
import {EntityMeanMap, EntityMeanMapping} from "../../../../../BrandVueApi";
import style from "./MeanValueAssignerContent.module.less";
import MeanValueRow from "./MeanValueRow";

interface IMeanValueAssignerContentProps {
    entityMeanMap: EntityMeanMap;
    updateEntityMeanMap(updatedMap: EntityMeanMap): void;
    entityMeanMapIsDefault: boolean;
    restoreDefaults(): void;
}

const MeanValueAssignerContent = (props: IMeanValueAssignerContentProps) => {
    const updateEntityMeanMap = (mappingToUpdate: EntityMeanMapping) => {
        const newMapping = props.entityMeanMap!.mapping.map(m => {
            return new EntityMeanMapping({...m})
        })
        const mapIndex = newMapping.findIndex(m => m.entityId == mappingToUpdate.entityId)
        newMapping.splice(mapIndex, 1, mappingToUpdate);
        const newEntityMeanMap = new EntityMeanMap({entityTypeIdentifier: props.entityMeanMap.entityTypeIdentifier, mapping: newMapping})
        props.updateEntityMeanMap(newEntityMeanMap);
    }

    if(!props.entityMeanMap) {
        return <></>;
    }

    return (
        <div className={style.meanValueAssignerContent}>
            <div className={style.header}>
                <div className={style.meanValuesContainerHeader}>Mean values</div>
                {!props.entityMeanMapIsDefault && <div className={style.restoreDefault} onClick={props.restoreDefaults}>Restore mean values</div>}
            </div>
            <div className={style.meanValuesContainer}>
                <div className={style.header}>
                    <div className={style.choice}>
                        Choice
                    </div>
                    <div className={style.values}>
                        <div>Include</div>
                        <div>Value</div>
                    </div>
                </div>
                <div className={style.body}>
                    { props.entityMeanMap!.mapping.map((m, i) => 
                       <MeanValueRow entityMeanMap={m} updateEntityMeanMapping={e => updateEntityMeanMap(e)} key={i}/>
                    )}
                </div>
            </div>
        </div>
    )
}

export default MeanValueAssignerContent;