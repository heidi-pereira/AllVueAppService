import _ from "lodash";
import { CrossMeasure, IEntityType } from "../../../BrandVueApi";
import { MetricSet } from "../../../metrics/metricSet";

export const getTypesReferencedByBreaks = (crossMeasures: CrossMeasure[], enabledMetricSet: MetricSet): IEntityType[] => {
    return crossMeasures.map(c => {
        const measure = enabledMetricSet.getMetric(c.measureName);
        if (measure) {
            const childMeasureTypes = getTypesReferencedByBreaks(c.childMeasures, enabledMetricSet);
            return measure.entityCombination.concat(childMeasureTypes);
        }
        return [];
    }).reduce((curr, prev) => _.unionBy(curr, prev, (t: IEntityType) => t.identifier), []);
}

export const doBreaksMatch = (breaksOne: CrossMeasure[], breaksTwo: CrossMeasure[]): boolean => {
    if (breaksOne.length !== breaksTwo.length) {
        return false;
    }
    for (let breaksIndex = 0; breaksIndex < breaksOne.length; breaksIndex++) {
        if (breaksOne[breaksIndex].measureName !== breaksTwo[breaksIndex].measureName) {
            return false;
        }
        if (breaksOne[breaksIndex].filterInstances.length !== breaksTwo[breaksIndex].filterInstances.length) {
            return false;
        }

        const breaksOneFilterInstances = breaksOne[breaksIndex].filterInstances.slice().sort((a, b) => a.instanceId - b.instanceId);
        const breaksTwoFilterInstances = breaksTwo[breaksIndex].filterInstances.slice().sort((a, b) => a.instanceId - b.instanceId);

        for (let instanceIndex = 0; instanceIndex < breaksOne[breaksIndex].filterInstances.length; instanceIndex++) {
            if (breaksOneFilterInstances[instanceIndex].instanceId !== breaksTwoFilterInstances[instanceIndex].instanceId) {
                return false;
            }
        }
        if (!doBreaksMatch(breaksOne[breaksIndex].childMeasures, breaksTwo[breaksIndex].childMeasures)) {
            return false;
        }
    }
    return true;
};