import React from "react";
import { useEffect, useState } from "react";
import { Input } from 'reactstrap';
import Tooltip  from "../components/Tooltip";
import {
    Factory,
    RimWeightingCalculationResult,
    SwaggerException,
    InstanceResultSize,
    UiWeightingConfigurationRoot,
    UiWeightingPlanConfiguration,
    UiWeightingTargetConfiguration,
    RimDimensionSampleSizeParameters,
    WeightingFilterInstance,
    Message,
    ErrorMessageLevel,
    IAverageDescriptor
} from "../BrandVueApi";
import { IGoogleTagManager } from "../googleTagManager";
import { Metric } from "../metrics/metric";
import { isSingleChoiceOrVariable } from "../metrics/metricHelper";
import { IEntityConfiguration } from "../entity/EntityConfiguration";
import Throbber from "../components/throbber/Throbber";
import { NoDataError } from "../NoDataError";
import AddMetricsModal, { QuestionModalOpenSessionStorageKey } from "../components/visualisations/Reports/Modals/AddMetricsModal";
import { EntityInstance } from "../entity/EntityInstance";
import { toast } from 'react-hot-toast';
import { toPercentage, preciseSum } from "../helpers/MathHelper";
import WarningBanner from "../components/visualisations/WarningBanner";
import WeightingSchemeConfigurationPage from "./WeightingSchemeConfigurationPage";
import DeleteModal from "../components/DeleteModal";
import UpdateModal from "../components/UpdateModal";
import SortableTree from "react-sortable-tree";
import CopyToSiblingsModal from "../components/visualisations/Settings/Weighting/Modals/CopyToSiblingsModal";
import { MetricSet } from "../metrics/metricSet";
import { PageHandler } from "../components/PageHandler";
import WeightingTreeDropDown from "./WeightingPlansControls/WeightingTreeDropDown";
import WeightingButton, { WeightingButtonSimple } from "./WeightingPlansControls/WeightingButton";
import { VariableProvider } from "../components/visualisations/Variables/VariableModal/Utils/VariableContext";
import DisplayWeightingStats from "./WeightingPlansControls/DisplayWeightingStats";
import WeightingDropDown from "./WeightingPlansControls/WeightingDropDown";
import { saveFile } from "../helpers/FileOperations";
import style from './WeightingPlansConfigurationPage.module.less'
import { ProductConfigurationContext } from "../ProductConfigurationContext";
import { maxWeightWarning, minWeightWarning, minWeightBelowThreshold, maxWeightAboveThreshold, savedWeightingNameSessionStorageKey, mostRecentNodeIdSessionStorageKey } from "../components/visualisations/Settings/Weighting/WeightingHelper";
import {useReadVueQueryParams} from "../components/helpers/UrlHelper";

export type UiRimCategory = {
    instance: EntityInstance;
    sampleSize: number;
    targetPercentage: number | undefined;
    targetPopulation: number | undefined;
    warningText: string | undefined;
}

export type UiRimDimension = {
    metric: Metric;
    instanceResults: UiRimCategory[];
};

type MetricInstanceTargets = {
    metric: Metric;
    initialCategoryTargets?: Map<string, number | undefined>;
    initialTargetPopulations?: Map<string, number | undefined>;
};

export type Node = {
    id: string;
    title: string | null;
    children: Node[] | null;
    expanded: boolean | undefined;
    data: PlanData | TargetData | null;
    tooltip: string;
    synthetic: boolean;
}

type PlanData = {
    planDatabaseId: number;
    variableIdentifier: string | null;
    isWeightingGroupRoot: boolean | null;
    planConfiguration: UiWeightingPlanConfiguration;
    parent: TargetData | null;
}

type WeightingFilterInstanceDescription = {
    filterMetricName: string;
    filterInstance: EntityInstance|null;
}

export type TargetData = {
    targetDatabaseId: number;
    entityInstance: EntityInstance | null;
    target: number | null;
    targetPopulation: number | null;
    targetConfiguration: UiWeightingTargetConfiguration;
    parent: PlanData | null;
}

const maxNumberOfAllowableQuotaCells = 200000;
const efficiencyScoreWarning = 0.7;
const treeScaffoldWidthPx = 32;
const treeRowHeightPx = 32;
const treeMaxDepth = 20;

export const TargetDataFromNode = (node: Node|null): TargetData | null => {
    if (node?.data && ('target' in node?.data)) {
        return node.data;
    }
    return null;
}

const PlanData = (data: PlanData | TargetData): PlanData | null => {
    if (('variableIdentifier' in data)) {
        return data;
    }
    return null;
}

const PlanDataFromNode = (node: Node | null): PlanData | null => {
    if (node?.data) {
        return PlanData(node.data);
    }
    return null;
}

const toRimWeightingPlan = (original: UiWeightingConfigurationRoot, dimensions: UiRimDimension[]): UiWeightingConfigurationRoot => {
    const newPlans : UiWeightingPlanConfiguration[] = [];
    dimensions.forEach(dimension => {
        const newPlan = new UiWeightingPlanConfiguration();
        newPlan.variableIdentifier = dimension.metric.name;
        newPlan.uiChildTargets = dimension.instanceResults.map(ir => {
            const item = new UiWeightingTargetConfiguration();
            item.entityInstanceId = ir.instance.id;
            item.target = ir.targetPercentage ? ir.targetPercentage / 100 : ir.targetPercentage;
            item.targetPopulation = ir.targetPopulation;
            return item;
        });
        newPlans.push(newPlan);
    });

    return new UiWeightingConfigurationRoot({ uiWeightingPlans: newPlans, subsetId: original.subsetId });
}

const isSingleQuestionAllNotSet = (dimensionsInScheme: UiRimDimension[]) => {
    return dimensionsInScheme.length == 1 && dimensionsInScheme.every(dimension => {
        return dimension.instanceResults.every(r => r.targetPercentage == undefined && r.targetPopulation == undefined);
    });
}

const isDimensionsValid = (dimensionsInPlan: UiRimDimension[]) => {
    return dimensionsInPlan.length > 0 && dimensionsInPlan.every(dimension => {
        const dimensionTargets = dimension.instanceResults.map(r => r.targetPercentage);
        return preciseSum(dimensionTargets) === 100 ||
            dimension.instanceResults.every(r => r.targetPopulation != undefined || r.sampleSize === 0);
    });
}

const getFilterInstances = (entityConfiguration: IEntityConfiguration, metrics: Metric[], filterMetricName: string | undefined) => {
    if (!filterMetricName) return {
        filterMetric: null,
        entityInstances: [] as EntityInstance[]
    };
    const filterMetric = metrics.find(m => m.name === filterMetricName);

    if (!filterMetric) {
        throw Error(`Can't find filter metric ${filterMetricName}`);
    }
    return {
        filterMetric: filterMetric,
        entityInstances: entityConfiguration.getAllEnabledInstancesForType(filterMetric.entityCombination[0])
    };
};

const toFilterInstances = (instances: WeightingFilterInstance[], data: TargetData|null) =>
{
    if (data && data.entityInstance && data.parent && data.parent.variableIdentifier) {

        const myParentPlan = data.parent;

        let item = new WeightingFilterInstance();
        item.filterInstanceId = data.entityInstance!.id;
        item.filterMetricName = myParentPlan.variableIdentifier!;
        instances.push(item);
        toFilterInstances(instances, myParentPlan.parent);
    }
}

const toFilterInstancesForPlan = (instances: WeightingFilterInstance[], data: PlanData | null) => {
    if (data && data.parent) {
        const myPlan = data;
        toFilterInstances(instances, myPlan.parent);
    }
}

const toFilterDescriptionInstances = (instances: WeightingFilterInstanceDescription[], data: TargetData | null) => {
    if (data && data.entityInstance && data.parent && data.parent.variableIdentifier) {

        const myParentPlan = data.parent;
        const item: WeightingFilterInstanceDescription = {
            filterMetricName : myParentPlan.variableIdentifier!,
            filterInstance : data.entityInstance,
        };
        instances.push(item);
        toFilterDescriptionInstances(instances, myParentPlan.parent);
    }
}

const toFilterDescriptionInstancesForPlan = (instances: WeightingFilterInstanceDescription[], myParentPlan: PlanData | null) => {
    if (myParentPlan) {
        toFilterDescriptionInstances(instances, myParentPlan.parent);
    }
}

