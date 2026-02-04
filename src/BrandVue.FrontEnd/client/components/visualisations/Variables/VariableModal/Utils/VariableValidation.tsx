import * as BrandVueApi from "../../../../../BrandVueApi";
import moment from 'moment';
import {
    BaseFieldExpressionVariableDefinition,
    BaseGroupedVariableDefinition, CompositeVariableComponent, DateRangeVariableComponent,
    FieldExpressionVariableDefinition,
    GroupedVariableDefinition,
    InclusiveRangeVariableComponent,
    InstanceListVariableComponent, SingleGroupVariableDefinition, SurveyIdVariableComponent,
    VariableComponent,
    VariableDefinition,
    VariableGrouping
} from "../../../../../BrandVueApi";


const getInstanceListVariableComponentErrorMessage = (component: InstanceListVariableComponent): string | undefined => {
    if (!component.fromEntityTypeName)
        return "No response type detected";

    if (component.instanceIds === undefined || !component.instanceIds.length)
        return "You need to select at least one item";

    return undefined;
}

const getInclusiveRangeVariableComponentErrorMessage = (component: InclusiveRangeVariableComponent): string | undefined => {
    if (component.operator === undefined)
        return "Operator value is invalid"

    const minValueValid = component.min !== undefined && !isNaN(component.min);
    if (!minValueValid)
        return "Min value is invalid";

    if (component.operator === BrandVueApi.VariableRangeComparisonOperator.Between) {
        const maxValueValid = component.max !== undefined && !isNaN(component.max);
        if (!maxValueValid)
            return "Max value is invalid";
    }

    return undefined;
}

const getCompositeVariableComponentErrorMessage = (component: CompositeVariableComponent): string | undefined => {
    if (component.compositeVariableSeparator === undefined)
        return ""

    if (component.compositeVariableComponents.length <= 1 || component.compositeVariableComponents.some(c => !c))
        return "Select options for all conditions";

    for (let i = 0; i < component.compositeVariableComponents.length; i++) {
        const childComponentErrorMsg = getComponentErrorMessage(component.compositeVariableComponents[i]);
        if (childComponentErrorMsg)
            return childComponentErrorMsg;
    }

    return undefined;
}

const getDateRangeVariableComponentErrorMessage = (component: DateRangeVariableComponent): string | undefined => {
    if (component.maxDate === undefined || component.minDate === undefined)
        return "You need a valid start and end date"

    if (moment(component.maxDate).diff(component.minDate, "hour") <= 0) {
        return "Start date must be larger than end date";
    }

    return undefined;
}

const getSurveyIdVariableComponentErrorMessage = (component: SurveyIdVariableComponent): string | undefined => {
    if (!component.surveyIds || component.surveyIds.length < 1) {
        return "Must select at least one survey";
    }
    return undefined;
}


export const getComponentErrorMessage = (component: VariableComponent): string | undefined => {
    if (!component)
        return "Group must have at least one condition";
    switch (true){
        case component instanceof InstanceListVariableComponent:
            return getInstanceListVariableComponentErrorMessage(component as InstanceListVariableComponent)
        case component instanceof InclusiveRangeVariableComponent:
            return getInclusiveRangeVariableComponentErrorMessage(component as InclusiveRangeVariableComponent)
        case component instanceof CompositeVariableComponent:
            return getCompositeVariableComponentErrorMessage(component as CompositeVariableComponent)
        case component instanceof DateRangeVariableComponent:
            return getDateRangeVariableComponentErrorMessage(component as DateRangeVariableComponent)
        case component instanceof SurveyIdVariableComponent:
            return getSurveyIdVariableComponentErrorMessage(component as SurveyIdVariableComponent)
    }
    throw new Error("Validation logic error - unsupported component type");
}


export const getGroupErrorMessage = (group: VariableGrouping, allGroups: VariableGrouping[]) : string | undefined => {
    if (!group.toEntityInstanceName)
        return "Group name must not be empty";

    if (group.toEntityInstanceName.includes("|"))
        return "Group name must not contain a |";

    if (allGroups.filter(g => g.toEntityInstanceName === group.toEntityInstanceName).length > 1)
        return "Each group name must be unique";

    const groupErrorMsg = getComponentErrorMessage(group.component);
    if (groupErrorMsg)
        return `Group ${group.toEntityInstanceName}: ${groupErrorMsg}`;

    return undefined;
}

export const getVariableErrorMessage = (variableName: string, flattenMultiEntity: boolean, variableDefinition?: VariableDefinition) => {
    if ((!variableName || variableName.length <= 3) && !flattenMultiEntity)
        return "Variable name must be more than 3 characters long";

    if (variableDefinition === undefined){
        return "No variable definition created"
    }
    else if (variableDefinition instanceof FieldExpressionVariableDefinition || variableDefinition instanceof  BaseFieldExpressionVariableDefinition){
        if (variableDefinition.expression !== undefined && variableDefinition.expression.trim().length === 0){
            return "Expression must not be empty";
        }
    }
    else if (variableDefinition instanceof GroupedVariableDefinition 
            || variableDefinition instanceof  BaseGroupedVariableDefinition 
            || variableDefinition instanceof SingleGroupVariableDefinition) {
        if (variableDefinition instanceof SingleGroupVariableDefinition && variableDefinition.group == null) {
            return "Variable must have at least one group";
        }

        const groups = variableDefinition instanceof SingleGroupVariableDefinition ? [variableDefinition.group] : variableDefinition.groups

        if (!groups || groups.length < 1) {
            return "Variable must have at least one group";
        }

        for (let i = 0; i < groups.length; i++) {
            const groupErrorMessage = getGroupErrorMessage(groups[i], groups);
            if (groupErrorMessage)
                return groupErrorMessage;
        }
    }
    else {
        throw new Error("Validation logic error - unsupported definition type");
    }
    return undefined
}

export const getIVariableGroupingNameErrorMessage = (newName: string, group: VariableGrouping, allGroups: VariableGrouping[]) => {
    if (!newName)
        return "Group name must not be empty";

    if (newName.includes("|"))
        return "Group name must not contain a |";

    if (allGroups.filter(g => g.toEntityInstanceId !== group.toEntityInstanceId && g.toEntityInstanceName === newName).length > 0)
        return "Each group name must be unique";

    return undefined
}