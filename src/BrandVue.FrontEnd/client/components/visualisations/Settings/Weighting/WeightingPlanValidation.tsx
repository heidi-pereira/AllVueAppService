import {
    WeightingStatus, WeightingValidationMessage, UiWeightingPlanConfiguration, DetailedPlanValidationV2, VariableGrouping, 
    ErrorMessageType, ErrorMessageLevel, Message } from "../../../../BrandVueApi";
import { IEntityConfiguration } from "../../../../entity/EntityConfiguration";
import { MetricSet } from "../../../..//metrics/metricSet";
import { doesWaveHaveWeights, createWaveDescriptor } from "./WeightingHelper";
import { WaveDescription } from "./WeightingWaveListItem";

export enum WaveError {
    None,
    MissingEntity,
    MultipleEntitiesCombination,
    MissingMetric,
    Unweighted,
    UnweightedNoTarget
}

interface IWeightingWaveValidation {
    instanceId: number;
    errorType: WaveError;
}

interface IWeightingWaveGroupedValidaton {
    errorType: WaveError;
    instanceIds: number[];
}

interface IWeightingPlanValidation {
    isLoading: boolean;
    isValid: boolean;
    status: WeightingStatus;
    messages: WeightingValidationMessage[];
    waveErrors: IWeightingWaveValidation[];
    weightingPlanConfiguration: UiWeightingPlanConfiguration;
}

export class WeightingPlanValidation implements IWeightingPlanValidation {
    isLoading: boolean;
    isValid: boolean;
    status: WeightingStatus;
    messages: WeightingValidationMessage[];
    waveErrors: IWeightingWaveValidation[];
    weightingPlanConfiguration: UiWeightingPlanConfiguration;
    groupedWaveErrors: IWeightingWaveGroupedValidaton[];
    instanceNames: {};
    entityConfiguration: IEntityConfiguration;
    metricSet: MetricSet;

    constructor(data?: IWeightingPlanValidation) {
        this.groupedWaveErrors = [];
        this.instanceNames = {};
        if (data) {
            this.messages = [];
            this.waveErrors = [];
            for (var property in data) {
                if (data.hasOwnProperty(property)) {
                    this[property] = data[property];
                }
            }
        } else {
            this.isLoading = true;
            this.isValid = true;
            this.status = WeightingStatus.WeightingConfiguredValid;
            this.messages = [];
            this.waveErrors = [];
        }
    }

    static fromUiWeightingPlanConfiguration = (weightingPlanConfiguration: UiWeightingPlanConfiguration): WeightingPlanValidation => {
        const planValidation = new WeightingPlanValidation();
        planValidation.weightingPlanConfiguration = weightingPlanConfiguration;
        return planValidation;
    }
    
    setFromDetailedPlanValidationV2 = (planValidation: DetailedPlanValidationV2): WeightingPlanValidation => {
        this.isLoading = false;
        this.isValid = planValidation.isValid;
        this.status = planValidation.status;
        this.messages = planValidation.messages;
        return this;
    }

    setWaveErrorsFromVariableGrouping = (variableGrouping: VariableGrouping[], metricSet: MetricSet, entityConfiguration: IEntityConfiguration) => {
        this.entityConfiguration = entityConfiguration;
        this.metricSet = metricSet;

        const unweightedWaveDescriptors = variableGrouping.map(wave => this.convertToWaveDescriptor(wave, this.weightingPlanConfiguration));
        if (unweightedWaveDescriptors.filter(wave => !wave.DatabaseId  && !doesWaveHaveWeights(wave)).length !== unweightedWaveDescriptors.length) {

            this.waveErrors = unweightedWaveDescriptors.filter(wave => !doesWaveHaveWeights(wave)).map(wave => {
                if (wave.DatabaseId) {
                    return ({ instanceId: wave.EntityId, errorType: WaveError.Unweighted });
                }

                return ({ instanceId: wave.EntityId, errorType: WaveError.UnweightedNoTarget })
            });
        }

        const entityErrors = variableGrouping.map(wave => ({ instanceId: wave.toEntityInstanceId, errorType: this.getWaveErrorForEntity(metricSet, entityConfiguration, this.weightingPlanConfiguration.variableIdentifier, wave.toEntityInstanceId) })).filter(error => error.errorType !== WaveError.None);
        this.waveErrors.push(...entityErrors);
        const errorTypes = new Set(this.waveErrors.map(error => error.errorType));
        errorTypes.forEach(errorType => {
            const instanceIds = this.waveErrors.filter(error => error.errorType === errorType).map(error => error.instanceId);
            this.groupedWaveErrors.push({ errorType, instanceIds });
        });
        this.setInstanceNamesLookup(variableGrouping);
        return this;
    }

