import React from "react";
import * as BrandVueApi from "../../../../../BrandVueApi";
import {
    BaseGroupedVariableDefinition, CompositeVariableComponent, DateRangeVariableComponent,
    GroupedVariableDefinition, VariableDefinition, SingleGroupVariableDefinition,
    MainQuestionType, SurveyIdVariableComponent,
    VariableConfigurationModel, VariableGrouping,
    VariableSampleResult
} from "../../../../../BrandVueApi";
import {Metric} from "../../../../../metrics/metric";
import {VariableGroupList} from "../Components/VariableGroupList";
import VariableConditionSelector from "../Components/VariableConditionSelector";
import {useEffect} from "react";
import {
    getGroupCountAndSample,
    SURVEY_ID_MEASURE_NAME,
    WAVE_PERIOD_MEASURE_NAME
} from "../Utils/VariableComponentHelpers";
import { getGroupErrorMessage } from "../Utils/VariableValidation";
import { isInfoPageMetric } from "../../../../helpers/SurveyVueUtils";
import { CreateVariableDefinition } from "./GroupedVariableModalContentHelper";

export interface IGroupedVariableModalContentProps {
    isBase?: boolean;
    nonMapFileSurveys: BrandVueApi.SurveyRecord[];
    questionTypeLookup: {[key: string]: MainQuestionType};
    variableDefinition: GroupedVariableDefinition | BaseGroupedVariableDefinition | SingleGroupVariableDefinition;
    setVariableDefinition: (variableDefinition: VariableDefinition) => void;
    metrics: Metric[];
    variableName: string;
    isSurveyGroup: boolean;
    variables: VariableConfigurationModel[];
    variableIdToView: number | undefined;
    hasWarning: boolean;
    groupThatIsEditing: number | undefined;
    subsetId: string;
    flattenMultiEntity: boolean;
    setGroupThatIsEditing: (groupThatIsEditing: number | undefined) => void;
}

export interface VariableGroupWithSample {
    group: VariableGrouping;
    sample: VariableSampleResult[] | undefined;
}

    const waveMeasure = new Metric(null);
    waveMeasure.name = WAVE_PERIOD_MEASURE_NAME;
    waveMeasure.varCode = WAVE_PERIOD_MEASURE_NAME;
    waveMeasure.displayName = WAVE_PERIOD_MEASURE_NAME;
    waveMeasure.entityCombination = [];

    const surveyIdMeasure = new Metric(null);
    surveyIdMeasure.name = SURVEY_ID_MEASURE_NAME;
    surveyIdMeasure.varCode = SURVEY_ID_MEASURE_NAME;
    surveyIdMeasure.displayName = SURVEY_ID_MEASURE_NAME;
    surveyIdMeasure.entityCombination = [];

