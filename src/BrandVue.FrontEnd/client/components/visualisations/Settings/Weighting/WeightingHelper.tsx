import { Subset, VariableDefinition, GroupedVariableDefinition, DateRangeVariableComponent, SurveyIdVariableComponent, VariableComponent,
    UiWeightingPlanConfiguration, VariableGrouping, UiWeightingTargetConfiguration, WeightingStyle, CompositeVariableComponent, InstanceListVariableComponent, Factory, VariableConfigurationModel, ValidationStatistics, ExtraResponseReason } from "../../../../BrandVueApi";
import { WaveDescription } from "./WeightingWaveListItem";
import { EntityInstance } from "../../../../entity/EntityInstance";
import { MetricSet } from "../../../../metrics/metricSet";
import { DataSubsetManager } from "../../../../DataSubsetManager";
import {NavigateFunction} from "react-router-dom";

const isValidWaveComponent = (component: VariableComponent) => {
    return component instanceof DateRangeVariableComponent || component instanceof SurveyIdVariableComponent;
}

export const minWeightWarning = 0.2;
export const maxWeightWarning = 5;
export const pendingRefreshSessionStorageKey = "pendingRefresh";
export const savedWeightingNameSessionStorageKey = "savedWeightingName";
export const mostRecentNodeIdSessionStorageKey = "mostRecentNodeId";

export const minWeightBelowThreshold = (weight: number | undefined) => weight !== undefined && weight < minWeightWarning;

export const maxWeightAboveThreshold = (weight: number | undefined) => weight !== undefined && weight > maxWeightWarning;

export const maxWeightAboveWarning = (statistic: ValidationStatistics | undefined) => statistic !== undefined && statistic.extraResponsesInExcel.filter(x => x.reason == ExtraResponseReason.ID_WeightTooLarge).length > 0;
export const minWeightBelowWarning = (statistic: ValidationStatistics | undefined) => statistic !== undefined && statistic.extraResponsesInExcel.filter(x => x.reason == ExtraResponseReason.ID_WeightTooSmall).length > 0;

export const navigateToWeightingPlan = (subsetId: string, navigate: NavigateFunction, wave?: WaveDescription) => {
    const subsetParam = `?subsetId=${subsetId}`;
    const nodeParam = wave?.DatabaseId ? `&nodeId=${wave.DatabaseId}` : `&waveInstanceId=${wave?.EntityId}`;
    navigate("weighting" + subsetParam + nodeParam);
}

export const navigateToWeightingImport = (subset: Subset, navigate: NavigateFunction, waveVariableIdentifier?: string, waveEntityInstanceId?: number) => {
    const subsetParam = `?importSubsetId=${subset.id}`;
    const waveVariableParam = waveVariableIdentifier ? `&importWaveVariableId=${waveVariableIdentifier}` : "";
    const waveParam = waveEntityInstanceId ? `&importWaveId=${waveEntityInstanceId}` : "";
    navigate("weighting" + subsetParam + waveVariableParam + waveParam);
}

export const getWavesFromVariableDefinition = (variableDefinition: VariableDefinition) => {
    if (isWaveVariable(variableDefinition)) {
        return variableDefinition.groups;
    }

    return [];
}

export const getReadableWeightingStyle = (style: WeightingStyle) => {
    switch(style) {
        case WeightingStyle.RIM:
        case WeightingStyle.Expansion:
            return "RIM / Expansion Weighting";
        case WeightingStyle.ResponseWeighting:
            return "Response-level Weighting";
    }
}

export const isWaveVariable = (variableDefinition: VariableDefinition): variableDefinition is GroupedVariableDefinition => {
    if (variableDefinition instanceof GroupedVariableDefinition) {
        return variableDefinition.groups.every(g => isValidWaveComponent(g.component));
    }
    return false;
}

export const doesWaveHaveWeights = (wave: WaveDescription) => {
    const responseWeighted = wave.ResponseWeightingContextId !== undefined;

    return responseWeighted || isWaveRIMWeighted(wave);
}

export const isWaveRIMWeighted = (wave: WaveDescription) => {
    return wave.DatabaseId !== undefined && (wave.ChildPlans !== undefined && wave.ChildPlans.length > 0);
}

export const doesInstanceHaveWeights = (instance: EntityInstance, plan: UiWeightingPlanConfiguration) => {
    return plan.uiChildTargets.some(target => target.entityInstanceId == instance.id && target.uiChildPlans.length > 0);
}

export const createWaveDescriptor = (wave: VariableGrouping, targetConfiguration?: UiWeightingTargetConfiguration, numberOfRespondentsForWave?:number): WaveDescription => {
    return {
        InstanceName: wave.toEntityInstanceName,
        Wave: wave,
        EntityId: wave.toEntityInstanceId,
        DatabaseId: targetConfiguration?.id,
        ChildPlans: targetConfiguration?.uiChildPlans,
        ResponseWeightingContextId: targetConfiguration?.responseWeightingContextId,
        NumberOfRespondentsForWave: numberOfRespondentsForWave,
    }
}

const getVariablesForChildComponents = (variableConfig: GroupedVariableDefinition, items: string[], metricsSet: MetricSet, variables: VariableConfigurationModel[]) => {
    const itemSet: Set<string> = new Set();

    variableConfig.groups.forEach((y) => {
        if (y.component instanceof CompositeVariableComponent) {
            y.component.compositeVariableComponents.forEach((sub) => {
                if (sub instanceof InstanceListVariableComponent) {
                    const relatedVariable = variables.find(v => v.identifier == sub.fromVariableIdentifier);
                    if (relatedVariable) {
                        const relatedMetric = metricsSet.metrics.find(m => m.variableConfigurationId == relatedVariable.id);
                        if (relatedMetric) {
                            itemSet.add(relatedMetric.varCode);
                        }
                    }
                }
            })
        }
    });

    itemSet.forEach(v => items.push(v));
}

export const getWavesForSurveyOrTimeBasedVariable = (variableIdentifier: string, metricsSet: MetricSet, variables: VariableConfigurationModel[]): Promise<VariableGrouping[]> => {
    const variableClient = Factory.VariableConfigurationClient(() => { });

    return variableClient.getVariableConfiguration(variableIdentifier)
        .then(variableConfig => {
            if (variableConfig.definition instanceof GroupedVariableDefinition) {
                const definition = variableConfig.definition as GroupedVariableDefinition;
                if (isWaveVariable(definition)) {
                    return definition.groups;
                }

                const childComponentVars: string[] = [];
                getVariablesForChildComponents(variableConfig.definition, childComponentVars, metricsSet, variables)

                if (childComponentVars.length == 0) {
                    return [];
                }

                const childWaveGroupPromises = childComponentVars.map(variable => {
                    return getWavesForSurveyOrTimeBasedVariable(variable, metricsSet, variables);
                });

                return Promise.all(childWaveGroupPromises).then(childWaveGroups => {
                    const populatedChildWaveGroups = childWaveGroups.filter(x => x.length > 0);
                    if (populatedChildWaveGroups.length == 1) {
                        return populatedChildWaveGroups[0];
                    }
                    return [];
                });
            }
            return [];
        });
}

export const reloadMetrics = (subsetId: string) => {
    if (DataSubsetManager.selectedSubset.id !== subsetId) {
        const metricClient = Factory.MetricsClient(error => error());
        return metricClient.getMetricsWithDisabledAndBaseDescription(subsetId).then(measures => {
            const metrics = MetricSet.mapMeasuresToMetrics(measures);
            return new MetricSet({ metrics: metrics });
        });
    }
}