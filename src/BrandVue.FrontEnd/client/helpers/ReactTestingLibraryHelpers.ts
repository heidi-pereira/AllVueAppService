import React from "react";
import { EntitySet } from "../entity/EntitySet";
import { CalculationType, AverageType, AxisRange, BaseExpressionDefinition, CrossMeasure, DataSortOrder, IEntityType, EntityType, ICrossMeasure, MultipleEntitySplitByAndFilterBy, PageDescriptor, PartDescriptor, ReportOrder, ReportWaveConfiguration, SelectedEntityInstances, CrosstabSignificanceType, BaseDefinitionType, DefaultReportFilter, IApplicationUser, RunningEnvironment, ReportType, SigConfidenceLevel, DisplaySignificanceDifferences, PermissionFeatureOptionWithCode, DataPermissionDto} from "../BrandVueApi";
import { EntityInstance } from "../entity/EntityInstance";
import { Metric } from "../metrics/metric";
import { MetricSet } from "../metrics/metricSet";
import { IEntityInstanceGroup } from "../entity/IEntityInstanceGroup";
import { IEntitySetAverageGroup } from "../entity/IEntitySetAverageGroup";
import { IEntityInstanceColourRepository } from "../entity/EntityInstanceColourRepository";
import { EntitySetAverage } from "../entity/EntitySetAverage";
import { Report } from "../BrandVueApi";

export const singleChoiceMetricName = "singleChoice";

export const generateMetric = (name: string, downIsGood: boolean = false) => {
    const ms = new MetricSet();
    const metric = new Metric(ms);
    metric.name = name;
    metric.downIsGood = downIsGood;
    metric.displayName = name;
    metric.varCode = name;
    return metric;
}

export const getMetrics = (numberOfMetrics: number) => {
    const metrics: Metric[] = [];
    for (let i = 0; i < numberOfMetrics; i++) {
        metrics.push(generateMetric(`metric ${i}`));
    }
    return metrics;
}

export const generateEntityType = (iterator: number) => {
    return new EntityType(
        {
            identifier: `test${iterator}`,
            displayNameSingular: `test${iterator}`,
            displayNamePlural: `test${iterator}`,
            isProfile: false,
            isBrand: false,
        }
    );
}

export const createEntities = (numberOfEntitites: number) => {
    const entities: IEntityType[] = [];
    for (let i = 0; i < numberOfEntitites; i++) {
        entities.push(generateEntityType(i));
    }
    return entities;
}

export const getCrossMeasures = (metrics: Metric[], numberOfCrossMeasures: number) => {
    const categories: CrossMeasure[] = [];

    for (let i = 0; i < numberOfCrossMeasures; i++) {
        const crossMeasure: ICrossMeasure = {
            childMeasures: [],
            filterInstances: [],
            measureName: metrics[i].name,
            multipleChoiceByValue: false,
            significanceFilterInstanceComparandName: metrics[i].name
        }

        categories.push(new CrossMeasure(crossMeasure));
    }
    return categories;
}

const getEntityType = () => {
    return new EntityType({
        createdFrom: undefined,
        displayNamePlural: "",
        displayNameSingular: "",
        identifier: "",
        isBrand: false,
        isProfile: false
    });
}

const getEntityCombination = (entityCount: number) => {
    const entityCombination: IEntityType[] = [];
    for (let i = 0; i < entityCount; i++) {
        entityCombination.push(getEntityType());
    }
    return entityCombination;
}

export const getMetricWithEntityCombinations = (entityCount: number) => {
    const metricSet = new MetricSet();
    const metric = new Metric({ metricSet });
    metric.name = singleChoiceMetricName;
    metric.calcType = CalculationType.Average;
    metric.entityCombination = getEntityCombination(entityCount);
    metric.numFormat = "0%";
    return metric;
}

export const getPartWithExtraData = (part: PartDescriptor, metric: Metric) => {
    return {
        part: part,
        metric: metric,
        ref: React.createRef<HTMLDivElement>(),
        selectedEntitySet: undefined,
    };
}

export enum AriaRoles {
    BUTTON = 'button',
    TABLE = 'table',
    TABLE_CELL = 'cell',
    SEARCHBAR = 'search',
    HEADER = 'heading',
    DOCUMENT = 'document',
    MENU = 'menu',
    LIST = 'list',
    LIST_ITEM = 'listitem',
    CHECKBOX = 'checkbox',
    TEXTBOX = 'textbox',
    RADIO_BUTTON = 'radio',
    SPIN_BUTTON = 'spinbutton',
    MODAL = 'dialog'
}

