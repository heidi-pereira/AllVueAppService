import { IEntityConfiguration } from "client/entity/EntityConfiguration";
import { CompositeVariableComponent, GroupedVariableDefinition, InstanceListVariableComponent, InstanceVariableComponentOperator, VariableConfigurationModel, VariableGrouping } from "../../../../../BrandVueApi";
import { Metric } from "../../../../../metrics/metric";
import { GetUnderlyingMetric, getEmptyVariableComponentForMetric, SURVEY_ID_MEASURE_NAME, WAVE_PERIOD_MEASURE_NAME } from "./VariableComponentHelpers";
import * as BrandVueApi from "../../../../../BrandVueApi";
import { createEntities } from "../../../../../helpers/ReactTestingLibraryHelpers";

const mockMetric = () => {
    const metric = new Metric(undefined);
    metric.name = "TestMetric";
    metric.varCode = "TestMetric";
    metric.primaryVariableIdentifier = "TestMetric";
    return metric;
}

const mockInstanceVariableComponent = (identifier: string) => {
    const variableComponent = new InstanceListVariableComponent();
    variableComponent.fromVariableIdentifier = identifier;
    variableComponent.fromEntityTypeName = "Test";
    variableComponent.instanceIds = [1];
    variableComponent.operator = InstanceVariableComponentOperator.Or;
    variableComponent.resultEntityTypeNames = [];

    return variableComponent;
}

const mockInstanceVariableConfig = (metric: Metric) => {
    const definitionGroup = new VariableGrouping();
    definitionGroup.toEntityInstanceId = 1;
    definitionGroup.toEntityInstanceName = "UK";
    definitionGroup.component = mockInstanceVariableComponent(metric.varCode);

    const variableDefinition = new GroupedVariableDefinition();
    variableDefinition.groups = [definitionGroup];

    const variable = new VariableConfigurationModel();
    variable.id = 1;
    variable.definition = variableDefinition;

    return variable;
}

const mockCompositeVariableConfig = (metric: Metric) => {
    const compositeVariableComponent = new CompositeVariableComponent();
    const instanceVariableComponent = mockInstanceVariableComponent(metric.varCode);
    compositeVariableComponent.compositeVariableComponents = [instanceVariableComponent, instanceVariableComponent];

    const definitionGroup = new VariableGrouping();
    definitionGroup.toEntityInstanceId = 1;
    definitionGroup.toEntityInstanceName = "UK";
    definitionGroup.component = compositeVariableComponent;

    const variableDefinition = new GroupedVariableDefinition();
    variableDefinition.groups = [definitionGroup];

    const variable = new VariableConfigurationModel();
    variable.id = 1;
    variable.definition = variableDefinition;

    return variable;
}

const mockVariableMetric = (variableConfig: VariableConfigurationModel) => {
    const variableMetric = new Metric(undefined);
    variableMetric.name = variableConfig.displayName;
    variableMetric.variableConfigurationId = variableConfig.id;
    variableMetric.isBasedOnCustomVariable = true;

    return variableMetric;
}

describe("GetUnderlyingMetric", () => {
    test("when given a variable metric that points solely at a possible single choice metric, return that metric", () => {
        const testMetric = mockMetric();
        const testMetricVariableConfig = mockInstanceVariableConfig(testMetric);
        const variables = [testMetricVariableConfig];
        const variableMetric = mockVariableMetric(testMetricVariableConfig);
        const metrics = [testMetric, variableMetric]

        const result = GetUnderlyingMetric(variableMetric, metrics, variables);

        expect(result).toBe(testMetric);
    });

    test("when given a variable metric that points at a non-instance list component, return undefined", () => {
        const testMetric = mockMetric();
        const testMetricVariableConfig = mockCompositeVariableConfig(testMetric);
        const variables = [testMetricVariableConfig];
        const variableMetric = mockVariableMetric(testMetricVariableConfig);
        const metrics = [testMetric, variableMetric]

        const result = GetUnderlyingMetric(variableMetric, metrics, variables);

        expect(result).toBe(undefined);
    });

    test("when given a metric with no variable config id, return undefined", () => {
        const testMetric = mockMetric();
        const testMetricVariableConfig = mockCompositeVariableConfig(testMetric);
        const variables = [testMetricVariableConfig];
        const variableMetric = mockVariableMetric(testMetricVariableConfig);
        const metrics = [testMetric, variableMetric]

        const result = GetUnderlyingMetric(testMetric, metrics, variables);

        expect(result).toBe(undefined);
    });
});

const metricName = "SingleChoiceMetric";
const testVariableName = "TestVariable";
const variableConfigId = "variableConfigId";
const variableIdentifier = "variableIdentifier";

