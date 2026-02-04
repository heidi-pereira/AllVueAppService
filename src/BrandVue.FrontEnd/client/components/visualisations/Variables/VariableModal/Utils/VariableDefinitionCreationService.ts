import {
    BaseFieldExpressionVariableDefinition,
    BaseGroupedVariableDefinition,
    FieldExpressionVariableDefinition,
    GroupedVariableDefinition,
    InclusiveRangeVariableComponent,
    InstanceListVariableComponent,
    MainQuestionType,
    VariableConfigurationModel,
    VariableGrouping
} from "../../../../../BrandVueApi";
import {Metric} from "../../../../../metrics/metric";
import {
    getEmptyVariableComponentForMetric,
    WAVE_PERIOD_MEASURE_NAME
} from "./VariableComponentHelpers";
import {IEntityConfiguration} from "../../../../../entity/EntityConfiguration";


export class VariableDefinitionCreationService {
    private _variables: VariableConfigurationModel[]
    private _questionTypeLookup: {[key: string]: MainQuestionType};
    private _entityConfiguration: IEntityConfiguration;

    constructor(variables: VariableConfigurationModel[], questionTypeLookup: {[key: string]: MainQuestionType}, entityConfiguration: IEntityConfiguration) {
        this._variables = variables;
        this._questionTypeLookup = questionTypeLookup;
        this._entityConfiguration = entityConfiguration;
    }

    public createGroupedVariableDefinition = (isBase?: boolean) => {
        const definition = isBase ? new BaseGroupedVariableDefinition() : new GroupedVariableDefinition()
        definition.groups = []
        definition.toEntityTypeName = ""
        definition.toEntityTypeDisplayNamePlural = ""
        return definition
    }

    public createFieldExpressionVariableDefinition = (isBase?: boolean) => {
        if (isBase){
            const definition = new BaseFieldExpressionVariableDefinition()
            definition.resultEntityTypeNames = [] //this will be calculated on the backend after parsing the expression
            definition.expression = ""
            return definition
        }
        const definition = new FieldExpressionVariableDefinition()
        definition.expression = ""
        return definition
    }

    public getExistingVariableConfiguration = (variableId: number) => {
        const variableToView = this._variables?.find(v => v.id === variableId)
        if (variableToView) {
            return variableToView
        }else {
            throw new Error(`Variable with ID: ${variableId} doesn't exist`);
        }
    }

    public getVariableDefinitionFromMetric = (metric: Metric) => {
        const copiedName = this.getCopiedQuestionName(metric);

        const component = getEmptyVariableComponentForMetric(metric, this._questionTypeLookup, this._variables, this._entityConfiguration);

        let metricGroups:VariableGrouping[] = []
        if (component instanceof InclusiveRangeVariableComponent) {
            const newGroup: VariableGrouping = new VariableGrouping({
                toEntityInstanceId: 1,
                toEntityInstanceName: `Range: ${copiedName}`,
                component: component
            })
            metricGroups.push(newGroup)
        } else if (component instanceof InstanceListVariableComponent) {
            const splitByEntityType = metric.entityCombination.find(t => t.identifier === component.fromEntityTypeName) ?? metric.entityCombination[0];
            const resultEntityTypeNames = metric.entityCombination.filter(t => t.identifier !== splitByEntityType.identifier).map(t => t.identifier);
            const instances = this._entityConfiguration.getAllEnabledInstancesForTypeOrdered(splitByEntityType);
            metricGroups = instances.map((instance, index) => {
                const comp: InstanceListVariableComponent = new InstanceListVariableComponent({
                    ...component,
                    instanceIds: [instance.id],
                    fromEntityTypeName: splitByEntityType.identifier,
                    resultEntityTypeNames: resultEntityTypeNames
                });
                return new VariableGrouping({
                    toEntityInstanceId: index + 1,
                    toEntityInstanceName: instance.name,
                    component: comp
                });
            })
        }

        const definition = new GroupedVariableDefinition()
        definition.groups = metricGroups
            .filter(g => g.component !== undefined);
        return definition
    }

    public createWaveDefinition = (isBase?: boolean) => {
        const waveMeasure = new Metric(null);
        waveMeasure.name = WAVE_PERIOD_MEASURE_NAME;
        waveMeasure.varCode = WAVE_PERIOD_MEASURE_NAME;
        waveMeasure.displayName = WAVE_PERIOD_MEASURE_NAME;
        waveMeasure.entityCombination = [];
        const waveGroup: VariableGrouping = new VariableGrouping({
            toEntityInstanceId: 1,
            toEntityInstanceName: "Wave 1",
            component: getEmptyVariableComponentForMetric(waveMeasure, this._questionTypeLookup, this._variables, this._entityConfiguration)
        });
        const definition = isBase ? new BaseGroupedVariableDefinition() : new GroupedVariableDefinition()
        definition.groups = [waveGroup]
            .filter(g => g.component !== undefined);
        return definition;
    }

    public getCopiedQuestionName(metric: Metric) {
        let text = metric.displayName;
        if (text.length <= 70) {
            return text;
        }
        text = text.substring(0, 70);
        const spaceIndex = text.lastIndexOf(" ");
        if (spaceIndex >= 50) {
            text = text.substring(0, spaceIndex);
        }
        return text;
    }
}