export const getPartDescriptor = (): PartDescriptor => {
    let partDescriptor = new PartDescriptor();
    partDescriptor.id = 1;
    partDescriptor.fakeId = 'fakeId';
    partDescriptor.paneId = 'paneId';
    partDescriptor.partType = 'partType';
    partDescriptor.spec1 = 'spec1';
    partDescriptor.spec2 = 'spec2';
    partDescriptor.spec3 = 'spec3';
    partDescriptor.defaultSplitBy = 'defaultSplitBy';
    partDescriptor.helpText = 'helpText';
    partDescriptor.defaultAverageId = 'defaultAverageId';
    partDescriptor.autoMetrics = ['metric1', 'metric2'];
    partDescriptor.autoPanes = ['pane1', 'pane2'];
    partDescriptor.ordering = ['order1', 'order2'];
    partDescriptor.orderingDirection = DataSortOrder.Ascending;
    partDescriptor.colours = ['red', 'blue'];
    partDescriptor.filters = ['filter1', 'filter2'];
    partDescriptor.xAxisRange = new AxisRange();
    partDescriptor.yAxisRange = new AxisRange();
    partDescriptor.sections = [['section1'], ['section2']];
    partDescriptor.breaks = [new CrossMeasure()];
    partDescriptor.overrideReportBreaks = true;
    partDescriptor.showTop = 10;
    partDescriptor.multipleEntitySplitByAndFilterBy = new MultipleEntitySplitByAndFilterBy();
    partDescriptor.reportOrder = ReportOrder.ResultOrderAsc;
    partDescriptor.baseExpressionOverride = new BaseExpressionDefinition();
    partDescriptor.waves = new ReportWaveConfiguration();
    partDescriptor.selectedEntityInstances = new SelectedEntityInstances();
    partDescriptor.averageTypes = [AverageType.Mean];
    partDescriptor.multiBreakSelectedEntityInstance = 5;
    partDescriptor.displayMeanValues = true;
    partDescriptor.customConfigurationOptions = undefined;
    return partDescriptor;
}

const getEntityInstance = (name: string): EntityInstance => {
    const mockEntityInstance: EntityInstance = new EntityInstance();
    mockEntityInstance.name = name;
    mockEntityInstance.enabledBySubset = {
        subset1: true,
        subset2: false,
    };
    return mockEntityInstance;
};

const getEntityInstanceGroup = (name: string): IEntityInstanceGroup => {
    const instance = getEntityInstance(name);
    const mockInstances: IEntityInstanceGroup = {
        getAll: () => [instance],
        getById: (id: number) => (id === instance.id ? instance : undefined),
        addInstances: () => { },
        addInstance: () => { },
        removeInstance: () => { },
        containsSameInstances: () => true,
        clone: () => mockInstances,
    };
    return mockInstances;
}

const getEntityAverageGroup = (): IEntitySetAverageGroup => {
    const mockAverages: IEntitySetAverageGroup = {
        getAll: () => [],
        getById: (id: number) => (id === 1 ? new EntitySetAverage(1) : undefined),
        addAverages: () => { },
        addAverage: () => { },
        removeAverage: () => { },
        clone: () => mockAverages
    };
    return mockAverages;
}
const getmockEntityInstanceColourRepository = (): IEntityInstanceColourRepository => {
    const mockEntityInstanceColourRepository: IEntityInstanceColourRepository = {
        get: () => '#FFFFFF',
    };
    return mockEntityInstanceColourRepository;
}

export const getEntitySet = (name: string) => {
    const mockEntitySet = new EntitySet(
        1,
        generateEntityType(1),
        name,
        getEntityInstanceGroup(name),
        true,
        true,
        getEntityInstance(name),
        getEntityAverageGroup(),
        true,
        false,
        getmockEntityInstanceColourRepository()
    );
    return mockEntitySet;
}

export const mockReport: Report = new Report({
    savedReportId: 1,
    isShared: true,
    pageId: 1,
    reportOrder: ReportOrder.ResultOrderAsc,
    modifiedDate: new Date(),
    modifiedGuid: 'guid-1234',
    lastModifiedByUser: 'user123',
    decimalPlaces: 2,
    reportType: ReportType.Chart,
    waves: new ReportWaveConfiguration(),
    breaks: [new CrossMeasure()],
    includeCounts: true,
    highlightLowSample: false,
    highlightSignificance: true,
    displaySignificanceDifferences: DisplaySignificanceDifferences.ShowBoth,
    significanceType: CrosstabSignificanceType.CompareToTotal,
    sigConfidenceLevel: SigConfidenceLevel.NinetyFive,
    singlePageExport: false,
    isDataWeighted: true,
    hideEmptyRows: false,
    hideEmptyColumns: true,
    hideTotalColumn: false,
    hideDataLabels: false,
    showMultipleTablesAsSingle: false,
    baseTypeOverride: BaseDefinitionType.AllRespondents,
    baseVariableId: 1,
    defaultFilters: [new DefaultReportFilter()],
    subsetId: "subset-1234",
    calculateIndexScores: false,
    userHasAccess: true,
});

export const mockApplicationUser: IApplicationUser = {
    userId: '12345',
    userName: 'johndoe',
    name: 'John',
    surname: 'Doe',
    accountName: 'JohnDoeAccount',
    products: ['Product1', 'Product2'],
    isAdministrator: true,
    isSystemAdministrator: false,
    isThirdPartyLoginAuth: false,
    isReportViewer: true,
    isTrialUser: false,
    canEditMetricAbouts: true,
    canAccessRespondentLevelDownload: false,
    runningEnvironmentDescription: 'Development',
    runningEnvironment: RunningEnvironment.Development,
    doesUserHaveAccessToInternalSavantaSystems: false,
    featurePermissions: [] as PermissionFeatureOptionWithCode[],
    dataPermission: {
        name: 'Full Access',
        variableIds: [],
        filters: []
    },
};