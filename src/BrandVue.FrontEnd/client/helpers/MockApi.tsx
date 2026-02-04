import {
    AbstractCommonResultsInformation,
    CompetitionResults,
    CuratedResultsModel,
    DataClient,
    DataSortOrder,
    Factory,
    IPartDescriptor,
    PartDescriptor,
    SampleSizeMetadata,
    ScorecardPerformanceCompetitorDataResult,
    ScorecardPerformanceCompetitorResults,
    ScorecardPerformanceCompetitorsMetricResult,
    ScorecardPerformanceMetricResult,
    ScorecardPerformanceResults,
    WeightedDailyResult,
    PeriodResult,
    EntityWeightedDailyResults,
    MetaDataClient,
    IAverageDescriptor,
    AverageDescriptor,
    MultiEntityRequestModel,
    StackedMultiEntityResults,
    StackedInstanceResult, StackedMultiEntityRequestModel, OverTimeResults
} from "../BrandVueApi";
import {BRAND_QUESTION, IS_FUN_QUESTION, IS_GOOD_QUESTION, IS_PURPLE_QUESTION, MockApplication} from "./MockApp";
import {filterSet} from "../filter/filterSet";
import {getTypedPart} from "../parts/IPart";
import {CuratedFilters} from "../filter/CuratedFilters";
import {
    convertedTestAverages,
    createMockEntityConfiguration,
    createMockProductConfiguration,
    createMockSession
} from "./MockSession";
import { IGoogleTagManager } from "../googleTagManager";
import {IDashPartProps} from "../components/DashBoard";
import {viewBase} from "../core/viewBase";
import {mock} from "jest-mock-extended";
import {PageHandler} from "../components/PageHandler";
import {ProductConfigurationContext} from "../ProductConfigurationContext";
import React from "react";
import {ReactElement} from "react";
import {MockRouter} from "./MockRouter";

export const setMockDataClient = (resultOverrides: { [id: string]: AbstractCommonResultsInformation }) : DataClient => {
    const dataClient = createMockDataClient([BRAND_QUESTION], resultOverrides)
    Factory.DataClient = (handleError: ((errorLambda: () => never, error?: any) => void), baseUri?: string) => {
        return dataClient;
    };
    Factory.DataClientWithNoHandler = (handleError: ((errorLambda: () => never, error?: any) => void), baseUri?: string) => {
        return dataClient;
    };
    Factory.MetaDataClient = (handleError: ((errorLambda: () => never, error?: any) => void)) => {
        return createMockMetaDataClient();
    };
    return dataClient;
}
export const createMockDataClient = (metrics: string[], resultOverrides: {
    [id: string]: AbstractCommonResultsInformation
}): DataClient => {
    //feel free to add additional mock implementations
    let scorecardPerformanceResults = resultOverrides["ScorecardPerformanceResults"] as ScorecardPerformanceResults
        ?? mockScorecardPerformanceResults(metrics);
    let scorecardPerformanceCompetitorResults = resultOverrides["ScorecardPerformanceCompetitorResults"] as ScorecardPerformanceCompetitorResults
        ?? mockScorecardPerformanceCompetitorResults(metrics);
    let stackedMultiEntityResults = resultOverrides["StackedMultiEntityResults"] as StackedMultiEntityResults 
        ?? mockStackedMultiEntityResults(metrics[0], ["Brand1" ] , [
            {name: BRAND_QUESTION, id: 1, weightedResults: [0.5, 0.7]},
            {name: IS_GOOD_QUESTION, id: 2, weightedResults: [0.5, 0.3]},
            {name: IS_FUN_QUESTION, id: 3, weightedResults: [0.5, 0.6]},
            {name: IS_PURPLE_QUESTION, id: 4, weightedResults: [0.5, 0.45]}
        ]);        
    let competitionResults = resultOverrides["CompetitionResults"] as CompetitionResults ?? mockCompetitionResults(metrics)
    return {
        getScorecardPerformanceResults: jest.fn((model?: CuratedResultsModel | undefined) =>
            Promise.resolve<ScorecardPerformanceResults>(scorecardPerformanceResults)),
        getScorecardPerformanceResultsAverage: jest.fn((model?: CuratedResultsModel | undefined) =>
            Promise.resolve<ScorecardPerformanceCompetitorResults>(scorecardPerformanceCompetitorResults)),
        getCompetitionResults: jest.fn((model?: MultiEntityRequestModel) =>
            Promise.resolve<CompetitionResults>(competitionResults)),
        getStackedResultsForMultipleEntities: jest.fn((model?: StackedMultiEntityRequestModel) =>
            Promise.resolve<StackedMultiEntityResults>(stackedMultiEntityResults))
    } as Partial<DataClient> as DataClient
}
export const createMockMetaDataClient = (): MetaDataClient => {
    return {
        getAverages: jest.fn((subsetId: string) =>
            Promise.resolve<AverageDescriptor[]>(convertedTestAverages))
    } as Partial<MetaDataClient> as MetaDataClient
}

