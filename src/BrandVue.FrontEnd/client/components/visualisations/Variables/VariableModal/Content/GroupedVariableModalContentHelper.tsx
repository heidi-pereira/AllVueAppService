import { SingleGroupVariableDefinition, GroupedVariableDefinition, BaseGroupedVariableDefinition, VariableGrouping,
    CompositeVariableComponent, DateRangeVariableComponent, SurveyIdVariableComponent, InstanceListVariableComponent,
    VariableConfigurationModel, 
    AggregationType} from "../../../../../BrandVueApi";
import { VariableGroupWithSample } from "./GroupedVariableModalContent";
import { Metric } from "client/metrics/metric";

interface CreateVariableDefinitionProps {
    newGroups: VariableGroupWithSample[];
    variableDefinition: GroupedVariableDefinition | BaseGroupedVariableDefinition | SingleGroupVariableDefinition;
    variableName: string;
    isBase?: boolean;
    metrics: Metric[];
    variables: VariableConfigurationModel[];
    flattenMultiEntity: boolean;
}

export const CreateVariableDefinition = (props: CreateVariableDefinitionProps) => {
    if (props.flattenMultiEntity) {
        //do we need this?
    }
    
    const filteredGroups = props.newGroups.filter(g => g.group.component !== undefined).map(g => g.group);

    if(isSingleVariableGroupDefinition(props, filteredGroups)) {
        return new SingleGroupVariableDefinition({
            group: filteredGroups[0],
            aggregationType: AggregationType.MaxOfSingleReferenced
        });
    }

    const toEntityTypeName = props.variableDefinition instanceof GroupedVariableDefinition ?
        props.variableDefinition.toEntityTypeName : props.variableName;
    const toEntityTypeNamePlural = props.variableDefinition instanceof GroupedVariableDefinition ?
        props.variableDefinition.toEntityTypeDisplayNamePlural : props.variableName;

    const definition = props.isBase ? new BaseGroupedVariableDefinition() : new GroupedVariableDefinition();
    definition.toEntityTypeDisplayNamePlural = toEntityTypeNamePlural;
    definition.toEntityTypeName = toEntityTypeName;
    definition.groups = [...filteredGroups];

    return definition;
}

const convertGroupToVariableDefinition = (group: VariableGroupWithSample) => {
    return new SingleGroupVariableDefinition({
        group: group.group,
        aggregationType: AggregationType.MaxOfSingleReferenced
    });
}

const isSingleVariableGroupDefinition = (props: CreateVariableDefinitionProps, groups: VariableGrouping[]) => {
    if(!groups){
        return false;
    }

    const isBase = props.isBase;
    const isNotSigleGroup = groups.length !== 1;
    const hasSpecialComponent = groups.some(g =>
        g.component instanceof CompositeVariableComponent ||
        g.component instanceof DateRangeVariableComponent ||
        g.component instanceof SurveyIdVariableComponent
    );

    if (isBase || isNotSigleGroup || hasSpecialComponent) {
        return false;
    }

    if(groups[0].component instanceof InstanceListVariableComponent) {

        const component = groups[0].component;
        const variable = props.variables.find(f => f.identifier == component.fromVariableIdentifier);
        const metric = props.metrics.find(m => m.variableConfigurationId == variable?.id);
        if(metric && metric.entityCombination?.length > 1) {
            return true;
        }
    }

    return false;
}