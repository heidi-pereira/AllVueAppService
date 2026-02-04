import * as BrandVueApi from "../../../../../BrandVueApi";
import {
    CompositeVariableComponent,
    DateRangeVariableComponent, InclusiveRangeVariableComponent, InstanceListVariableComponent,
    InstanceVariableComponentOperator,
    MainQuestionType, SurveyIdVariableComponent, VariableComponent,
    VariableDefinition,
    VariableGrouping,
    GroupedVariableDefinition,
    VariableGroupSampleModel,
    VariableSampleResult,
    BaseGroupedVariableDefinition,
    FieldExpressionVariableDefinition,
    VariableFieldExpressionSampleModel,
    VariableConfigurationModel
} from "../../../../../BrandVueApi";
import { IEntityConfiguration } from "../../../../../entity/EntityConfiguration";
import {Metric} from "../../../../../metrics/metric";
import {getEndOfDayDateUtc, getStartOfDayDateUtc} from "../../../../helpers/PeriodHelper";
import { getSplitByAndFilterByEntityTypesForMetric } from "../../../../helpers/SurveyVueUtils";

export const WAVE_PERIOD_MEASURE_NAME = "Wave/Period";
export const SURVEY_ID_MEASURE_NAME = "Survey";

const createDateRangeVariableComponent = () => {
    const currentDate = new Date();
    return new DateRangeVariableComponent({
        minDate: getStartOfDayDateUtc(currentDate),
        maxDate: getEndOfDayDateUtc(currentDate)
    });
}

const createSurveyIdVariableComponent =() => {
    return new SurveyIdVariableComponent({ surveyIds: [] });
}

const createInclusiveRangeVariableComponent = (variableIdentifier: string, metric: Metric) => {
    const component = new InclusiveRangeVariableComponent();
    component.operator = BrandVueApi.VariableRangeComparisonOperator.Between;
    component.fromVariableIdentifier = variableIdentifier;
    component.resultEntityTypeNames = metric.primaryFieldEntityCombination.map(t => t.identifier);
    return component;
}

const createInstanceListVariableComponent = (variableIdentifier: any, metric: Metric, entityConfiguration: IEntityConfiguration, preselectedVariableComponents: any[] | undefined) => {
    const entityTypes = getSplitByAndFilterByEntityTypesForMetric(metric, entityConfiguration);
    const fromEntityType = entityTypes.filterByEntityTypes[0] ?? metric.entityCombination[0];
    let fromEntityTypeName = fromEntityType.identifier;
    let resultEntityTypeNames = metric.entityCombination.filter(e => e.identifier !== fromEntityType.identifier).map(e => e.identifier);

    if (preselectedVariableComponents && preselectedVariableComponents.length > 0) {
        const preselectedComponent = preselectedVariableComponents.find(c => c.fromVariableIdentifier === variableIdentifier);
        if (preselectedComponent) {
            resultEntityTypeNames = preselectedComponent.resultEntityTypeNames;
            fromEntityTypeName = preselectedComponent.fromEntityTypeName;
        }
    }

    return new InstanceListVariableComponent({
        fromEntityTypeName: fromEntityTypeName,
        fromVariableIdentifier: variableIdentifier,
        instanceIds: [],
        operator: InstanceVariableComponentOperator.Or,
        resultEntityTypeNames: resultEntityTypeNames
    });
}

const getVariableIdentifier = (metric: Metric, variables: any[]) => {
    let variableIdentifier = metric.primaryVariableIdentifier;
    const variable = variables.find(v => v.id === metric.variableConfigurationId);
    if (variable) {
        variableIdentifier = variable.identifier;
    }
    return variableIdentifier;
}

const instanceListVariableComponentQuestionTypes = [
    BrandVueApi.MainQuestionType.SingleChoice,
    BrandVueApi.MainQuestionType.MultipleChoice,
    BrandVueApi.MainQuestionType.CustomVariable,
    BrandVueApi.MainQuestionType.GeneratedNumeric
];