const toUiRimDimension = (instanceSamples: InstanceResultSize[], metricInstanceTargets: MetricInstanceTargets): UiRimDimension => {
    return {
        metric: metricInstanceTargets.metric,
        instanceResults: instanceSamples.map(i => ({
            instance: EntityInstance.convertInstanceFromApi(i.entityInstance),
            sampleSize: i.result,
            targetPercentage: metricInstanceTargets.initialCategoryTargets?.get(i.entityInstance.id.toString()),
            targetPopulation: metricInstanceTargets.initialTargetPopulations?.get(i.entityInstance.id.toString()),
            warningText: undefined,
        }))
    };
}

const toMetricInstancesForNewMetrics = (availableMetrics: Metric[]): MetricInstanceTargets[] => {

    return availableMetrics.map(metric => {
        return { metric, metricInstanceTargets: null } as MetricInstanceTargets;
    });

}
const toMetricsInstancesTargetsPlan = (availableMetrics: Metric[], weightingPlans: UiWeightingPlanConfiguration[]): MetricInstanceTargets[] => {
    return weightingPlans.map(plan => {
        const metric = availableMetrics.find(m => m.name === plan.variableIdentifier)!;
        const categoryTargetsLookup = new Map<string, number | undefined>();
        const targetPopulationsLookup = new Map<string, number | undefined>();
        plan.uiChildTargets.forEach(target => {
            categoryTargetsLookup.set(target.entityInstanceId.toString(), target.target !== undefined ? toPercentage(target.target) : undefined);
            targetPopulationsLookup.set(target.entityInstanceId.toString(), target.targetPopulation !== undefined ? target.targetPopulation : undefined);
        });
        return { metric: metric, initialCategoryTargets: categoryTargetsLookup, initialTargetPopulations: targetPopulationsLookup };
    });
};

const GetQuestionNameForIdentifier = (identifier: string, metrics: Metric[]): string => {
    const planMetric = metrics.find(x => x.name == identifier);
    const title = planMetric ? planMetric.varCode : identifier;
    return title;
};

const getUnweightedVariableInstancesAsTargets = (weightingPlan: UiWeightingPlanConfiguration, entityConfiguration: IEntityConfiguration, variableMeasure: Metric): UiWeightingTargetConfiguration[] => {
    const weightedInstanceIds = weightingPlan.uiChildTargets.map(ct => ct.entityInstanceId);
    const allInstancesForMeasure = entityConfiguration.getAllEnabledInstancesForType(variableMeasure.entityCombination[0]);
    const unweightedInstances = allInstancesForMeasure.filter(i => !weightedInstanceIds.includes(i.id));

    return unweightedInstances.map(instance => {
        const target = new UiWeightingTargetConfiguration;
        target.entityInstanceId = instance.id;
        return target;
    });
}

const GetPlanNode = (weightingPlan: UiWeightingPlanConfiguration, entityConfiguration: IEntityConfiguration, metrics: Metric[], parent: TargetData | null): Node[] => {
    const planData: PlanData = {
        planDatabaseId: weightingPlan.id,
        variableIdentifier: weightingPlan.variableIdentifier,
        isWeightingGroupRoot: weightingPlan.isWeightingGroupRoot,
        planConfiguration: weightingPlan,
        parent: parent
    }

    const variableMeasure = metrics.find(m => m.name === weightingPlan.variableIdentifier);
    const allChildTargets = variableMeasure
        ? weightingPlan.uiChildTargets.concat(getUnweightedVariableInstancesAsTargets(weightingPlan, entityConfiguration, variableMeasure))
        : weightingPlan.uiChildTargets;

    const targets = allChildTargets.sort((a: UiWeightingTargetConfiguration, b: UiWeightingTargetConfiguration) => a.entityInstanceId - b.entityInstanceId)
        .map(p => GetTargetNode(p, entityConfiguration, metrics, weightingPlan.variableIdentifier, planData));

    const haveChildren = targets.filter(t => t.children && t.children.length > 0).length > 0 && parent != null;
    if (haveChildren) {
        if (weightingPlan.uiChildTargets.every(x => x.target == undefined && x.targetPopulation == undefined)) {
            return targets;
        }
    }

    const title = GetQuestionNameForIdentifier(weightingPlan.variableIdentifier, metrics);
    const plan: Node = {
        id: `plan-${weightingPlan.id}`,
        title: title,
        expanded: false,
        children: targets,
        data: planData,
        tooltip: `Question: ${title}`,
        synthetic: false,
    }

    return [plan];
}

const GetTargetNode = (weightingTarget: UiWeightingTargetConfiguration, entityConfiguration: IEntityConfiguration, metrics: Metric[], variableIdentifier: string, parent: PlanData|null) => {
    const filterMetricWithInstances = getFilterInstances(entityConfiguration, metrics, variableIdentifier);
    const instance = filterMetricWithInstances &&
        filterMetricWithInstances.entityInstances.find(i => i.id === weightingTarget.entityInstanceId);
    const targetData: TargetData = {
        targetDatabaseId: weightingTarget.id,
        entityInstance: instance ?? null,
        target: weightingTarget.target ?? null,
        targetPopulation: weightingTarget.targetPopulation ?? null,
        targetConfiguration: weightingTarget,
        parent: parent,
    }
    const plans = weightingTarget.uiChildPlans.map(p => GetPlanNode(p, entityConfiguration, metrics, targetData)).flat();
    let tooltip = '';
    if (parent && parent.variableIdentifier) {
        tooltip = `Question: "${GetQuestionNameForIdentifier(parent.variableIdentifier, metrics)}" - `
    }
    if (instance) {
        tooltip += `Selection: "${instance.name}"`;
    }
    const target: Node = {
        id: `target-${weightingTarget.id}`,
        title: (instance ? instance.name : "noname"),
        expanded: false,
        children: plans,
        data: targetData,
        tooltip: tooltip,
        synthetic: false,
    }
    return target;
}

const createTreeFromPlan = (subsetDisplayName: string, weightingPlan: UiWeightingConfigurationRoot, entityConfiguration: IEntityConfiguration, metrics: Metric[]): Node[] => {
    const nodes = weightingPlan.uiWeightingPlans.map(wp => GetPlanNode(wp, entityConfiguration, metrics, null)).flat();
    const target: Node = {
            id: `RIM root`,
            title: `${subsetDisplayName}`,
            expanded: true,
            children: nodes,
            data: null,
            tooltip: `${subsetDisplayName}`,
            synthetic: true,
        }
    return [target];
}

const subPlansForSyntheticNode = (node: Node) => {
    const subPlans: UiWeightingPlanConfiguration[] = [];
    if (node.synthetic && node.children) {
        node.children.forEach(node => {
            const subPlan = PlanDataFromNode(node)?.planConfiguration;
            if (subPlan) {
                subPlans.push(subPlan);
            }
        });
    }
    return subPlans;
}

const getPrimaryButtonText = (metricsToBeAdded: Metric[]) => `Add ${metricsToBeAdded.length === 1 ? "question" : "questions"}`;

const doesPlanForInstanceHaveWeights = (instance: EntityInstance, plan: PlanData | null) : boolean => {
    if (plan) {
        const relatedTarget = plan.planConfiguration.uiChildTargets.find(target => target.entityInstanceId == instance.id);
        if (relatedTarget) {
            return relatedTarget.uiChildPlans == null || relatedTarget.uiChildPlans.length == 0;
        }
        return true;
    }
    return false;
}

const readQuestionModalOpenFlagFromSessionStorage = (): boolean => {
    const questionModalOpen = window.sessionStorage.getItem(QuestionModalOpenSessionStorageKey);
    window.sessionStorage.removeItem(QuestionModalOpenSessionStorageKey);
    return questionModalOpen ? true : false;
}

interface IWeightingPlansConfigurationPageProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    entityConfiguration: IEntityConfiguration;
    subsetId: string;
    subsetDisplayName: string;
    averages: IAverageDescriptor[];
}

