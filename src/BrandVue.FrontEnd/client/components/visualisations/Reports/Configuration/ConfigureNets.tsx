import {
    CalculationType,
    GroupedVariableDefinition,
    InstanceListVariableComponent,
} from "../../../../BrandVueApi";
import * as BrandVueApi from "../../../../BrandVueApi";
import { EntityInstance } from "../../../../entity/EntityInstance";
import { PartWithExtraData } from "../ReportsPageDisplay";
import { getSplitByAndFilterByEntityTypesForPart } from "../../../helpers/SurveyVueUtils";
import { Metric } from "../../../../metrics/metric";
import { useContext, useEffect, useState } from "react";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { PageHandler } from "../../../PageHandler";
import { useMetricStateContext } from "../../../../metrics/MetricStateContext";
import { useEntityConfigurationStateContext } from "../../../../entity/EntityConfigurationStateContext";
import { useSavedReportsContext } from "../SavedReportsContext";
import { fetchVariableConfiguration } from "client/state/variableConfigurationsSlice";
import { useAppDispatch } from "client/state/store";
import { handleError } from 'client/components/helpers/SurveyVueUtils';

export interface IConfigureNets {
    createNet: (netName: string, selectedMetric: Metric, selectedEntityInstances: EntityInstance[]) => Promise<void>,
    removeNet: (option: EntityInstance) => Promise<void>,
    availableEntityInstances: EntityInstance[],
    canPickEntityInstances: boolean,
    canAddNet: boolean,
    isNetInstance: (instance: EntityInstance) => boolean,
    isDeletingNet: boolean,
    getNettedInstanceNames: (instance: EntityInstance) => string[]
}