export const getEmptyVariableComponentForMetric = (
    metric: Metric,
    questionTypeLookup: { [key: string]: MainQuestionType },
    variables: BrandVueApi.VariableConfigurationModel[],
    entityConfiguration: IEntityConfiguration,
    compositeVariableComponents?: InstanceListVariableComponent[]
): VariableComponent => {

    if (metric.entityCombination.length === 0) {
        if (metric.name === WAVE_PERIOD_MEASURE_NAME) {
            return createDateRangeVariableComponent();
        }

        if (metric.name === SURVEY_ID_MEASURE_NAME) {
            return createSurveyIdVariableComponent();
        }
    }

    if (metric.calcType !== BrandVueApi.CalculationType.Text) {
        const questionType = questionTypeLookup[metric.name];
        const variableIdentifier = getVariableIdentifier(metric, variables);
        const isInstanceListVariableComponentQuestionType = instanceListVariableComponentQuestionTypes.includes(questionType);
        const isVariableWithEntities = metric.isBasedOnCustomVariable && metric.entityCombination.length > 0;

        if (isAnswerANumericValue(questionType, metric) && metric.entityCombination.length === 0) {
            return createInclusiveRangeVariableComponent(variableIdentifier, metric);
        } else if (isVariableWithEntities || isInstanceListVariableComponentQuestionType) {
            return createInstanceListVariableComponent(variableIdentifier, metric, entityConfiguration, compositeVariableComponents);
        }
    }

    throw new Error(`Invalid metric for variable component: ${metric.name}`);
};

const isAnswerANumericValue = (questionType: MainQuestionType, metric: Metric): boolean => {
    //ToDo: Improve this
    //https://app.shortcut.com/mig-global/story/81853/when-cloning-a-variable-deal-correctly-with-number-calculations
    return (questionType === BrandVueApi.MainQuestionType.Value)
        || (questionType === BrandVueApi.MainQuestionType.SingleChoice && metric.primaryFieldEntityCombination.length === 0)
        || (metric.calcType == BrandVueApi.CalculationType.NetPromoterScore)
}

export function getFromVariableIdentifier(apiComponent: BrandVueApi.VariableComponent): string {

    if (apiComponent instanceof BrandVueApi.InstanceListVariableComponent) {
        return apiComponent.fromVariableIdentifier;
    }

    if (apiComponent instanceof BrandVueApi.InclusiveRangeVariableComponent) {
        return apiComponent.fromVariableIdentifier;
    }

    return "";
}

export function getVariableId(apiComponent: BrandVueApi.VariableComponent, variables: BrandVueApi.VariableConfigurationModel[]): number | undefined {
    if (apiComponent instanceof BrandVueApi.InstanceListVariableComponent || apiComponent instanceof BrandVueApi.InclusiveRangeVariableComponent) {
        return variables.find(x => x.identifier == apiComponent.fromVariableIdentifier)?.id;
    }
}

export function getSelectedMetric(validMetricsForVariables: Metric[], apiComponent: BrandVueApi.VariableComponent, variables: BrandVueApi.VariableConfigurationModel[]): Metric {
    let metric: Metric | undefined;
    if (apiComponent instanceof DateRangeVariableComponent){
        metric = validMetricsForVariables.find(x => x.name === WAVE_PERIOD_MEASURE_NAME)!;
        return metric;
    }

    if (!metric && apiComponent instanceof SurveyIdVariableComponent){
        metric = validMetricsForVariables.find(x => x.name === SURVEY_ID_MEASURE_NAME)!;
        return metric;
    }

    if (!metric) {
        const variableConfigurationId = getVariableId(apiComponent, variables);
        metric = validMetricsForVariables.find(x => variableConfigurationId && (x.variableConfigurationId == variableConfigurationId));
    }

    if (!metric) {
        console.log("Unable to match variable id to metric, doing best guess")
        const apiFieldName = getFromVariableIdentifier(apiComponent);
        //
        //Assume that if there was a related metric based off a variable then the above code has already found it.
        //So can now look for only metrics that are not based on variables.
        //
        const validMetricsThatAreNotVariables = validMetricsForVariables.filter(m => m.primaryFieldDependencies?.length == 1 && m.variableConfigurationId == null);

        const metricsByInstanceName = validMetricsThatAreNotVariables.filter(x => x.primaryVariableIdentifier == apiFieldName);
        metric = metricsByInstanceName[0];
        if (!metric) {

            const metricsContainingFieldSorted = validMetricsThatAreNotVariables.filter(x => x.primaryFieldDependencies[0].name.includes(apiFieldName))
                .sort((a, b) => b.primaryFieldDependencies[0].name.length - a.primaryFieldDependencies[0].name.length);
            //
            // Atempting to find the related metric
            //
            // for all vue this is likely to be correct
            // however in BrandVue this will be wrong sometimes as there can easily be more than one metric that matches the criteria
            //
            metric = metricsContainingFieldSorted[0];
        }
    }

    if (!metric) {
        throw new Error(`No matching metric for field ${getFromVariableIdentifier(apiComponent)}`);
    }

    return metric;
}

