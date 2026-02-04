import {
    AverageType,
    CalculationType,
    EntityTypeAndInstance,
    IPageDescriptor,
    IPaneDescriptor,
    DataSortOrder,
    MainQuestionType,
    MultipleEntitySplitByAndFilterBy,
    PageDescriptor,
    PaneDescriptor,
    PartDescriptor,
    ReportType,
    AxisRange,
    CrossMeasure,
    Subset
} from "../../../../BrandVueApi";
import { Metric } from "../../../../metrics/metric";
import { PartType } from "../../../panes/PartType";
import { PaneType } from "../../../panes/PaneType";
import { ViewTypeEnum } from "../../../helpers/ViewTypeHelper";
import { IEntityConfiguration } from "../../../../entity/EntityConfiguration";
import { getSplitByAndFilterByEntityTypesForMetric, SplitByAndFilterByEntityTypes, getTypedEmptyArray } from "../../../helpers/SurveyVueUtils";
import { getUrlSafePageName } from "../../../helpers/UrlHelper";

export const CreateNewReportPage = (
    pageName: string,
    metrics: Metric[],
    entityConfiguration: IEntityConfiguration,
    questionTypeLookup: {[metricName: string]: MainQuestionType},
    reportType: ReportType,
    reportHasWaves: boolean): PageDescriptor =>
{
    const urlSafePageName = getUrlSafePageName(pageName);
    const page = {
        name: urlSafePageName,
        displayName: pageName,
        pageType: 'SubPage',
        panes: [createReportPane(urlSafePageName, metrics, entityConfiguration, questionTypeLookup, reportType, reportHasWaves)]
    } as IPageDescriptor;
    return new PageDescriptor(page);
}

const createReportPane = (
    parentPageName: string,
    metrics: Metric[],
    entityConfiguration: IEntityConfiguration,
    questionTypeLookup: {[metricName: string]: MainQuestionType},
    reportType: ReportType,
    reportHasWaves: boolean): PaneDescriptor =>
{
    const pane = {
        height: 500,
        pageName: parentPageName,
        paneType: PaneType.reportSubPage,
        view: ViewTypeEnum.SingleSurveyNav,
        parts: metrics.map((m, index) => getPartFromMetric(m, undefined, index, entityConfiguration, questionTypeLookup, reportType, reportHasWaves))
    } as IPaneDescriptor;
    return new PaneDescriptor(pane);
}

export const clonePart = (part: PartDescriptor): PartDescriptor => {
    return new PartDescriptor({
        paneId: part.paneId,
        partType: part.partType,
        spec1: part.spec1,
        spec2: "",
        defaultSplitBy: part.defaultSplitBy,
        helpText: part.helpText,
        breaks: part.breaks,
        overrideReportBreaks: part.overrideReportBreaks,
        showTop: part.showTop,
        multipleEntitySplitByAndFilterBy: part.multipleEntitySplitByAndFilterBy,
        reportOrder: part.reportOrder,
        baseExpressionOverride: part.baseExpressionOverride,
        waves: part.waves,
        selectedEntityInstances: part.selectedEntityInstances,
        averageTypes: part.averageTypes,
        displayMeanValues: part.displayMeanValues,
        showOvertimeData: part.showOvertimeData,
        displayStandardDeviation: part.displayStandardDeviation,

        id: 0,
        fakeId: '',
        spec3: part.spec3,
        defaultAverageId: part.defaultAverageId,
        autoMetrics: part.autoMetrics,
        autoPanes: part.autoPanes,
        ordering: part.ordering,
        orderingDirection: part.orderingDirection,
        colours: part.colours,
        filters: part.filters,
        xAxisRange: part.xAxisRange,
        yAxisRange: part.yAxisRange,
        sections: part.sections,
        subset: part.subset,
        environment: part.environment,
        roles: part.roles,
        disabled: false,
        customConfigurationOptions: part.customConfigurationOptions,
    });
}