export const createMockComponent = (partOverrides): ReactElement => {
    const partInfo = {...mockDefaultPartInfo, ...partOverrides} as IPartDescriptor;
    const productConfiguration = createMockProductConfiguration();
    const filter = new filterSet();
    filter.filters = [];
    const part = getTypedPart(partInfo);
    const mockEntityConfiguration = createMockEntityConfiguration();
    const curatedFilters = new CuratedFilters(filter, mockEntityConfiguration, []);
    curatedFilters.average = convertedTestAverages[0];
    curatedFilters.setEndDate(new Date("2020-01-01"));
    const session = createMockSession(MockApplication.averageFilter);
    const gtm = mock<IGoogleTagManager>();

    const mockDashProps: IDashPartProps = {
        updateAverageRequests: jest.fn(), getAllInstancesForType: jest.fn(),
        removeFromLowSample: jest.fn(),
        updateMetricResultsSummary: jest.fn(),
        activeView: {
            curatedFilters: curatedFilters,
            activeMetrics: MockApplication.allMetrics.metrics
        } as viewBase,
        availableEntitySets: [],
        mainInstance: MockApplication.mainBrand,
        curatedFilters: curatedFilters,
        entitySet: MockApplication.defaultBrandSet,
        googleTagManager: gtm,
        enabledMetricSet: MockApplication.allMetrics,
        entityConfiguration: mockEntityConfiguration,
        pageHandler: mock<PageHandler>(),
        paneHeight: 0,
        partConfig: part,
        session: session
    }

    return <ProductConfigurationContext.Provider value={{productConfiguration: productConfiguration}}>
        <MockRouter>
            {part.getPartComponent(mockDashProps)!}
        </MockRouter>
    </ProductConfigurationContext.Provider>;
}

const mockDefaultPartInfo: IPartDescriptor = {
    defaultSplitBy: "",
    helpText: "",
    paneId: "",
    partType: "",
    spec1: "",
    spec2: "",
    spec3: "",
    id: 1,
    autoMetrics: [],
    autoPanes: [],
    ordering: [],
    orderingDirection: DataSortOrder.Ascending,
    colours: [],
    filters: [],
    xAxisRange: {},
    yAxisRange: {},
    sections: [],
    defaultAverageId: '',
    breaks: [],
    overrideReportBreaks: false,
    multipleEntitySplitByAndFilterBy: {splitByEntityType: '', filterByEntityTypes: []},
    showTop: 0,
    averageTypes: [],
    displayMeanValues: false,
    displayStandardDeviation: false,
    customConfigurationOptions : undefined,
    disabled: false,
    environment: [],
    fakeId: "",
    roles: [],
    subset: [],
    showOvertimeData: false,
}