export const duplicateComponent = (component: VariableComponent | undefined): VariableComponent | undefined => {
    if (component) {
        if (component instanceof CompositeVariableComponent) {
            return new CompositeVariableComponent({
                compositeVariableComponents: [...component.compositeVariableComponents],
                compositeVariableSeparator: component.compositeVariableSeparator,
            });
        }

        if (component instanceof InclusiveRangeVariableComponent){
            return new InclusiveRangeVariableComponent({
                min: component.min,
                max: component.max,
                exactValues: [...component.exactValues],
                inverted: component.inverted,
                fromVariableIdentifier: component.fromVariableIdentifier,
                operator: component.operator,
                resultEntityTypeNames: [...component.resultEntityTypeNames]
            })
        }

        if (component instanceof InstanceListVariableComponent){
            return new InstanceListVariableComponent({
                fromVariableIdentifier: component.fromVariableIdentifier,
                fromEntityTypeName: component.fromEntityTypeName,
                operator: component.operator,
                resultEntityTypeNames: [...component.resultEntityTypeNames],
                instanceIds: [...component.instanceIds]
            })
        }

        if (component instanceof DateRangeVariableComponent){
            return new DateRangeVariableComponent({
                minDate: component.minDate,
                maxDate: component.maxDate
            })
        }

        if (component instanceof SurveyIdVariableComponent){
            return new SurveyIdVariableComponent({
                surveyIds: [...component.surveyIds]
            })
        }
    }
    return undefined
}

export async function getGroupCountAndSample(subsetId: string, group: VariableGrouping): Promise<VariableSampleResult[]> {
    const model = new VariableGroupSampleModel({
        subsetId: subsetId,
        group: group,
    });
    return await BrandVueApi.Factory.VariableConfigurationClient(error => error())
        .getVariableGroupCountAndSamplePreview(model);
}

export async function getFieldExpressionCountAndSample(subsetId: string, definition: FieldExpressionVariableDefinition): Promise<VariableSampleResult[]> {
    const model = new VariableFieldExpressionSampleModel({
        subsetId: subsetId,
        definition: definition
    });
    return await BrandVueApi.Factory.VariableConfigurationClient(error => error())
        .getFieldExpressionVariableCountAndSamplePreview(model);
}

export function checkHasGroupedGroupsEntityTypes(variableDefinition: VariableDefinition, variableName: string): VariableDefinition {
    if (variableDefinition instanceof BaseGroupedVariableDefinition || variableDefinition instanceof GroupedVariableDefinition) {
        const groupsClone = variableDefinition.groups.map(g => new VariableGrouping({...g}));
        const definitionClone = variableDefinition instanceof BaseGroupedVariableDefinition ?
            new BaseGroupedVariableDefinition({...variableDefinition, groups: groupsClone}) : new GroupedVariableDefinition({...variableDefinition, groups: groupsClone});
        if (definitionClone.groups.length === 1 && definitionClone.groups[0].toEntityInstanceName === `New group 1`){
            definitionClone.groups[0].toEntityInstanceName = variableName;
        }
        definitionClone.toEntityTypeName = variableName;
        definitionClone.toEntityTypeDisplayNamePlural = variableName;
        return definitionClone;
    }
    return variableDefinition;
}

export const GetUnderlyingMetric = (metric: Metric | undefined, allMetrics: Metric[], variables: VariableConfigurationModel[]) => {
    if (!metric?.isBasedOnCustomVariable) return;

    const variableConfig = variables.find(v => v.id == metric.variableConfigurationId);
    const varDefinition = variableConfig?.definition;
    if (!(varDefinition instanceof GroupedVariableDefinition)) return;

    const components = varDefinition.groups.map(g => g.component);
    if (!(components.length && components[0] instanceof InstanceListVariableComponent)) return;

    const firstComponentIdentifier = components[0].fromVariableIdentifier;
    const mixedComponentsFound = components.some(c => !(c instanceof InstanceListVariableComponent && c.fromVariableIdentifier === firstComponentIdentifier));
    if (mixedComponentsFound) return;

    return allMetrics.find(m => m.primaryVariableIdentifier == firstComponentIdentifier);
}

export const GetBaseMetric = (metric: Metric | undefined, allMetrics: Metric[], variables: VariableConfigurationModel[]) => {
    const baseMetric = GetUnderlyingMetric(metric, allMetrics, variables);
    return baseMetric ? GetBaseMetric(baseMetric, allMetrics, variables) : metric;
}