export const getPartFromMetric = (
    metric: Metric,
    paneId: string | undefined,
    index: number,
    entityConfiguration: IEntityConfiguration,
    questionTypeLookup: {[metricName: string]: MainQuestionType},
    reportType: ReportType,
    reportHasWaves: boolean): PartDescriptor =>
{
    const partType = getPartTypeForMetric(metric, questionTypeLookup, reportType, reportHasWaves);
    const entityTypes = getSplitByAndFilterByEntityTypesForMetric(metric, entityConfiguration);
    const splitByAndFilterBy = metric.calcType === CalculationType.Text ?
        getMultipleEntitySplitByAndFilterByTextQuestion(entityConfiguration, entityTypes) :
        getMultipleEntitySplitByAndFilterBy(entityConfiguration, partType, entityTypes);

    return new PartDescriptor({
        paneId: paneId ?? '',
        partType: partType,
        spec1: metric.name,
        spec2: index.toString(),
        defaultSplitBy: entityTypes.splitByEntityType?.identifier ?? '',
        helpText: metric.helpText,
        breaks: getTypedEmptyArray<CrossMeasure>(),
        overrideReportBreaks: false,
        showTop: undefined,
        multipleEntitySplitByAndFilterBy: splitByAndFilterBy,
        reportOrder: undefined,
        baseExpressionOverride: undefined,
        waves: undefined,
        selectedEntityInstances: undefined,
        averageTypes: getTypedEmptyArray<AverageType>(),
        displayMeanValues: false,
        displayStandardDeviation: false,
        customConfigurationOptions: undefined,
        showOvertimeData: undefined,

        id: 0,
        fakeId: '',
        spec3: '',
        defaultAverageId: '',
        autoMetrics: getTypedEmptyArray<string>(),
        autoPanes: getTypedEmptyArray<string>(),
        ordering: getTypedEmptyArray<string>(),
        orderingDirection: DataSortOrder.Ascending,
        colours: getTypedEmptyArray<string>(),
        filters: getTypedEmptyArray<string>(),
        xAxisRange: new AxisRange({min: undefined, max: undefined}),
        yAxisRange: new AxisRange({min: undefined, max: undefined}),
        sections: getTypedEmptyArray<string[]>(),
        subset: getTypedEmptyArray<Subset>(),
        environment: getTypedEmptyArray<string>(),
        roles: getTypedEmptyArray<string>(),
        disabled: false,
    });
}

export const getPartTypeForMetric = (
    metric: Metric,
    questionTypeLookup: {[metricName: string]: MainQuestionType},
    reportType: ReportType,
    reportHasWaves: boolean): PartType =>
{
    if(reportType == ReportType.Table) {
        return PartType.ReportsTable;
    }
    if (metric.calcType === CalculationType.Text) {
        if (questionTypeLookup[metric.name] === MainQuestionType.HeatmapImage) {
            return PartType.ReportsCardHeatmapImage;
        }
        return PartType.ReportsCardText;
    }
    if (reportHasWaves) {
        return PartType.ReportsCardLine;
    }
    if (metric.entityCombination.length > 1) {
        if (questionTypeLookup[metric.name] === MainQuestionType.MultipleChoice ||
            metric.entityCombination.length > 2 ||
            !metric.isPercentage())
        {
            return PartType.ReportsCardMultiEntityMultipleChoice;
        }
        return PartType.ReportsCardStackedMulti;
    }
    return PartType.ReportsCardChart;
}

const partTypesWithFilterInstances = [
    PartType.ReportsCardText,
    PartType.ReportsCardMultiEntityMultipleChoice,
    PartType.ReportsCardStackedMulti,
    PartType.ReportsCardLine,
];

const getMultipleEntitySplitByAndFilterBy = (entityConfiguration: IEntityConfiguration, partType: PartType, entityTypes: SplitByAndFilterByEntityTypes): MultipleEntitySplitByAndFilterBy => {
    const splitByAndFilterBy = new MultipleEntitySplitByAndFilterBy();

    if (entityTypes.splitByEntityType) {
        splitByAndFilterBy.splitByEntityType = entityTypes.splitByEntityType.identifier;
    }

    if (entityTypes.filterByEntityTypes.length > 0) {
        if (partTypesWithFilterInstances.includes(partType)) {
            splitByAndFilterBy.filterByEntityTypes = entityTypes.filterByEntityTypes.map(type => {
                var allInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(type);
                return new EntityTypeAndInstance({
                    type: type.identifier,
                    instance: allInstances[0].id
                });
            })
        } else {
            splitByAndFilterBy.filterByEntityTypes = entityTypes.filterByEntityTypes.map(type =>
                new EntityTypeAndInstance({ type: type.identifier }));
        }
    }

    return splitByAndFilterBy;
}

const getMultipleEntitySplitByAndFilterByTextQuestion = (entityConfiguration: IEntityConfiguration, entityTypes: SplitByAndFilterByEntityTypes): MultipleEntitySplitByAndFilterBy => {
    const splitByAndFilterBy = new MultipleEntitySplitByAndFilterBy();

    if (entityTypes.filterByEntityTypes.length == 0) {
        if (entityTypes.splitByEntityType) {
            var allInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(entityTypes.splitByEntityType);
            splitByAndFilterBy.filterByEntityTypes = [new EntityTypeAndInstance({
                type: entityTypes.splitByEntityType.identifier,
                instance: allInstances[0].id
            })];
        }
    } else {
        if (entityTypes.splitByEntityType) {
            splitByAndFilterBy.splitByEntityType = entityTypes.splitByEntityType?.identifier;
        }
        splitByAndFilterBy.filterByEntityTypes = entityTypes.filterByEntityTypes.map(type => {
            var allInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(type);
            return new EntityTypeAndInstance({
                type: type.identifier,
                instance: allInstances[0].id
            });
        });
    }

    return splitByAndFilterBy;
}