const genericEntityWeightedDailyResults = (entityNames: string[], weightedResults: number[]) => entityNames.map((e,j) => new EntityWeightedDailyResults({
    entityInstance: {
        name: e,
        id: j,
        enabledBySubset: {},
        startDateBySubset: {},
        fields: {},
        color: "#fff",
        imageUrl:"https://example.com/image.png",
    },
    unweightedResponseCount: 0,
    weightedResponseCount: 0,
    weightedDailyResults: weightedResults.map(r => new WeightedDailyResult({
        date: new Date("2020-01-01"),
        weightedResult: r,
        weightedValueTotal: 600,
        unweightedValueTotal: 500,
        unweightedSampleSize: 1000,
        weightedSampleSize: 1200,
        responseIdsForDay: [],
        childResults: [],
        text: "",
    }))
}))
export const mockScorecardPerformanceResults: (metric: string[]) => ScorecardPerformanceResults = (metric: string[]) => new ScorecardPerformanceResults({
    metricResults: [
        new ScorecardPerformanceMetricResult({
            metricName: metric[0],
            periodResults: [
                new WeightedDailyResult({
                    date: new Date("2020-01-01"),
                    weightedResult: 0.5,
                    weightedValueTotal: 600,
                    unweightedValueTotal: 500,
                    unweightedSampleSize: 1000,
                    weightedSampleSize: 1200,
                    responseIdsForDay: [],
                    childResults: [],
                    text: "",
                })
            ]
        })
    ],
    sampleSizeMetadata: new SampleSizeMetadata({
        sampleSize: {
            unweighted: 1000,
            weighted: 1000,
            hasDifferentWeightedSample: false,
        },
        sampleSizeByMetric: {},
        sampleSizeByEntity: {},
        currentDate: new Date("2020-01-01")
    }),
    hasData: true,
    lowSampleSummary: [],
    trialRestrictedData: false,
    typeName: "ScorecardPerformanceResults"
});
export const mockScorecardPerformanceResultsWithPrevious: (metric: string[], resultChange: number[]) => ScorecardPerformanceResults = (metric: string[], resultChange: number[]) => {
    return new ScorecardPerformanceResults({
        metricResults: metric.map((x, i) => new ScorecardPerformanceMetricResult({
            metricName: x,
            periodResults: [

                new WeightedDailyResult({
                    date: new Date("2019-12-01"),
                    weightedResult: 0.5,
                    weightedValueTotal: 600,
                    unweightedValueTotal: 500,
                    unweightedSampleSize: 1000,
                    weightedSampleSize: 1200,
                    responseIdsForDay: [],
                    childResults: [],
                    text: "",
                }),
                new WeightedDailyResult({
                    date: new Date("2020-01-01"),
                    weightedResult: 0.5 + resultChange[i],
                    weightedValueTotal: 600,
                    unweightedValueTotal: 500,
                    unweightedSampleSize: 1000,
                    weightedSampleSize: 1200,
                    responseIdsForDay: [],
                    childResults: [],
                    text: "",
                }),
            ]
        })),
        sampleSizeMetadata: new SampleSizeMetadata({
            sampleSize: {
                unweighted: 1000,
                weighted: 1000,
                hasDifferentWeightedSample: false,
            },
            sampleSizeByMetric: {},
            sampleSizeByEntity: {},
            currentDate: new Date("2020-01-01")
        }),
        hasData: true,
        lowSampleSummary: [],
        trialRestrictedData: false,
        typeName: "ScorecardPerformanceResults"
    })
}

interface EntityResultMock {
    name: string,
    id: number,
    weightedResults: number[]
}
export const mockStackedMultiEntityResults: (metric: string, entityNames: string[], filterInstances: EntityResultMock[]) => 
    StackedMultiEntityResults = (metric: string, entityNames: string[], filterInstances: EntityResultMock[]) =>
    new StackedMultiEntityResults({
        resultsPerInstance: filterInstances.map(x=> new StackedInstanceResult({
            filterInstance: {
                name: x.name,
                enabledBySubset: {},
                startDateBySubset: {},
                fields: {},
                id: x.id,
                color: "#fff",
                imageUrl: "https://example.com/image.png",
            },
            data: genericEntityWeightedDailyResults(entityNames, x.weightedResults),
            lowSampleSummary:[],
            sampleSizeMetadata: new SampleSizeMetadata(),
            trialRestrictedData:false,
            hasData: true,
            typeName: "StackedInstance"
        })),
        lowSampleSummary:[],
        sampleSizeMetadata: new SampleSizeMetadata(),
        trialRestrictedData:false,
        hasData: true,
        typeName: "StackedMultiEntityResults"
    });