const GroupedVariableModalContent = (props: IGroupedVariableModalContentProps) => {
    const [groups, setGroups] = React.useState<VariableGroupWithSample[]>([]);
    const groupsWithoutSample = groups.map(g => g.group);
    const defaultActiveGroupValue = -1;
    const [activeGroupId, setActiveGroupId] = React.useState(defaultActiveGroupValue);

    const getVariableDefinitionSignature = (
        variableDefinition: GroupedVariableDefinition | BaseGroupedVariableDefinition | SingleGroupVariableDefinition) => {
        if (!variableDefinition) return '';

        if (variableDefinition instanceof SingleGroupVariableDefinition) {
            const component = variableDefinition.group?.component;
            if (component instanceof BrandVueApi.InstanceListVariableComponent) {
                return `Single-InstanceList:${component.instanceIds?.join(',') ?? ''}`;
            }
            if (component instanceof BrandVueApi.CompositeVariableComponent) {
                return `Single-Composite:${component.compositeVariableComponents?.length ?? 0}`;
            }
            if (component instanceof BrandVueApi.DateRangeVariableComponent) {
                return `Single-DateRange:${JSON.stringify(component)}`;
            }
            if (component instanceof BrandVueApi.SurveyIdVariableComponent) {
                return `Single-SurveyId:${JSON.stringify(component)}`;
            }
            return `Single-Other:${component ? component.constructor.name : 'undefined'}`;
        }

        if (variableDefinition instanceof GroupedVariableDefinition && Array.isArray(variableDefinition.groups)) {
            return variableDefinition.groups.map(g => {
                if (!g?.component) return 'undefined';
                if (g.component instanceof BrandVueApi.InstanceListVariableComponent) {
                    return `InstanceList:${g.component.instanceIds?.join(',') ?? ''}`;
                }
                if (g.component instanceof BrandVueApi.CompositeVariableComponent) {
                    return `Composite:${g.component.compositeVariableComponents?.length ?? 0}`;
                }
                if (g.component instanceof BrandVueApi.DateRangeVariableComponent) {
                    return `DateRange:${JSON.stringify(g.component)}`;
                }
                if (g.component instanceof BrandVueApi.SurveyIdVariableComponent) {
                    return `SurveyId:${JSON.stringify(g.component)}`;
                }
                return `Other:${g.component.constructor.name}`;
            }).join('|');
        }
        return '';
    }

    const variableDefSignature = getVariableDefinitionSignature(props.variableDefinition);

    useEffect(() => {
        const apiGroups = props.variableDefinition instanceof SingleGroupVariableDefinition ?
            [props.variableDefinition.group] : [...(props.variableDefinition.groups ?? [])];
        if (apiGroups.length === 0) {
            addGroup()
        } else {
            const newGroups: VariableGroupWithSample[] = apiGroups.map(g => ({
                group: g,
                sample: undefined
            }));
            setGroups(newGroups);
            if(activeGroupId == defaultActiveGroupValue && apiGroups.length > 0) {
                setActiveGroupId(apiGroups[0].toEntityInstanceId)
            }
        }
    }, [props.metrics, props.variables, variableDefSignature]);

    useEffect(() => {
        let isCancelled = false;
        const debounceTime = 1000;

        const activeGroup = groups.find(g => g.group.toEntityInstanceId == activeGroupId);
        if (
            activeGroup?.group &&
            activeGroup.sample == undefined &&
            getGroupErrorMessage(activeGroup.group, [activeGroup.group]) == undefined
        ) {
            setTimeout(() => {
                if (!isCancelled) {
                    getGroupCountAndSample(props.subsetId, activeGroup.group).then(result => {
                        if (!isCancelled) {
                            updateGroup({
                                group: activeGroup.group,
                                sample: result
                            });
                        }
                    });
                }
            }, debounceTime);
        }

        return () => { isCancelled = true; };
    }, [groups, activeGroupId]);

    const updateGroups = (newGroups: VariableGroupWithSample[]) => {
        setGroups(newGroups);
        const definition = CreateVariableDefinition({
            newGroups: newGroups,
            variableDefinition: props.variableDefinition,
            variableName: props.variableName,
            isBase: props.isBase,
            metrics: props.metrics,
            variables: props.variables,
            flattenMultiEntity: props.flattenMultiEntity
        });
        props.setVariableDefinition(definition);
    }

    const updateGroup = (group: VariableGroupWithSample) => {
        const groupsClone = [...groups]
        const groupIndex = groupsClone.findIndex(g => g.group.toEntityInstanceId === group.group.toEntityInstanceId)
        groupsClone[groupIndex] = {
            group: new VariableGrouping({...group.group}),
            sample: group.sample
        };

        updateGroups(groupsClone)
    }

    const variableIsWave = (groups: VariableGrouping[]) : boolean => {
        if (!groups.length) {
            return false;
        }

        return groups.every(g => g.component instanceof DateRangeVariableComponent);
    }

    const isSurveyIdVariable = (groups: VariableGrouping[]): boolean => {
        if (!groups.length) {
            return false;
        }
        return groups.some(g => g.component instanceof SurveyIdVariableComponent);
    }

    const variableSupportsWaveCondition = (groups : VariableGrouping[]) : boolean => {
        if (!groups.length) {
            return true;
        }

        if (groups.length === 1) {
            return !(groups[0].component instanceof CompositeVariableComponent);
        }

        // If someone keeps adding groups without selecting a condition we want to restrict adding wave condition as it's not supported to mix it with other conditions
        if (groups.filter(g => !g.component).length > 1) {
            return false;
        }

        return groups.every(g => !g.component || g.component instanceof DateRangeVariableComponent);
    }

    const variableSupportsSurveyIdCondition = (groups : VariableGrouping[]) : boolean => {
        if (!props.isSurveyGroup) {
            return false;
        }

        if (groups.length === 1) {
            return !(groups[0].component instanceof CompositeVariableComponent);
        }

        return groups.every(g => !g.component || g.component instanceof SurveyIdVariableComponent);
    }

    const metricValidForVariableBase = (metric: Metric) => {
        var questionType = props.questionTypeLookup[metric.name];
        if (metric.entityCombination.length === 0) {
            return questionType === BrandVueApi.MainQuestionType.Value;
        }

        return (
            questionType != BrandVueApi.MainQuestionType.Text &&
            questionType != BrandVueApi.MainQuestionType.Unknown &&
            questionType != BrandVueApi.MainQuestionType.HeatmapImage
        );
    }

    const getMetricListForDropdown = () => {
        let validMetrics = [...props.metrics].filter(m =>
            m.eligibleForCrosstabOrAllVue &&
            !isInfoPageMetric(m, props.questionTypeLookup) &&
            m.calcType !== BrandVueApi.CalculationType.Text &&
            metricValidForVariableBase(m) &&
            (!props.variableIdToView || props.variableIdToView !== m.variableConfigurationId)
        )

        if (variableIsWave(groupsWithoutSample) && groups.length > 1)
            return Array(waveMeasure);

        if (isSurveyIdVariable(groupsWithoutSample) && groups.length > 1) {
            return Array(surveyIdMeasure);
        }

        if (variableSupportsWaveCondition(groupsWithoutSample)) {
            validMetrics.push(waveMeasure);
        }

        if (variableSupportsSurveyIdCondition(groupsWithoutSample)) {
            validMetrics.push(surveyIdMeasure);
        }
        return validMetrics;
    }

    const getAllMetrics = () => {
        let allMetrics = [...props.metrics];
        if (variableSupportsWaveCondition(groupsWithoutSample)) {
            allMetrics.push(waveMeasure);
        }

        if (variableSupportsSurveyIdCondition(groupsWithoutSample)) {
            allMetrics.push(surveyIdMeasure);
        }
        return allMetrics;
    }

    const AddNewWaveGroup = (newGroup: VariableGrouping) => {
        if (variableIsWave(groupsWithoutSample)) {
            newGroup.component = new DateRangeVariableComponent();
        } else if (isSurveyIdVariable(groupsWithoutSample)) {
            newGroup.component = new SurveyIdVariableComponent();
        } else if (props.metrics.length === 1) {
            //TODO: This is a hack to get around the fact components cant be undefined in the model
            // @ts-ignore
            newGroup.component = undefined;
        }
        return newGroup
    }

    const addGroup = () => {
        if (props.groupThatIsEditing !== undefined){
            return
        }
        const newId = groups.length > 0 ? Math.max(...groupsWithoutSample.map(g => g.toEntityInstanceId)) + 1 : 1;
        let newGroup: VariableGrouping = new VariableGrouping();
        newGroup.toEntityInstanceId = newId;
        newGroup.toEntityInstanceName = `New group ${newId}`;

        if (newId > 1){
            props.setGroupThatIsEditing(newId)
        }

        newGroup = AddNewWaveGroup(newGroup)

        const newGroups = [...groups];
        newGroups.push({
            group: newGroup,
            sample: undefined
        });

        setActiveGroupId(newId)
        updateGroups(newGroups);
    }

    return (
        <div className="net-stage">
            {(!props.isBase) &&
                <VariableGroupList
                    groups={groups}
                    setGroups={updateGroups}
                    activeGroupId={activeGroupId}
                    setActiveGroupId={setActiveGroupId}
                    updateGroup={updateGroup}
                    addGroup={addGroup}
                    groupThatIsEditing={props.groupThatIsEditing}
                    setGroupThatIsEditing={props.setGroupThatIsEditing}
                    flattenMultiEntity={props.flattenMultiEntity}
                />
            }
            <VariableConditionSelector
                isBase={props.isBase}
                activeGroupId={activeGroupId}
                updateGroup={(group) => updateGroup({group: group, sample: undefined})}
                metrics={getMetricListForDropdown()}
                allMetrics={getAllMetrics()}
                nonMapFileSurveys={props.nonMapFileSurveys}
                questionTypeLookup={props.questionTypeLookup}
                hasWarning={props.hasWarning}
                variables={props.variables}
                subsetId={props.subsetId}
                groups={groups}
            />
        </div>
    );
}

export default GroupedVariableModalContent