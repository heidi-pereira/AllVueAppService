import React from "react";
import {
    EntityMeanMapping,
} from "../../../../../BrandVueApi";
import style from "./MeanValueAssignerContent.module.less";
import Tooltip from "../../../../Tooltip";

interface IMeanValueRowProps {
    entityMeanMap: EntityMeanMapping;
    updateEntityMeanMapping(updatedMap: EntityMeanMapping): void;
}

const MeanValueRow = (props: IMeanValueRowProps) => {
    const id = props.entityMeanMap.entityId;
    const name = props.entityMeanMap.entityInstanceName;
    const includeInCalculation = props.entityMeanMap.includeInCalculation;
    const meanCalculationValue = props.entityMeanMap.meanCalculationValue;

    const updateMap = (newMeanValue: number, newIncludeInCalculation: boolean) => {
        const clonedMap = new EntityMeanMapping(props.entityMeanMap);
        clonedMap.meanCalculationValue = newMeanValue;
        clonedMap.includeInCalculation = newIncludeInCalculation
        props.updateEntityMeanMapping(clonedMap);
    }

    const onMeanValueChange = (stringValue: string) => {
        const numValue = parseInt(stringValue);
        updateMap(numValue, includeInCalculation);
    }

    const onIncludeInCalculationChange = (newValue: boolean) => {
        updateMap(meanCalculationValue, newValue);
    }

    return (
            <div className={style.row} key={id}>
                <div className={style.choice}>
                    {name}
                </div>
                <div className={style.values}>
                    <Tooltip placement="top" title={`Include the mean value in overall calculation.  This won't affect the base`} delay={300}>
                        <div className={style.checkbox}>
                            <input type="checkbox"
                                key={id}
                                className="checkbox"
                                id={`${id}`}
                                checked={includeInCalculation}
                                onChange={() => onIncludeInCalculationChange(!includeInCalculation)}
                            />
                            <label htmlFor={`${id}`}></label>
                        </div>
                    </Tooltip>
                    <input type="number"
                        className={style.numberSelector}
                        disabled={!includeInCalculation}
                        autoFocus
                        autoComplete="off"
                        step="1"
                        value={meanCalculationValue}
                        onChange={(e) => onMeanValueChange(e.target.value)}
                    />
                </div>
            </div>
    )
}

export default MeanValueRow;