    getErrorMessageTextFromValidationMessage = (message: WeightingValidationMessage, entityIds: number[]): string => {
        const instanceName = entityIds.length > 1 ? "Multiple waves" : this.getEntityNameByInstanceId(entityIds[0]);
        const haveOrHas = entityIds.length > 1 ? "have" : "has";
    
        switch (message.errorType) {
            case ErrorMessageType.MissingVariable:
                return `${instanceName} cannot be found, check to see if it has been renamed or deleted`;
            case ErrorMessageType.InvalidNestedTarget:
                return `The weights for ${instanceName} under ${message.parentWithGrouping} don't add up to 100%, please check that these weights are correct`;
            case ErrorMessageType.OverlappingWave:
                var instanceNames = entityIds.map(this.getEntityNameByInstanceId).join(', ');
                if (entityIds.length > 1) {
                    return `Overlapping waves cannot be weighted: ${instanceNames}`;
                }
                return `Overlapping waves cannot be weighted. Amend date ranges to fix this`;
            case ErrorMessageType.QuestionHasNoTargets:
                return `Add weights to ${message.filterMetricName} to fix this.`;
            case ErrorMessageType.QuestionNotValid:
                return `${instanceName} ${haveOrHas} no targets. Add weighting targets`;
            case ErrorMessageType.QuestionUsedMoreThanOnce:
                return `${instanceName} ${haveOrHas} been used more than one time.  Remove all but one of the duplicates of ${instanceName}`;
            case ErrorMessageType.EmptyPlan:
                return `This weighting plan has no questions associated with it. Add variables and weighting targets to this plan to apply weighting to the data`;
            case ErrorMessageType.MixedTargetPercentageAndPopulation:
                return `${message.filterMetricName} has a mix of target percentages and target sample. Use only one method per question.`;
            case ErrorMessageType.TargetPopulationOutsideOfRoot:
                return `${message.filterMetricName} has target sample configured, but is not the singular root question.`;
            default:
                return `Unknown error type ${message.errorType}`;
        }
    }

    convertToWaveDescriptor = (wave: VariableGrouping, weightingPlanConfiguration: UiWeightingPlanConfiguration): WaveDescription => {
        const targetConfiguration = weightingPlanConfiguration.uiChildTargets.find(target => target.entityInstanceId === wave.toEntityInstanceId);
        return createWaveDescriptor(wave, targetConfiguration);
    }

    setInstanceNamesLookup = (variableGrouping: VariableGrouping[]) => {
        variableGrouping.forEach(wave => {
            this.instanceNames[wave.toEntityInstanceId] = wave.toEntityInstanceName;
        });
    }

    getWaveErrorForEntity = (metricSet: MetricSet, entityConfiguration: IEntityConfiguration, metricName: string, entityInstanceId: number): WaveError => {
        const metrics = metricSet.getMetrics(metricName);
        if (metrics && metrics.length == 1) {
            const entities = metrics[0].entityCombination;
            if (entities && entities.length == 1) {
                const myEntitySet = entityConfiguration.getDefaultEntitySetFor(entities[0]);
                const entityName = myEntitySet.getInstances().getById(entityInstanceId)?.name;
                return entityName ? WaveError.None : WaveError.MissingEntity
            }
            return WaveError.MultipleEntitiesCombination;
        }
        return WaveError.MissingMetric;
    }

    static defaultInstanceName = (entityInstanceId: number): string => `Instance ${entityInstanceId}`;

    getEntityNameByInstanceId = (entityInstanceId: number): string => {
        if (this.instanceNames.hasOwnProperty(entityInstanceId)) {
            return this.instanceNames[entityInstanceId];
        }
        return WeightingPlanValidation.defaultInstanceName(entityInstanceId);
    }

    getWaveErrorMessage = (errorType: WaveError, instanceIds: number[]): string => {
        const entities: string = instanceIds.map(this.getEntityNameByInstanceId).join(', ');
        const metricName = this.weightingPlanConfiguration.variableIdentifier;
        switch (errorType) {
            case WaveError.UnweightedNoTarget:
            case WaveError.Unweighted:
                return `Unweighted ${entities}.`;
            case WaveError.MissingEntity:
                return `Variable ${metricName} does not have ${entities}.`;
            case WaveError.MissingMetric:
                return `Variable ${metricName} is missing.`;
            case WaveError.MultipleEntitiesCombination:
                //This is a simplification, but it should make sense to the user
                //Only internal Savanta users from the tech team are likely to generate
                //this error.
                return `Only Date-based or Survey-based wave variables are supported.`;
        }
        return ``;
    }

    getGroupedWaveErrorMessages = () => {
        const allWavesUnweightedNoTarget = this.groupedWaveErrors.length > 0 && this.groupedWaveErrors.every(g => g.errorType == WaveError.UnweightedNoTarget);

        return this.groupedWaveErrors.map(group => {
            const newMessage = new Message();
            newMessage.messageText = this.getWaveErrorMessage(group.errorType, group.instanceIds);
            newMessage.errorLevel = allWavesUnweightedNoTarget ? ErrorMessageLevel.Warning : ErrorMessageLevel.Error;
            return newMessage;
        });
    }

    getValidationMessagesForEntityInstanceId = (entityInstanceId: number): WeightingValidationMessage[] => {
        return this.messages.filter(message => message.parentVariables && message.parentVariables.length > 0 ? message.parentVariables[0].instanceId === entityInstanceId : 
            message.instanceIds.some(x => x === entityInstanceId) ||
            message.targets.some(x => x.entityId === entityInstanceId) ||
            message.suspectMetrics.some(x => x.instanceIds.some(y => y === entityInstanceId)));
    }

    getWaveErrorsForEntityInstanceId = (entityInstanceId: number): WaveError[] => {
        return this.waveErrors.filter(error => error.instanceId === entityInstanceId).map(error => error.errorType);
    }
}