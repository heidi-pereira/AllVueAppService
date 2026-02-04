import React from 'react';
import { LowSampleSummary, IEntityType } from "../BrandVueApi";
import WarningWithToggle from './WarningWithToggle';

const LowSampleWarning = (props: {toggleVisibility: () => void, activeEntityType: IEntityType, lowSampleSummaries: LowSampleSummary[] }) => {
    const lowSampleIds = props.lowSampleSummaries.map(x => x.entityInstanceId).filter(x => x != null) as number[];
    const uniqueIds = Array.from(new Set(lowSampleIds));
    const lowSampleCount = uniqueIds.length;
    if (lowSampleCount === 0) {
        return null;
    }
    // if the active entity is not set at the start (e.g. metric comparison, we use the neutral word "metric")
    const entityName = lowSampleCount > 1
        ? (props.activeEntityType ? props.activeEntityType.displayNamePlural : "metrics")
        : (props.activeEntityType ? props.activeEntityType.displayNameSingular : "metric");

    return <WarningWithToggle
        text={`Low sample - ${lowSampleCount} ${entityName.toLowerCase()}`}
        toggleVisibility={props.toggleVisibility} />;
}
export default LowSampleWarning;