export const mockScorecardPerformanceCompetitorResults: (metric: string[]) => ScorecardPerformanceCompetitorResults = (metric: string[]) => new ScorecardPerformanceCompetitorResults({
    metricResults: [
        new ScorecardPerformanceCompetitorsMetricResult({
            metricName: metric[0],
            competitorAverage: 0.40,
            competitorData: [
                new ScorecardPerformanceCompetitorDataResult({
                    entityInstance: {
                        name: "01 - Brand One",
                        enabledBySubset: {},
                        startDateBySubset: {
                            "All": new Date("2019-08-01T")
                        },
                        fields: {},
                        id: 1,
                        color: "",
                        imageUrl: "",
                    },
                    result: new WeightedDailyResult({
                        date: new Date("2020-01-01"),
                        weightedResult: 0.2,
                        weightedValueTotal: 200,
                        unweightedValueTotal: 200,
                        unweightedSampleSize: 1000,
                        weightedSampleSize: 1000,
                        responseIdsForDay: [],
                        childResults: [],
                        text: ""
                    }),
                }),
                new ScorecardPerformanceCompetitorDataResult({
                    entityInstance: {
                        name: "02 - Brand Two",
                        enabledBySubset: {},
                        startDateBySubset: {
                            "All": new Date("2019-08-01T")
                        },
                        fields: {},
                        id: 2,
                        color: "",
                        imageUrl: "",
                    },
                    result: new WeightedDailyResult({
                        date: new Date("2020-01-01"),
                        weightedResult: 0.6,
                        weightedValueTotal: 600,
                        unweightedValueTotal: 600,
                        unweightedSampleSize: 1000,
                        weightedSampleSize: 1000,
                        responseIdsForDay: [],
                        childResults: [],
                        text: ""
                    }),
                }),
            ],
        })
    ],
    sampleSizeMetadata: new SampleSizeMetadata({
        sampleSize: {
            unweighted: 1000,
            weighted: 1000,
            hasDifferentWeightedSample: false,
        },
        sampleSizeByMetric: {},
        sampleSizeByEntity: {},
        currentDate: new Date("2020-01-01")
    }),
    hasData: true,
    lowSampleSummary: [],
    trialRestrictedData: false,
    typeName: "ScorecardPerformanceCompetitorResults"
})
export const mockCompetitionResults: (metric: string[]) => CompetitionResults = (metric: string[]) => new CompetitionResults({
    periodResults: [
        new PeriodResult({
            period: {
                startDate: new Date("2020-01-01"),
                endDate: new Date("2020-01-31"),
                name: "Jan 2020"
            },
            resultsPerEntity: [
                new EntityWeightedDailyResults({
                    entityInstance: {
                        name: "00 - Main brand",
                        enabledBySubset: {},
                        startDateBySubset: {
                            "All": new Date("2019-08-01T")
                        },
                        fields: {},
                        id: 0,
                        color: "",
                        imageUrl: "",
                    },
                    unweightedResponseCount: 0,
                    weightedResponseCount: 0,
                    weightedDailyResults: [new WeightedDailyResult({
                        date: new Date("2020-01-01"),
                        weightedResult: 0.5,
                        weightedValueTotal: 600,
                        unweightedValueTotal: 600,
                        unweightedSampleSize: 1000,
                        weightedSampleSize: 1000,
                        responseIdsForDay: [],
                        childResults: [],
                        text: ""
                    })]
                }),
                new EntityWeightedDailyResults({
                    entityInstance: {
                        name: "01 - Brand One",
                        enabledBySubset: {},
                        startDateBySubset: {
                            "All": new Date("2019-08-01T")
                        },
                        fields: {},
                        id: 1,
                        color: "",
                        imageUrl:"",
                    },
                    unweightedResponseCount: 0,
                    weightedResponseCount: 0,
                    weightedDailyResults: [new WeightedDailyResult({
                        date: new Date("2020-01-01"),
                        weightedResult: 0.5,
                        weightedValueTotal: 600,
                        unweightedValueTotal: 600,
                        unweightedSampleSize: 1000,
                        weightedSampleSize: 1000,
                        responseIdsForDay: [],
                        childResults: [],
                        text: ""
                    })]
                }),
                new EntityWeightedDailyResults({
                    entityInstance: {
                        name: "02 - Brand Two",
                        enabledBySubset: {},
                        startDateBySubset: {
                            "All": new Date("2019-08-01T")
                        },
                        fields: {},
                        id: 2,
                        color: "",
                        imageUrl: "",
                    },
                    unweightedResponseCount: 0,
                    weightedResponseCount: 0,
                    weightedDailyResults: [new WeightedDailyResult({
                        date: new Date("2020-01-01"),
                        weightedResult: 0.6,
                        weightedValueTotal: 600,
                        unweightedValueTotal: 600,
                        unweightedSampleSize: 1000,
                        weightedSampleSize: 1000,
                        responseIdsForDay: [],
                        childResults: [],
                        text: ""
                    })]
                }),
            ]
        }),
    ],
    sampleSizeMetadata: new SampleSizeMetadata({
        sampleSize: {
            unweighted: 1000,
            weighted: 1000,
            hasDifferentWeightedSample: false,
        },
        sampleSizeByMetric: {},
        sampleSizeByEntity: {},
        currentDate: new Date("2020-01-01")
    }),
    hasData: true,
    lowSampleSummary: [],
    trialRestrictedData: false,
    typeName: "CompetitionResults"
})

export const mockOverTimeResults = (metric: string[]) =>  new OverTimeResults({
    entityWeightedDailyResults: genericEntityWeightedDailyResults(["01 - Brand 1", "01 - Brand 2"], [0.4, 0.5, 0.6]),
    sampleSizeMetadata: new SampleSizeMetadata(),
    hasData: true,
    lowSampleSummary: [],
    trialRestrictedData: false,
    typeName: "OverTimeResults"
});