describe("getEmptyVariableComponentForMetric", () => {
    const mockMetric = (name, calcType, variableConfigurationId, primaryVariableIdentifier, primaryFieldEntityCombination) => {
        const metric = new Metric(undefined);
        metric.name = name;
        metric.calcType = calcType;
        metric.variableConfigurationId = variableConfigurationId;
        metric.primaryVariableIdentifier = primaryVariableIdentifier;
        metric.primaryFieldEntityCombination = primaryFieldEntityCombination;
        metric.entityCombination = primaryFieldEntityCombination;
        return metric;
    };

    const mockVariable = (id, identifier) => {
        const variable = new VariableConfigurationModel();
        variable.id = id;
        variable.identifier = identifier;
        return variable;
    };

    const mockEntityConfiguration = () => {
        return {} as IEntityConfiguration;
    };

    const questionTypeLookup = {
        "SingleChoiceMetric": BrandVueApi.MainQuestionType.SingleChoice,
        "ValueMetric": BrandVueApi.MainQuestionType.Value,
        "CustomVariableMetric": BrandVueApi.MainQuestionType.CustomVariable
    };

    const variables = [
        mockVariable(1, testVariableName)
    ];

    const entityConfiguration = mockEntityConfiguration();

    const mockCompositeVariableComponent = (fromVariableIdentifier, resultEntityTypeNames, fromEntityTypeName) => {
        return new InstanceListVariableComponent( {
            fromEntityTypeName: fromEntityTypeName,
            fromVariableIdentifier: fromVariableIdentifier,
            instanceIds: [],
            operator: InstanceVariableComponentOperator.Or,
            resultEntityTypeNames: resultEntityTypeNames
        });
    };

    test("should return DateRangeVariableComponent for WAVE_PERIOD_MEASURE_NAME", () => {
        const metric = mockMetric(WAVE_PERIOD_MEASURE_NAME, BrandVueApi.CalculationType.Average, 1, testVariableName, []);
        const result = getEmptyVariableComponentForMetric(metric, questionTypeLookup, variables, entityConfiguration);
        expect(result).toBeInstanceOf(BrandVueApi.DateRangeVariableComponent);
    });

    test("should return SurveyIdVariableComponent for SURVEY_ID_MEASURE_NAME", () => {
        const metric = mockMetric(SURVEY_ID_MEASURE_NAME, BrandVueApi.CalculationType.Average, 1, testVariableName, []);
        const result = getEmptyVariableComponentForMetric(metric, questionTypeLookup, variables, entityConfiguration);
        expect(result).toBeInstanceOf(BrandVueApi.SurveyIdVariableComponent);
    });

    test("should return InclusiveRangeVariableComponent for numeric answer", () => {
        const metric = mockMetric(metricName, BrandVueApi.CalculationType.Average, 1, testVariableName, []);
        const result = getEmptyVariableComponentForMetric(metric, questionTypeLookup, variables, entityConfiguration);
        expect(result).toBeInstanceOf(BrandVueApi.InclusiveRangeVariableComponent);
    });

    test("should return InstanceListVariableComponent for SingleChoice question type", () => {
        const metric = mockMetric(metricName, BrandVueApi.CalculationType.Average, 1, testVariableName, [createEntities(1)]);
        const result = getEmptyVariableComponentForMetric(metric, questionTypeLookup, variables, entityConfiguration);
        expect(result).toBeInstanceOf(BrandVueApi.InstanceListVariableComponent);
    });

    test("should return InstanceListVariableComponent for CustomVariable question type", () => {
        const metric = mockMetric("CustomVariableMetric", BrandVueApi.CalculationType.Average, 1, testVariableName, [createEntities(1)]);
        metric.isBasedOnCustomVariable = true;
        const result = getEmptyVariableComponentForMetric(metric, questionTypeLookup, variables, entityConfiguration);
        expect(result).toBeInstanceOf(BrandVueApi.InstanceListVariableComponent);
    });

    test("should return InstanceListVariableComponent for metric based on a custom variable", () => {
        const metric = mockMetric("ValueMetric", BrandVueApi.CalculationType.Average, 1, testVariableName, [createEntities(1)]);
        metric.isBasedOnCustomVariable = true;
        const result = getEmptyVariableComponentForMetric(metric, questionTypeLookup, variables, entityConfiguration);
        expect(result).toBeInstanceOf(BrandVueApi.InstanceListVariableComponent);
    });

    test("should throw error for invalid metric", () => {
        const metric = mockMetric("InvalidMetric", BrandVueApi.CalculationType.Text, 1, testVariableName, []);
        expect(() => getEmptyVariableComponentForMetric(metric, questionTypeLookup, variables, entityConfiguration))
            .toThrowError(`Invalid metric for variable component: InvalidMetric`);
    });

    test("should preselect entity types if compositeVariableComponents contains a preselected component", () => {
        const metric = mockMetric(metricName, BrandVueApi.CalculationType.Average, "variableConfigId", "primaryVariableIdentifier", createEntities(1));
        const variables = [mockVariable(variableConfigId, variableIdentifier)];
        const entityConfiguration = mockEntityConfiguration();
        const currentEntity = "entity1";
        const entityIdentifier = "entity2";
        const compositeVariableComponents = [mockCompositeVariableComponent(variableIdentifier, [entityIdentifier], currentEntity)];

        const result = getEmptyVariableComponentForMetric(metric, questionTypeLookup, variables, entityConfiguration, compositeVariableComponents);

        if (result instanceof InstanceListVariableComponent) {
            expect(result.fromEntityTypeName).toBe("entity1");
            expect(result.resultEntityTypeNames).toEqual(["entity2"]);
        } else {
            throw new Error("Expected result to be an instance of InstanceListVariableComponent");
        }
    });
});