const WeightingPlansConfigurationPage: React.FunctionComponent<IWeightingPlansConfigurationPageProps> = (props) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());
    const weightingsPlansClient = Factory.WeightingPlansClient(error => error());
    const metaDataClient = Factory.MetaDataClient(error => error());
    const metricClient = Factory.MetricsClient(error => error());

    const [questionTypeLookup, setQuestionTypeLookup] = React.useState({});
    const [selectedPlan, setSelectedPlan] = useState<UiWeightingConfigurationRoot | null>(null);
    const [tree, setTree] = useState<Node[] | null>(null);
    const [selectedNode, setSelectedNode] = useState<Node | null>(null);
    const [populationSampleSize, setPopulationSampleSize] = React.useState(Number.MAX_SAFE_INTEGER);
    const [dimensionsInPlan, setDimensionsInPlan] = useState<UiRimDimension[]>([]);
    const [validateResult, setValidateResult] = useState<RimWeightingCalculationResult | null>(null);
    const [deleteConfirmationModalVisible, setDeleteConfirmationModalVisible] = React.useState(false);
    const [weightingStatsModalVisible, setWeightingStatsModalVisible] = React.useState(false);
    const [updateConfirmationModalVisible, setUpdateConfirmationModalVisible] = React.useState(false);
    const [addQuestionModalVisible, setAddQuestionModalVisible] = React.useState<boolean>(false);
    const [flattenToRim, setFlattenToRim] = React.useState<boolean>(false);
    const [isLoading, setIsLoading] = useState(false);
    const [isWeightingGroupCheckboxAvailable, setIsWeightingGroupCheckboxAvailable] = useState(true);
    const [isEditing, setIsEditing] = useState(false);
    const [isBusyDeleting, setIsBusyDeleting] = useState(false);
    const [isBusyCopying, setIsBusyCopying] = useState(false);
    const [isCurrentlyLeafNodeOfRim, setIsCurrentlyLeafNodeOfRim] = useState(false);
    const [copyPlanToSiblingsModalVisible, setCopyPlanToSiblingsModalVisible] = React.useState(false);
    const [validationMessages, setValidationMesssages] = useState<Message[] | null>(null);
    const [metrics, setMetrics] = useState<Metric[]>([]);
    const { getQueryParameter } = useReadVueQueryParams();
    const validateAndAddVariableIdentifier = (data: PlanData, variableIdentifiers: string[]) => {
        if (data.variableIdentifier
            && !variableIdentifiers.includes(data.variableIdentifier)) {
            variableIdentifiers.push(data.variableIdentifier);
        }
    }

    const addVariableIdentifierAndCheckParent = (data: PlanData | TargetData, variableIdentifiers: string[]) => {
        const planData = PlanData(data);
        planData && validateAndAddVariableIdentifier(planData, variableIdentifiers);
        data.parent && addVariableIdentifierAndCheckParent(data.parent, variableIdentifiers);
    }

    const addVariableIdentifierAndCheckChildren = (node: Node, variableIdentifiers: string[]) => {
        const planData = PlanDataFromNode(node);
        planData && validateAndAddVariableIdentifier(planData, variableIdentifiers);
        node.children && node.children.forEach(n => addVariableIdentifierAndCheckChildren(n, variableIdentifiers));
    }

    const getVariableIdentifiersForNodeBranch = (node: Node) => {
        const variableIdentifiers: string[] = [];
        if (node) {
            addVariableIdentifierAndCheckChildren(node, variableIdentifiers);
            if (node.data) {
                addVariableIdentifierAndCheckParent(node.data, variableIdentifiers);
            }
        }
        return variableIdentifiers;
    };

    const getValidMetrics = () => {
        if (selectedNode) {
            const existingVariableIdentifiersForNodeBranch = getVariableIdentifiersForNodeBranch(selectedNode);
            return metrics.filter(m =>
                isSingleChoiceOrVariable(m, questionTypeLookup)
                && !existingVariableIdentifiersForNodeBranch.includes(m.name)
                && !dimensionsInPlan.map(s => s.metric.name).includes(m.name)
                && m.originalMetricName == undefined);
        } else {
            return metrics.filter(m => isSingleChoiceOrVariable(m, questionTypeLookup)
                && m.originalMetricName == undefined
                && !dimensionsInPlan.map(s => s.metric.name).includes(m.name)
            );
        }
    }

    const [validMetrics, setValidMetrics] = useState<Metric[]>(getValidMetrics());
    useEffect(() => {
        setValidMetrics(getValidMetrics);
    }, [selectedNode, dimensionsInPlan, metrics])

    const toastError = (userFriendlyText: string) => {
        setIsBusyDeleting(false);
        toast.error(userFriendlyText);
        setIsLoading(false);
    };

    const reloadMetrics = () => {
        metricClient.getMetricsWithDisabledAndBaseDescription(props.subsetId).then(measures => {
            const metrics = MetricSet.mapMeasuresToMetrics(measures);
            setMetrics(metrics);
        });
    }

    const getUiRimDimensionsWithFilters = (subsetId: string, filterInstances: WeightingFilterInstance[], metricInstanceTargets: MetricInstanceTargets[]): Promise<UiRimDimension[]> => {
        const requests = metricInstanceTargets.map(m => {

            const params = new RimDimensionSampleSizeParameters();
            params.metricName = m.metric.name;
            params.instances = filterInstances;
            return weightingAlgorithmsClient.
                getRimDimensionSampleSizesWithFilters(params, subsetId)
                .then(instanceSamples => toUiRimDimension(instanceSamples, m));
        });
        return Promise.all(requests);
    };

    const isNodeLeafForRim = (node: Node): boolean => {
        const planData = PlanDataFromNode(node);
        if (planData) {
            const hasChildPlans = planData.planConfiguration.uiChildTargets.some(t => t.uiChildPlans.length > 0);
            const hasParent = tree ? flattenTree(tree).some(n => n.children && n.children.includes(node)) : false;
            return hasParent && !hasChildPlans;
        }
        const target = TargetDataFromNode(node)
        return target != null;
    }

    const loadForNode = (weightingPlan: UiWeightingConfigurationRoot | null, node: Node | null) => {
        let array: WeightingFilterInstance[] = [];
        if (!weightingPlan || !node) {
            setDimensionsInPlan([]);
            weightingAlgorithmsClient.getTotalSampleSizeWithFilters(props.subsetId, array)
                .then(populationSize => {
                    setPopulationSampleSize(populationSize);
                })
            setIsEditing(false);
            return;
        }

        if (!node.data && !node.synthetic) {
            return;
        }
        const planData = PlanDataFromNode(node);

        if (planData) {
            toFilterInstancesForPlan(array, planData);

            setIsLoading(true);

            setIsCurrentlyLeafNodeOfRim(isNodeLeafForRim(node));

            weightingAlgorithmsClient.getTotalSampleSizeWithFilters(props.subsetId, array)
                .then(populationSize => {
                    setPopulationSampleSize(populationSize);
                })
                .then(() => {
                    const metricsInstancesTargets = toMetricsInstancesTargetsPlan(metrics, [planData.planConfiguration]);
                    getUiRimDimensionsWithFilters(props.subsetId, array, metricsInstancesTargets)
                        .then(dimensions => {
                            setDimensionsInPlan(setDimensionsWithZeroSample(dimensions, [planData.planConfiguration]));
                        });
                    setIsEditing(false);
                })
                .catch((e: Error) => toastError("An error occurred trying to load questions"))
                .finally(() => setIsLoading(false));

            return;
        }

        setIsCurrentlyLeafNodeOfRim(false);
        const targetData = TargetDataFromNode(node);
        toFilterInstances(array, targetData);

        setIsLoading(true);
        weightingAlgorithmsClient.getTotalSampleSizeWithFilters(props.subsetId, array)
            .then(populationSize => {
                setPopulationSampleSize(populationSize);
            })
            .then(() => {
                if (node.synthetic) {
                    const subPlans = subPlansForSyntheticNode(node);
                    const metricsInstancesTargets = toMetricsInstancesTargetsPlan(metrics, subPlans);

                    getUiRimDimensionsWithFilters(props.subsetId, array, metricsInstancesTargets)
                        .then(dimensions => {
                            setDimensionsInPlan(setDimensionsWithZeroSample(dimensions, subPlans));
                        });

                }
                else if (targetData && targetData.targetConfiguration.uiChildPlans && targetData.targetConfiguration.uiChildPlans.length > 0) {
                    const subPlans = targetData.targetConfiguration.uiChildPlans.filter(p => p.uiChildTargets.length > 0);

                    const metricsInstancesTargets = toMetricsInstancesTargetsPlan(metrics, subPlans);

                    getUiRimDimensionsWithFilters(props.subsetId, array, metricsInstancesTargets)
                        .then(dimensions => {
                            setDimensionsInPlan(setDimensionsWithZeroSample(dimensions, subPlans));
                        });
                } else {
                    setDimensionsInPlan([]);
                }
                setIsEditing(false);
            })
            .catch((e: Error) => toastError("An error occurred trying to load questions"))
            .finally(() => setIsLoading(false));
    };

    const addNodesAndChildren = (node: Node, allNodes: Node[]) => {
        allNodes.push(node);
        if (node.children && node.children.length > 0) {
            node.children.forEach(n => addNodesAndChildren(n, allNodes));
        }
    }

    const addParentToNodePath = (node: Node, allNodes: Node[], nodePath: Node[]) => {
        const parentNode = allNodes.find(n => n.children && n.children.includes(node));

        if (parentNode) {
            nodePath.unshift(parentNode);
            addParentToNodePath(parentNode, allNodes, nodePath);
        }
    }

    const expandNodePath = (nodePath: Node[]) => {
        nodePath.forEach(n => n.expanded = true);
    }

    const flattenTree = (tree: Node[]): Node[] => {
        if (tree == null || tree.length === 0) {
            return [];
        }

        const allNodes: Node[] = [];
        tree.forEach(t => addNodesAndChildren(t, allNodes));

        return allNodes;
    }

    const onNodeClick = (node: Node) => {
        setSelectedNode(node);
    };

    const selectNodeInTree = (tree: Node[], node: Node, availableMetrics: Metric[]) => {
        if (tree != null) {
            const nodePath: Node[] = [node];
            addParentToNodePath(node, tree, nodePath);
            expandNodePath(nodePath);
        }
    }

    const findDefaultNode = (tree: Node[]):Node => {
        const firstNode = tree?.find(n => n.children && n.children.length > 0);
        return firstNode ?? tree[0];
    }

    const saveNodeIdToSessionStorage = (nodeId: string) => {
        window.sessionStorage.setItem(mostRecentNodeIdSessionStorageKey, JSON.stringify(nodeId));
    }

    const readNodeFromSessionStorage = (tree: Node[]): Node | null => {
        const savedNodeId = window.sessionStorage.getItem(mostRecentNodeIdSessionStorageKey);
        window.sessionStorage.removeItem(mostRecentNodeIdSessionStorageKey);
        const mostRecentNodeId = savedNodeId !== null ? JSON.parse(savedNodeId) : undefined;
        const mostRecentNode = mostRecentNodeId && tree.find(n => n.id === mostRecentNodeId);
        return mostRecentNode ? mostRecentNode : null;
    }

    const saveNodeNameToSessionStorage = (nodeName: string | undefined) => {
        window.sessionStorage.setItem(savedWeightingNameSessionStorageKey, nodeName ?? "");
    }

    const findSpecifiedNode = (tree: Node[]): Node | null => {
        const specifiedNodeId = getQueryParameter<number>('nodeId');
        let specifiedNode = specifiedNodeId && tree.find(n => n.id === `target-${specifiedNodeId}`);
        if (!specifiedNode) {
            const instanceId = getQueryParameter<number>('waveInstanceId');
            specifiedNode = instanceId && tree.find(n => {
                const td = TargetDataFromNode(n);
                if (td && td.parent?.parent == null) {
                    if (td.entityInstance?.id == instanceId) {
                        return true;
                    }
                }
                return false;
            }
            );
        }
        return specifiedNode ? specifiedNode : null;
    }

    const loadForSubset = (subsetId: string) => {
        metricClient.getMetricsWithDisabledAndBaseDescription(subsetId).then(measures => {
            const metrics = MetricSet.mapMeasuresToMetrics(measures);
            setMetrics(metrics);
            metaDataClient.getQuestionTypes(subsetId)
            .then(lookup => setQuestionTypeLookup(lookup))
            .catch((e: Error) => toastError("An error occurred trying to load question type lookups"));

        weightingsPlansClient.isWeightingPlanDefinedAndValid(subsetId).then(validation => {
        setValidationMesssages(validation.messages);

        weightingsPlansClient
            .getWeightingPlan(subsetId)
            .then(plan => {
                const treeFromPlan = createTreeFromPlan(props.subsetDisplayName, plan, props.entityConfiguration, metrics);
                const flattenedTree = flattenTree(treeFromPlan);
                const initialNode = readNodeFromSessionStorage(flattenedTree) ?? findSpecifiedNode(flattenedTree) ?? findDefaultNode(flattenedTree);
                selectNodeInTree(flattenedTree, initialNode, metrics);
                setSelectedPlan(plan);
                setTree(treeFromPlan);
                setSelectedNode(initialNode);
            })
            .catch((e: NoDataError) => {
                toastError("No weighting found for this survey segment." + e.message);
                setDimensionsInPlan([]);
                setSelectedPlan(null);
                setIsLoading(false);
            })
            .catch((e: Error) => toastError("An error has occurred trying to get weighting"));
        });
        });
    }

    const exportWeights = (e: React.MouseEvent, average: IAverageDescriptor, selectedNode: Node | null) => {

        e.stopPropagation();
        if (selectedPlan) {
            const targetData = TargetDataFromNode(selectedNode);
            const weightingFilterInstances: WeightingFilterInstance[] = [];
            targetData ? toFilterInstances(weightingFilterInstances, targetData) : toFilterInstancesForPlan(weightingFilterInstances, PlanDataFromNode(selectedNode));

            const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());
            const filterPartOfFileName = descriptionForNode(selectedNode).replace("/", "- ");
            return weightingAlgorithmsClient.exportRespondentWeightsForSubset(selectedPlan.subsetId, average.averageId, weightingFilterInstances)
                .then(r => saveFile(r, `Weightings- ${selectedPlan.subsetId}- (${average.displayName}- ${filterPartOfFileName})- Private.csv`))
                .catch(error => {
                    toast.error("Export failed");
                });
        }
    }

    const validateNodeAndSetCalculationResults = (dimensions: UiRimDimension[], selectedNode: Node | null) => {
        setValidateResult(null);
        if (selectedPlan && isDimensionsValid(dimensions)) {
            const targetData = TargetDataFromNode(selectedNode);
            const weightingFilterInstances: WeightingFilterInstance[] = [];

            targetData ? toFilterInstances(weightingFilterInstances, targetData) : toFilterInstancesForPlan(weightingFilterInstances, PlanDataFromNode(selectedNode));

            const rimWeightingRoot = toRimWeightingPlan(selectedPlan, dimensions);

            weightingAlgorithmsClient.validateRimWeightingPartialRoot(rimWeightingRoot, weightingFilterInstances)
                .then(result => {
                    setValidateResult(result);
                })
                .catch((e: SwaggerException) => {
                    let errorMessage = "An error occurred trying to validate weighting.";
                    const extraErrorContext = e.response ? JSON.parse(e.response).message : null;
                    errorMessage = extraErrorContext ? `${errorMessage} ${extraErrorContext}` : errorMessage;
                    return toastError(errorMessage);
                });
        }
    };

    useEffect(() => {
        loadForSubset(props.subsetId);
    }, [props.subsetId]);

    useEffect(() => {
        loadForNode(selectedPlan, selectedNode);
        if (readQuestionModalOpenFlagFromSessionStorage()) {
            setAddQuestionModalVisible(true);
        }
    }, [selectedNode]);

    useEffect(() => {
        validateNodeAndSetCalculationResults(dimensionsInPlan, selectedNode);
    }, [dimensionsInPlan]);

    if (!selectedPlan) {
        return (
            <div className="configuration-page">
                <div className="throbber-container-fixed">
                    <Throbber />
                </div>
            </div>
        );
    }

    const deleteFromWeightingPlan = () => {
        if (selectedNode?.synthetic) {
            props.googleTagManager.addConfigurationEvent('weightingsConfigureDelete');
            weightingsPlansClient.deleteWeightingPlanForSubset(selectedPlan.subsetId).then(() => loadForSubset(props.subsetId))
                .catch((e: Error) => toastError("An error occurred trying to delete weighting target"));
        }
        else {
            props.googleTagManager.addConfigurationEvent('weightingsConfigureDeleteTarget');
            const targetNode = TargetDataFromNode(selectedNode);
            const planNode = PlanDataFromNode(selectedNode);

            if (planNode || targetNode) {
                let planDatabaseId: number | null = null;
                let targetDatabaseId: number | null = null;

                if (targetNode) {
                    targetDatabaseId = targetNode.targetDatabaseId;
                    if (targetNode.parent) {
                        planDatabaseId = targetNode.parent.planDatabaseId;
                    }
                    selectedNode && saveNodeIdToSessionStorage(selectedNode.id);
                }
                else if (planNode) {
                    planDatabaseId = planNode.planDatabaseId;
                    const parentNode = selectedNode && getParentNode(selectedNode);
                    parentNode && saveNodeIdToSessionStorage(parentNode.id);
                }
                if (targetDatabaseId && planDatabaseId) {
                    weightingsPlansClient.deleteWeightingTarget(selectedPlan.subsetId, planDatabaseId, targetDatabaseId)
                        .then(() => loadForSubset(props.subsetId))
                        .catch((e: Error) => toastError("An error occurred trying to delete weighting target"));

                }
                else if (planDatabaseId) {
                    weightingsPlansClient.deleteWeightingPlan(planDatabaseId)
                        .then(() => loadForSubset(props.subsetId))
                        .catch((e: Error) => toastError("An error occurred trying to delete weighting"));
                }
            }
        }
    };

    const updateWeightingPlan = () => {
        if (dimensionsInPlan.length == 0) {
            deleteFromWeightingPlan();
        }
        else if (isDimensionsValid(dimensionsInPlan) || isSingleQuestionAllNotSet(dimensionsInPlan)) {
            if (selectedPlan) {
                const plansToDelete: UiWeightingPlanConfiguration[] = [];
                let plansExisting: UiWeightingPlanConfiguration[] | null = [];
                let targetData: TargetData | null = null;
                let targetDatabaseId: number | null = null;
                let planDatabaseId: number | null = null;

                if (selectedNode) {
                    if (selectedNode.synthetic) {
                        plansExisting = subPlansForSyntheticNode(selectedNode)
                    }
                    else if (selectedNode.data) {
                        targetData = TargetDataFromNode(selectedNode);
                        if (targetData) {
                            targetDatabaseId = targetData.targetDatabaseId;
                            plansExisting = targetData.targetConfiguration.uiChildPlans;
                            if (targetData.parent) {
                                planDatabaseId = targetData.parent.planDatabaseId;
                            }
                        }
                        else {
                            const planData = PlanDataFromNode(selectedNode);
                            if (planData) {
                                planDatabaseId = planData.planDatabaseId;
                                targetData = planData.parent;
                                plansExisting.push(planData.planConfiguration);
                            }
                        }
                    }
                    saveNodeIdToSessionStorage(selectedNode.id);
                    saveNodeNameToSessionStorage(selectedNode.title!);
                }
                const isNodeRimWeighted = plansExisting?.length >= 2;
                //Update the existing
                plansExisting?.forEach(plan => {
                    const updated = dimensionsInPlan.find(dim => dim.metric.name == plan.variableIdentifier);
                    if (updated) {
                        updated.instanceResults.forEach(result => {
                            let targetToUpdate = plan.uiChildTargets.find(target => target.entityInstanceId == result.instance.id);
                            if (!targetToUpdate) {
                                if (isNodeRimWeighted || (result.targetPercentage != undefined) || (result.targetPopulation != undefined)) {
                                    var validMetric = metrics.find(x => x.name == plan.variableIdentifier);
                                    if (validMetric) {
                                        const combinations = props.entityConfiguration.getAllEnabledInstancesForType(validMetric.entityCombination[0]);
                                        const ids = combinations.map(x => x.id);
                                        targetToUpdate = plan.uiChildTargets.find(target => !ids.includes(target.entityInstanceId));
                                        if (targetToUpdate) {
                                            //Recycle an invalid one
                                            targetToUpdate.entityInstanceId = result.instance.id;
                                        }
                                        else {
                                            //Add a new one to the plan as there is nothing for me to add to...
                                            targetToUpdate = new UiWeightingTargetConfiguration();
                                            targetToUpdate.entityInstanceId = result.instance.id;
                                            plan.uiChildTargets.push(targetToUpdate);
                                        }
                                    }
                                }
                            }
                            if (targetToUpdate) {
                                targetToUpdate.target = result.targetPercentage ? result.targetPercentage / 100 : result.targetPercentage;
                                targetToUpdate.targetPopulation = result.targetPopulation;
                            }
                        })
                    }
                    else {
                        plansToDelete.push(plan)
                    }
                });

                //Delete any items
                plansToDelete.forEach(planToDelete => {
                    const index = plansExisting?.findIndex(plan => plan.id == planToDelete.id);
                    if (index != undefined) {
                        plansExisting?.splice(index, 1);
                    }
                });

                //Add any items
                dimensionsInPlan.forEach(dimension => {
                    if (!plansExisting?.find(plan => plan.variableIdentifier == dimension.metric.name)) {
                        const plan = new UiWeightingPlanConfiguration();
                        plan.variableIdentifier = dimension.metric.name;
                        plan.uiChildTargets = dimension.instanceResults.map(ir => {
                            const item = new UiWeightingTargetConfiguration();
                            item.entityInstanceId = ir.instance.id;
                            item.target = ir.targetPercentage ? ir.targetPercentage / 100 : ir.targetPercentage;
                            item.targetPopulation = ir.targetPopulation;
                            return item;
                        });
                        plansExisting!.push(plan);
                    }
                });

                if (planDatabaseId == null && targetData == null) {
                    if (plansExisting) {
                        const newOne = new UiWeightingConfigurationRoot({ uiWeightingPlans: plansExisting, subsetId: selectedPlan.subsetId });
                        props.googleTagManager.addConfigurationEvent('weightingsConfigureCreate');
                        weightingsPlansClient.createWeightingPlan(newOne).
                            catch((e: Error) => { toastError(`An error occurred trying to createe weighting`) });
                    }
                    else {
                        toastError('Failed Existing plans are null!');
                    }
                }
                else if (planDatabaseId && targetDatabaseId == null) {

                    if (targetData && targetData.parent && targetData.parent.planConfiguration.uiChildTargets) {
                        const targetToUpdate = targetData?.parent?.planConfiguration.uiChildTargets.find(target => target.id == targetDatabaseId);
                        weightingsPlansClient.createWeightingTarget(planDatabaseId, targetData.targetConfiguration).
                            catch((e: Error) => { toastError(`An error occurred trying to update weighting`) });
                    }
                    else if (plansExisting && plansExisting.length == 1) {
                        props.googleTagManager.addConfigurationEvent('weightingsConfigureUpdate');
                        weightingsPlansClient.updateWeightingPlan(planDatabaseId, plansExisting[0]).
                            catch((e: Error) => { toastError(`An error occurred trying to update weighting`) });
                    }
                    else {
                        toastError('Incorrect number of items to update (expecting a single item)');
                    }
                }
                else if (targetData && targetData.parent && targetData.parent.planConfiguration.uiChildTargets) {
                    const targetToUpdate = targetData.parent.planConfiguration.uiChildTargets.find(target => target.id == targetDatabaseId);
                    if (targetToUpdate && planDatabaseId && targetDatabaseId) {
                        props.googleTagManager.addConfigurationEvent('weightingsConfigureUpdate');
                        weightingsPlansClient.updateWeightingTarget(planDatabaseId, targetDatabaseId, targetToUpdate).
                            catch((e: Error) => { toastError(`An error occurred trying to update weighting (target)`) });
                    }
                    else {
                        toastError('Failed to find target to Update');
                    }
                }
                else
                    toastError('Failed to find target parent');
            }
            else {
                toastError('No Plan!');
            }
        }
        else {
            toastError('Dimensions not valid');
        }
    };

    const updateWeightingPlanWithPossiblePopup = () => {
        if (validateResult && (validateResult.efficiencyScore < efficiencyScoreWarning || maxWeightAboveThreshold(validateResult.maxWeight) || minWeightBelowThreshold(validateResult.minWeight))) {
            setUpdateConfirmationModalVisible(true);
        }
        else {
            updateWeightingPlan();
        }
    }

    const setNewDimensions = (dimensions: UiRimDimension[], plans: UiWeightingPlanConfiguration[] | null) => {
        setDimensionsInPlan(setDimensionsWithZeroSample(dimensions, plans));
        if (!isEditing) setIsEditing(true);
    };

    function addQuestionForMetric(selectedPlans: UiWeightingPlanConfiguration[] | null,
        filterInstances: WeightingFilterInstance[],
        metricsToAdd: Metric[],
        startingDimensions: UiRimDimension[]) {
            const metricsInstancesTargets = toMetricInstancesForNewMetrics(metricsToAdd);
            getUiRimDimensionsWithFilters(props.subsetId, filterInstances, metricsInstancesTargets)
                .then(dimensions => {
                    const newDimensions = startingDimensions.concat(dimensions);
                    const totalNumberOfQuotaCells = newDimensions.reduce((accumulator, current) => accumulator * current.instanceResults.length, 1);
                    //greater than or equal to accounts for the extra 1 unweighted cell
                    if (totalNumberOfQuotaCells >= maxNumberOfAllowableQuotaCells) {
                        toastError(`Failed to add question(s) as there would contain too many categories. It would yield more than the maximum ${maxNumberOfAllowableQuotaCells.toLocaleString()} quota cells.`);
                    } else {
                        setNewDimensions(newDimensions, selectedPlans);
                    }
                }
            );
    }

    const addQuestionsToPlan = (metricsToAdd: Metric[]) => {
        if (selectedPlan.uiWeightingPlans.length == 0) {
            addQuestionForMetric(null, [], metricsToAdd, []);
            return;
        }

        if (!validForRimWeighting(selectedNode) && (dimensionsInPlan.length + metricsToAdd.length > 1)) {
            toastError("Cannot add multiple questions here as Rim weighting already applied elsewhere in tree");
            return;
        }

        const data = TargetDataFromNode(selectedNode);
        const isAvailable = (data && data.targetConfiguration.uiChildPlans) || selectedNode?.synthetic;
        if (isAvailable) {
            const copy = [...dimensionsInPlan];
            let array: WeightingFilterInstance[] = [];
            toFilterInstances(array, data);
            addQuestionForMetric(selectedPlan.uiWeightingPlans, array, metricsToAdd, copy);
        }
        else {
            addQuestionForMetric(null, [], metricsToAdd, []);
        }
    };

    const removeQuestionFromPlan = (dimension: UiRimDimension) => {
        const copy = [...dimensionsInPlan];
        const remainingDimensions = copy.filter(r => r.metric.name !== dimension.metric.name);
        setNewDimensions(remainingDimensions, null);
    };

    const updateQuestionInPlanTargetPercentage = (dimension: UiRimDimension, categoryWithTarget: UiRimCategory, targetPercentage: number | undefined) => {
        const copy = [...dimensionsInPlan];

        const findDimension = copy.find(r => r.metric.name === dimension.metric.name);
        if (findDimension) {
            findDimension.instanceResults.forEach(r => r.targetPopulation = undefined);
            const findCategoryWithTarget = findDimension.instanceResults.find(r => r.instance.id === categoryWithTarget.instance.id);
            if (findCategoryWithTarget) {
                findCategoryWithTarget.targetPercentage = targetPercentage;
                setNewDimensions(copy, null);
            }
            else {
                toastError(`Failed to find metric ${dimension.metric.name} instance ${categoryWithTarget.instance.id}(${categoryWithTarget.instance.name})`);
            }
        }
        else {
            toastError(`Failed to find metric ${dimension.metric.name}  in dimension`);
        }
    };

    const updateQuestionInPlanTargetPopulation = (dimension: UiRimDimension, categoryWithTarget: UiRimCategory, targetPopulation: number | undefined) => {
        const copy = [...dimensionsInPlan];

        const findDimension = copy.find(r => r.metric.name === dimension.metric.name);
        if (findDimension) {
            findDimension.instanceResults.forEach(r => r.targetPercentage = undefined);
            const findCategoryWithTarget = findDimension.instanceResults.find(r => r.instance.id === categoryWithTarget.instance.id);
            if (findCategoryWithTarget) {
                findCategoryWithTarget.targetPopulation = targetPopulation;
                setNewDimensions(copy, null);
            }
            else {
                toastError(`Failed to find metric ${dimension.metric.name} instance ${categoryWithTarget.instance.id}(${categoryWithTarget.instance.name})`);
            }
        }
        else {
            toastError(`Failed to find metric ${dimension.metric.name}  in dimension`);
        }
    };

    const onTreeChange = (treeData) => {
        setTree(treeData)
    };

    const copyToSiblings = (targetInstanceIds: number[]) => {
        const targetData = TargetDataFromNode(selectedNode);

        if (targetData) {
            props.googleTagManager.addConfigurationEvent('weightingsConfigureCopy');
            weightingsPlansClient.copyWeightingPlansToSiblings(props.subsetId, targetData.targetDatabaseId, flattenToRim ? 1 : 0, targetInstanceIds)
                .then(() => loadForSubset(props.subsetId))
                .catch((e: Error) => toastError("An error occurred trying to update weighting"));
        }
    };

    const descriptionForNode = (node: Node|null) => {
        let array: WeightingFilterInstanceDescription[] = [];
        if (node) {
            if (node.data) {
                const targetData = TargetDataFromNode(node);
                const planData = PlanDataFromNode(node);

                if (targetData) {
                    toFilterDescriptionInstances(array, targetData);
                    array.reverse();
                }
                else if (planData) {
                    toFilterDescriptionInstancesForPlan(array, planData);
                    const item: WeightingFilterInstanceDescription = {
                        filterMetricName: planData.variableIdentifier!,
                        filterInstance: null,
                    };
                    array.reverse();
                    array.push(item);
                }
            }
            else {
                return (`'${node.tooltip}'`);
            }
        }
        return "'" + array.map(i => i.filterInstance ? i.filterInstance.name : GetQuestionNameForIdentifier(i.filterMetricName, metrics)).join("/") + "'";
    }
    const displayPotentialValidationErrors = () => {
        if (validationMessages) {
            const maxWarningsToDisplay = 10;
            const errorMessages = validationMessages.filter(x => x.errorLevel == ErrorMessageLevel.Error).map(x => `${x.messageText}`);
            const errorMessageToDisplay = `Validation Errors (${errorMessages.length}):  ${errorMessages.slice(0, maxWarningsToDisplay).join(".  ")}`
            const warningMessages = validationMessages.filter(x => x.errorLevel == ErrorMessageLevel.Warning).map(x => `${x.messageText}`);
            const warningMessageToDisplay = errorMessages.length >= maxWarningsToDisplay ? `Validation Warnings (${warningMessages.length}):...` : `Validation Warnings (${warningMessages.length}): ${warningMessages.slice(0, maxWarningsToDisplay).join(".  ")}`

            return <>
                {errorMessages.length > 0 &&
                    <WarningBanner message={errorMessageToDisplay} materialIconName="error" />
                }
                {warningMessages.length > 0 &&
                    <WarningBanner message={warningMessageToDisplay} materialIconName="warning" />
                }
            </>
        }
    }

    const setDimensionsWithZeroSample = (dimensions: UiRimDimension[], plans: UiWeightingPlanConfiguration[] | null): UiRimDimension[] => {
        let plansExisting: UiRimDimension[] = []
        dimensions.forEach(dim => {
            const relatedPlan = plans ? plans.find(x => x.variableIdentifier == dim.metric.name) : null;
            let newDim = {
                metric: dim.metric,
                instanceResults: dim.instanceResults.map(i => {
                    let warningText: string | undefined = undefined;
                    if (relatedPlan && i.sampleSize) {
                        if (relatedPlan.uiChildTargets.find(x => x.entityInstanceId == i.instance.id) == undefined) {
                            warningText = `${i.instance.name} has ${i.sampleSize} respondent(s) but no related weighting. These respondents are currently not weighted.`;
                        }
                    }

                    return ({
                        instance: i.instance,
                        sampleSize: i.sampleSize,
                        targetPercentage: (i.sampleSize == 0 && i.targetPercentage != 0) ? undefined : i.targetPercentage,
                        targetPopulation: i.sampleSize == 0 ? undefined : i.targetPopulation,
                        warningText: warningText,
                    })
                })
            };
            plansExisting.push(newDim);
        });
        return plansExisting;
    }

    const onWeightingGroupCheckboxChangeHandler = (planConfiguration: UiWeightingPlanConfiguration, checkboxValue: boolean) => {
        if (planConfiguration) {
            planConfiguration.isWeightingGroupRoot = checkboxValue;

            props.googleTagManager.addConfigurationEvent('weightingsConfigureUpdate');
            weightingsPlansClient.updateWeightingPlan(planConfiguration.id, planConfiguration).
                catch((e: Error) => { toastError(`An error occurred trying to update weighting`) });
            setIsLoading(true);
            setIsWeightingGroupCheckboxAvailable(false);
        }
    }

    const renderBreadCrumbs = (selectedNode: Node | null) => {
        const breadCrumbs = selectedNode?.tooltip;
        if (!isCurrentlyLeafNodeOfRim) {
            let planConfiguration: UiWeightingPlanConfiguration | null = null;
            let planData = PlanDataFromNode(selectedNode);
            const targetData = TargetDataFromNode(selectedNode);
            if (planData) {
                planConfiguration = planData.planConfiguration;
            }
            if (targetData && dimensionsInPlan.length == 1) {
                const plan = targetData.targetConfiguration.uiChildPlans[0];
                if (plan)
                    planConfiguration = plan;
            }
            if (planConfiguration != null) {
                return <div>
                    {breadCrumbs}
                    <div className={`${style.weightingOptions} ${style.option}`} key="set-as-default-input">
                        <Input disabled={!isWeightingGroupCheckboxAvailable} id="set-as-default-input" type="checkbox" className="checkbox weighting-group-checkbox" checked={planConfiguration.isWeightingGroupRoot} onChange={() => onWeightingGroupCheckboxChangeHandler(planConfiguration!, !planConfiguration!.isWeightingGroupRoot)}></Input>
                        <label className="filter-instance-label" htmlFor="set-as-default-input" title=''>
                            Group weightings below this level
                        </label>
                    </div>
                </div>
            }
        }
        return <div>
            {breadCrumbs}</div>;
    }

    const getEfficiencyTooltip = (validResults: RimWeightingCalculationResult | null) => {
        if (validResults) {
            const title = validResults.converged ? (validResults.efficiencyScore < efficiencyScoreWarning ? `CAUTION: Poor efficiency < ${toPercentage(efficiencyScoreWarning, 0)}%` : "OK") : "No convergence";
            return `${title}, number of iterations: ${validResults.iterationsRequired}`;
        }
        return "No valid results";
    }

    const getMinWeightTooltip = (validResults: RimWeightingCalculationResult | null, dims: UiRimDimension[]) => {
        if (validResults) {
            const title = validResults.converged ? (minWeightBelowThreshold(validResults.minWeight) ? `CAUTION: Min weight < ${minWeightWarning}` : "OK") : "No convergence";
            let reason = "";
            if (validResults.minWeight == 0.0) {
                reason = "  Caution: Zero targets set for " + dimensionsInPlan.map(
                    item => item.instanceResults.filter(res => res.sampleSize != 0 && res.targetPercentage == 0.0).
                        map(res => `${item.metric.name}:${res.instance.name}`)).filter(x => x.length > 0).join(",");
            }
            return title + reason;
        }
        return "No valid results"
    }

    const getMaxWeightTooltip = (validResults: RimWeightingCalculationResult | null) => {
        if (validResults) {
            return validResults.converged ? (maxWeightAboveThreshold(validResults.maxWeight) ? `CAUTION: Max weight > ${maxWeightWarning}` : "OK") : "No convergence";
        }
        return "No valid results"
    }

    const isCloneAvailableFor = (node: Node | null) => {
        const targetData = TargetDataFromNode(node);
        const isCloningAvailable = targetData && targetData.entityInstance && targetData.targetConfiguration.uiChildPlans.length > 0;
        return isCloningAvailable != null && isCloningAvailable;
    }

    const displayAddCopyDeleteButtons =
        ((TargetDataFromNode(selectedNode) != null) || selectedNode == null || (selectedNode?.synthetic == true))
        && dimensionsInPlan.length > 0
        && !isCurrentlyLeafNodeOfRim;
    const dimensionTotalSampleSizes = dimensionsInPlan.map(d => preciseSum(d.instanceResults.map(r => r.sampleSize)));
    const inconsistentBaseSizes = dimensionTotalSampleSizes.some(t => t < populationSampleSize);
    const minWeightClass = !validateResult || minWeightBelowThreshold(validateResult.minWeight) ? "warning" : "";
    const maxWeightClass = !validateResult || maxWeightAboveThreshold(validateResult.maxWeight) ? "warning" : "";
    const efficiencyClass = !validateResult || validateResult.efficiencyScore < efficiencyScoreWarning ? "warning" : "";
    const targetData = TargetDataFromNode(selectedNode);
    const copyToSiblingsEnabled = targetData && !isEditing;
    let deleteButtonAvailable = !isCurrentlyLeafNodeOfRim && selectedNode?.data ? true : false;
    const cloneButtonAvailable = isCloneAvailableFor(selectedNode);
    const isRootNode = selectedNode?.data?.parent == null;
    const isExpansionWeightedNode = dimensionsInPlan.some(dim => dim.instanceResults.some(inst => inst.targetPopulation != undefined));

    if (targetData) {
        deleteButtonAvailable = targetData.targetConfiguration.uiChildPlans.length > 0;
    }

    const getCopyToSiblingsModal = () => {
        const targetData = TargetDataFromNode(selectedNode);

        if (targetData) {
            const entityInstances = getFilterInstances(props.entityConfiguration, metrics, targetData.parent?.variableIdentifier ?? undefined).entityInstances;

            if (entityInstances) {
                const sortedInstancesExcludingMe = entityInstances.filter(x => x.id != targetData.entityInstance?.id).sort((a, b) => a.id - b.id)
                const sortedInstancesExcludingMeAndNoWeightings = sortedInstancesExcludingMe.filter(instance => doesPlanForInstanceHaveWeights(instance, targetData.parent));

                return (
                    <CopyToSiblingsModal
                        isOpen={copyPlanToSiblingsModalVisible}
                        activeInstance={targetData.entityInstance!}
                        closeModal={() => setCopyPlanToSiblingsModalVisible(false)}
                        confirm={copyToSiblings}
                        entityInstances={sortedInstancesExcludingMe}
                        entityInstancesWithNoWeightings={sortedInstancesExcludingMeAndNoWeightings}
                        flattenToRim={flattenToRim}
                    />
                );
            }
        }
    }

    const getParentNode = (node: Node): Node | undefined => {
        return tree
            ? flattenTree(tree).find(n => n.children && n.children.map(c => c.id).includes(node.id))
            : undefined;
    }

    const isTargetInstanceOfRimWeighting = (treeNode: Node | null) => {
        if (treeNode == null) { //Make sure that we have a node
            return false;
        }
        var targetInstance = TargetDataFromNode(treeNode); //Make sure that we have a target instance
        if (targetInstance == null) {
            return false;
        }

        // See if there are any children from this (if so cann't be RIM here)
        if (treeNode.children != null && treeNode.children.length > 0) {
            return false;
        }
        const treeNodeForPlan = getParentNode(treeNode);
        if (treeNodeForPlan == undefined) {
            return false;
        }
        const treeNodeForParentOfPlan = getParentNode(treeNodeForPlan);
        if (treeNodeForParentOfPlan == undefined)
            return false;
        //If there are more than 1 child node here then we were on an target instance of a RIM weighting
        return (treeNodeForParentOfPlan.children != null && treeNodeForParentOfPlan.children?.length > 1);
    };

    const validForRimWeighting = (node: Node | null): boolean => {
        if (node == null)
            return true;

        const targetData = TargetDataFromNode(node);

        if (node.children) {

            return (targetData || node.synthetic) && node.children.some(n => n.children && n.children.some(c => c.children && c.children.length > 0)) ? false : true;
        }

        return true;
    }

    const relatedMetricFromNode = (selectedNode : Node |null): Metric | null => {
        if (selectedNode) {
            if (!selectedNode.synthetic) {
                const targetPlan = PlanDataFromNode(selectedNode);
                if (targetPlan) {
                    const associatedMetric = metrics.find(x => x.name == targetPlan.variableIdentifier);
                    if (associatedMetric && associatedMetric.variableConfigurationId != undefined) {
                        return associatedMetric;
                    }
                }
            }
        }
        return null;
    }

    const disableAddButton = () => {
        return isSingleQuestionAllNotSet(dimensionsInPlan) || !validForRimWeighting(selectedNode);
    }

    const getAddButtonTitle = disableAddButton()
        ? "Cannot currently add additional questions at this level due to nested Rim weighting"
        : "Add additional questions";

    const targetInstanceOfRimWeighting = isTargetInstanceOfRimWeighting(selectedNode);
    return (
        <VariableProvider
            googleTagManager={props.googleTagManager}
            pageHandler={props.pageHandler}
            user={productConfiguration.user}
            nonMapFileSurveys={productConfiguration.nonMapFileSurveys}
            isSurveyGroup={productConfiguration.isSurveyGroup}
        >
            <div className={`configuration-page ${style.weightingPlansConfigurationPage}`}>
                <div className="question-page">
                    <div className="top-pane">
                        {!isLoading && inconsistentBaseSizes &&
                        <WarningBanner message="Weighting is invalid because one or more questions don't represent total population" materialIconName="warning" />
                        }
                        {validateResult && (maxWeightAboveThreshold(validateResult.maxWeight) || validateResult.efficiencyScore < efficiencyScoreWarning) &&
                            < WarningBanner message={`Poor weighting efficiency ${toPercentage(validateResult.efficiencyScore, 1)}% < ${toPercentage(efficiencyScoreWarning, 0)}%` } materialIconName="warning" />
                        }
                        {displayPotentialValidationErrors() }
                        <div className="top-pane-controls">
                            <div className="left-controls">
                                <div>Segment: {props.subsetDisplayName}</div>
                            </div>
                            <div className="right-controls">
                                {!targetInstanceOfRimWeighting &&
                                    <>
                                    {!isExpansionWeightedNode &&
                                        <div className="rim-scores">
                                            <ul>
                                                <li>Min Weight:
                                                    <Tooltip title={getMinWeightTooltip(validateResult, dimensionsInPlan) } >
                                                        <span className={minWeightClass}>{validateResult ? validateResult.minWeight.toFixed(2) : "--"}</span>
                                                    </Tooltip>
                                                </li>
                                                <li>Max Weight:
                                                    <Tooltip placement="top" title={getMaxWeightTooltip(validateResult) }>
                                                        <span className={maxWeightClass}>{validateResult ? validateResult.maxWeight.toFixed(2) : "--"}</span>
                                                    </Tooltip>
                                                </li>
                                                <li>Efficiency:
                                                    <Tooltip placement="top" title={getEfficiencyTooltip(validateResult)} >
                                                        <span className={efficiencyClass}>{validateResult ? (toPercentage(validateResult.efficiencyScore, 0)) : "--"}%</span>
                                                    </Tooltip>
                                                </li>
                                            </ul>
                                        </div>
                                    }

                                    <WeightingButtonSimple
                                        disabled={!validateResult && !isSingleQuestionAllNotSet(dimensionsInPlan) && dimensionsInPlan.length != 0}
                                        className='primary-button'
                                        buttonIcon='save'
                                        buttonText='Save'
                                        onClick={() => updateWeightingPlanWithPossiblePopup()}
                                        toolTipText={`Save weightings for ${descriptionForNode(selectedNode)}`}
                                    />
                                </>
                                }
                                {deleteButtonAvailable &&
                                    <WeightingButton disabled={false}
                                        className='negative-button'
                                        buttonIcon='delete'
                                        buttonText='Delete'
                                        onClick={() => setDeleteConfirmationModalVisible(true)}
                                        toolTipText={`Delete weightings for ${descriptionForNode(selectedNode)}`}
                                        isBusy={isBusyDeleting}
                                        setIsBusy={setIsBusyDeleting }
                                />
                                }
                                {validateResult && !isExpansionWeightedNode &&
                                    <WeightingDropDown
                                        onShowStats={() => setWeightingStatsModalVisible(true)}
                                    />
                                }
                            </div>
                        </div>
                    </div>
                    <div className="left-pane">
                        <div className="tree-wrapper">
                            <SortableTree
                                isVirtualized={false}
                                treeData={tree}
                                canDrag={({ node }) => false}
                                canDrop={() => false}
                                maxDepth={treeMaxDepth}
                                onChange={onTreeChange}
                                scaffoldBlockPxWidth={treeScaffoldWidthPx}
                                rowHeight={treeRowHeightPx}
                                generateNodeProps={(rowInfo) => {
                                    const { node } = rowInfo;
                                    return {
                                        buttons:
                                            [
                                                <WeightingTreeDropDown
                                                    fullNodeDescription={descriptionForNode(node)}
                                                    averages={props.averages}
                                                    metric={relatedMetricFromNode(node)}
                                                    canClone={isCloneAvailableFor(node)}
                                                    cloneNode={(e) => {
                                                        setFlattenToRim(false);
                                                        setCopyPlanToSiblingsModalVisible(true);
                                                    }}
                                                    flattenAndCloneNode={(e) => {
                                                        setFlattenToRim(true);
                                                        setCopyPlanToSiblingsModalVisible(true);
                                                    }}
                                                    canExport={isCloneAvailableFor(node) || isNodeLeafForRim(node)}
                                                    export={(e, average) => {
                                                        exportWeights(e, average, node);
                                                    } }
                                                    subsetId={props.subsetId}
                                                />,

                                                    ],
                                        onClick: () => {
                                            onNodeClick(node);
                                        },
                                        style:
                                            node === selectedNode
                                                ? {
                                                    background: "rgba(182, 216, 247, 0.25)"
                                                    }
                                                : {}
                                    };
                                }}
                            />
                        </div>
                    </div>
                    <div className="right-pane">
                        <div className="header-right-flex">
                            <h3>{renderBreadCrumbs(selectedNode)}</h3>
                            {displayAddCopyDeleteButtons && <div className="dimensions-controls">
                                <button type="button" disabled={disableAddButton()} title={getAddButtonTitle} className="primary-button" onClick={() => setAddQuestionModalVisible(true)}><i className="material-symbols-outlined">add</i> Add questions</button>
                                {cloneButtonAvailable &&
                                    <WeightingButton disabled={!copyToSiblingsEnabled}
                                        className='hollow-button'
                                        buttonIcon='content_copy'
                                        buttonText=''
                                        onClick={() => { setFlattenToRim(false); setCopyPlanToSiblingsModalVisible(true); }}
                                        toolTipText={`Copy weightings from ${descriptionForNode(selectedNode)} to other weightings`}
                                        isBusy={isBusyCopying && copyPlanToSiblingsModalVisible}
                                        setIsBusy={setIsBusyCopying}
                                    />
                                }
                            </div>
                            }
                        </div>
                        <WeightingSchemeConfigurationPage
                            isLoading={isLoading}
                            dimensionsInScheme={dimensionsInPlan}
                            addQuestionsButtonHandler={() => setAddQuestionModalVisible(true)}
                            removeDimensionHandler={removeQuestionFromPlan}
                            populationSampleSize={populationSampleSize}
                            updateDimensionTargetPercentage={updateQuestionInPlanTargetPercentage}
                            updateDimensionTargetPopulation={updateQuestionInPlanTargetPopulation}
                            showDeleteButton={displayAddCopyDeleteButtons}
                            isRimTarget={targetInstanceOfRimWeighting}
                            isRootNode={isRootNode}
                        />
                    </div>
                    <AddMetricsModal
                        isOpen={addQuestionModalVisible}
                        metrics={validMetrics}
                        getPrimaryButtonText={getPrimaryButtonText}
                        modalHeaderText={"Add questions to weighting"}
                        onMetricsSubmitted={addQuestionsToPlan}
                        setAddChartModalVisibility={setAddQuestionModalVisible}
                        preSelectedMetrics={dimensionsInPlan.map(d => d.metric)}
                        disableSelectAll
                        onMetricAdded={() => reloadMetrics()}
                        saveOpenStateToSessionOnVariableCreation
                    />
                    <DeleteModal
                        isOpen={deleteConfirmationModalVisible}
                        thingToBeDeletedName={`weightings for ${descriptionForNode(selectedNode)}`}
                        thingToBeDeletedType="weightings"
                        delete={() => deleteFromWeightingPlan()}
                        closeModal={(deleted: boolean) => {
                            setIsBusyDeleting(false);
                            setDeleteConfirmationModalVisible(deleted);
                        }}
                        affectAllUsers
                        delayClick
                    />
                    <DisplayWeightingStats
                        isOpen={weightingStatsModalVisible}
                        stats={validateResult}
                        nodeName={selectedNode?.tooltip}
                        maxWeightClass={maxWeightClass}
                        minWeightClass={minWeightClass}
                        efficiencyClass={efficiencyClass}
                        closeModal={() => {
                            setWeightingStatsModalVisible(false);
                        }}

                     />

                    <UpdateModal
                        isOpen={updateConfirmationModalVisible}
                        description={`Currently there are warnings about under min weight, over max weight and/or poor efficiency.`}
                        title="Weightings"
                        update={() => updateWeightingPlan()}
                        closeModal={(close: boolean) => setUpdateConfirmationModalVisible(close)}
                        affectAllUsers
                        delayClick
                    />
                    {getCopyToSiblingsModal()}
                </div>
            </div>
        </VariableProvider>
    );
};

export default WeightingPlansConfigurationPage;