export const useConfigureNets = (reportPart: PartWithExtraData, allMetrics: Metric[], subsetId: string, googleTagManager: IGoogleTagManager, pageHandler: PageHandler): IConfigureNets => {
    const { entityConfiguration, entityConfigurationDispatch } = useEntityConfigurationStateContext();
    const { metricsDispatch } = useMetricStateContext();
    const { reportsDispatch } = useSavedReportsContext();
    const dispatch = useAppDispatch();

    const metric = reportPart.metric;
    const originalMetric = metric && allMetrics.find(m => m.name === metric.originalMetricName);
    const entityTypes = getSplitByAndFilterByEntityTypesForPart(reportPart.part, metric, entityConfiguration);
    const [variableDefinition, setVariableDefinition] = useState<GroupedVariableDefinition | undefined>(undefined);

    useEffect(() => {
        if (reportPart.metric?.isBasedOnCustomVariable) {
            BrandVueApi.Factory.VariableConfigurationClient((_, error) => handleError(error))
                .getVariableConfiguration(reportPart.metric.name)
                .then(data => setVariableDefinition(data?.definition as GroupedVariableDefinition))
                .catch(e => handleError(e))
        }
    }, [reportPart.metric]);

    const getNettedInstanceNames = (instance: EntityInstance): string[] => {
        if (!isNetInstance(instance)) {
            return [];
        }

        const instanceDefinition = variableDefinition?.groups?.find(e => e.toEntityInstanceId === instance.id)
        if (instanceDefinition && instanceDefinition.component instanceof InstanceListVariableComponent) {
            return instanceDefinition.component.instanceIds
                .map(i => availableEntityInstances.find(e => e.id === i)?.name)
                .filter((instanceName): instanceName is string => !!instanceName);
        }

        return [];
    }

    /*
        This is a bit of a hack to calculate entity type heirarchy for nets.
        We assume when adding a Net that there's only 1 type different between the original & output metric
        If the selected type is in the original metric's entity combination, then it hasn't been netted.
        Otherwise find the "new" type, and its instances are the original ones.
    */
    const getOriginalInstances = (selectedType: BrandVueApi.IEntityType | undefined): EntityInstance[] | undefined => {
        if (selectedType && originalMetric && metric && entityConfiguration) {
            const originalType = originalMetric.entityCombination.find(t => t.identifier === selectedType.identifier);
            if (originalType) {
                return entityConfiguration.getAllEnabledInstancesForTypeOrdered(originalType);
            } else {
                const newType = originalMetric.entityCombination.filter(t => !metric.entityCombination.some(x => x.identifier === t.identifier));
                if (newType.length === 1) {
                    return entityConfiguration.getAllEnabledInstancesForTypeOrdered(newType[0]);
                }
            }
        }
    }

    const getEntityInstanceOptions = () => {
        const availableEntityInstances = entityTypes?.splitByEntityType ? entityConfiguration.getAllEnabledInstancesForTypeOrdered(entityTypes?.splitByEntityType) : [];
        const originalEntityInstances = getOriginalInstances(entityTypes?.splitByEntityType) ?? availableEntityInstances;

        return { availableEntityInstances, originalEntityInstances };
    }

    const { availableEntityInstances, originalEntityInstances } = getEntityInstanceOptions();
    const [isDeletingNet, setIsDeletingNet] = useState(false);

    const canPickEntityInstances: boolean = availableEntityInstances.length > 0
        //&& reportPart.part.partType !== PartType.ReportsCardStackedMulti 
        && metric?.calcType !== CalculationType.Text;

    /*
        Once you add a net to a type, you can only add more nets to that type for a given metric.
        This is because variables only output in 1 dimension and we don't handle calculating the "original" instances more than 1 level deep
    */
    const canAddNet = () => {
        if (originalMetric) {
            const newType = reportPart.metric?.entityCombination.filter(t => !originalMetric.entityCombination.some(x => x.identifier === t.identifier));
            if (newType && newType.length === 1) {
                return newType[0].identifier == entityTypes?.splitByEntityType?.identifier;
            }
        }

        return true;
    }

    const isNetInstance = (instance: EntityInstance) => {
        return !originalEntityInstances.map(o => o.id).includes(instance.id)
    }

    const createNet = async (netName: string, selectedMetric: Metric, selectedEntityInstances: EntityInstance[]) => {
        googleTagManager.addEvent('createdNet', pageHandler, { value: netName });
        await BrandVueApi.Factory.VariableConfigurationClient((_, error) => handleError(error))
            .addNet(subsetId, reportPart.part.id, selectedMetric.name, netName, selectedEntityInstances.map(e => e.id))
            .then(async () => {
                await entityConfigurationDispatch({ type: "RELOAD_ENTITYCONFIGURATION" });
                await metricsDispatch({ type: "RELOAD_METRICS" });
                await dispatch(fetchVariableConfiguration()).unwrap();
                await reportsDispatch({ type: "TRIGGER_RELOAD" });
            })
            .catch((e) => handleError(e));
    }

    const removeNet = async (option: EntityInstance) => {
        googleTagManager.addEvent('removedNet', pageHandler, { value: option.name });
        setIsDeletingNet(true)
        await BrandVueApi.Factory.VariableConfigurationClient((_, error) => {
            setIsDeletingNet(false)
            handleError(error)
        })
            .removeNet(subsetId, reportPart.part.id, reportPart.metric?.name!, reportPart.metric?.variableConfigurationId!, option.id)
            .then(async () => {
                setIsDeletingNet(false);
                await metricsDispatch({ type: "RELOAD_METRICS" });
                await entityConfigurationDispatch({ type: "RELOAD_ENTITYCONFIGURATION" });
                await reportsDispatch({ type: "TRIGGER_RELOAD" });
            })
            .catch((e) => handleError(e));
    }

    return {
        createNet: createNet,
        removeNet: removeNet,
        availableEntityInstances: availableEntityInstances,
        canPickEntityInstances: canPickEntityInstances,
        isNetInstance: isNetInstance,
        isDeletingNet: isDeletingNet,
        getNettedInstanceNames: getNettedInstanceNames,
        canAddNet: canAddNet(),
    }
}