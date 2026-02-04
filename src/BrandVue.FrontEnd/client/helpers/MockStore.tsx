import { RootState } from "../state/store";
import { LlmInsightState } from "../state/llmInsightSlice";
import { ResultsState } from "../state/resultsSlice";
import { CategorySortKey, IEntityType } from "../BrandVueApi";
import { EntitySelectionState } from "client/state/entitySelectionSlice";
import { MockApplication } from "./MockApp";

export class MockStoreBuilder {
    private readonly state: RootState;

    constructor() {
        this.state = {
            application: {
                isSessionLoaded: false,
                primaryMetric: null,
            },
            average: {
                allAverages: MockApplication.averages,
            },
            entityConfiguration: {
                activeEntityTypeIdentifier: null,
                configurationByIdentifier: {},
                loading: false,
                error: null,
            },
            features: {
                features: [],
                userFeaturesByFeatureId: {},
                orgFeatures: [],
                allUsers: [],
                allOrgs: [],
                loading: false,
                error: null,
            },
            llmDiscovery: {
                results: null,
                requested: null,
                loading: false,
            },
            report: {
                errorState: {
                    isError: false,
                    errorMessage: "",
                },
                isSettingsChange: false,
                currentReportId: undefined,
                currentReportGuid: undefined,
                allReports: [],
                isLoading: false,
                isDataInSyncWithDatabase: true,
            },
            templates: {
                templates: [],
                isLoading: false,
                error: null,
            },
            variableConfiguration: {
                variables: [],
                loading: false,
            },
            llmInsights: {
                results: null,
                loading: false,
                error: null,
                requested: null,
            },
            results: {
                results: {},
                averages: {},
            },
            entitySelection: {
                activeBreaks: {},
                entitySets: {
                    [MockApplication.brandEntityType.identifier]: {
                        active: 1,
                        highlighted: [],
                        entitySetId: 1,
                        entitySetAverages: [],
                    },
                },
                priorityOrderedEntityTypes: [],
                categorySortKey: CategorySortKey.None,
            },
            timeSelection: {
                scorecardPeriod: "Monthly",
            },
            subset: {
                subsetId: "all",
                subsetConfigurations: [],
            },
        };
    }

    setLlmInsights(overrides: Partial<LlmInsightState>): this {
        this.state.llmInsights = {
            ...this.state.llmInsights,
            ...overrides,
        } as LlmInsightState;
        return this;
    }

    setResults(overrides: Partial<ResultsState>): this {
        this.state.results = {
            ...this.state.results,
            ...overrides,
        } as ResultsState;
        return this;
    }

    setEntitySelection(overrides: Partial<EntitySelectionState>): this {
        this.state.entitySelection = {
            ...this.state.entitySelection,
            ...overrides,
        } as EntitySelectionState;
        return this;
    }

    setPriorityOrderedEntityTypes(types: IEntityType[]): this {
        if (this.state.entitySelection) {
            this.state.entitySelection.priorityOrderedEntityTypes = types;
        }
        return this;
    }
    
    setActiveBrandSetId(id: number): this {
        this.state!.entitySelection!.entitySets![MockApplication.brandEntityType.identifier]!.entitySetId = id;
        return this;
    }
    
    setScorecardAverage(scorecardAverage: string): this {
        this.state.timeSelection.scorecardPeriod = scorecardAverage;
        return this;
    }

    setSubset(overrides: Partial<RootState['subset']> = {}): this {
        this.state.subset = {
            ...this.state.subset,
            subsetId: 'all',
            subsetConfigurations: [],
            ...overrides,
        };
        return this;
    }

    setReport(overrides: Partial<RootState['report']>): this {
        this.state.report = {
            ...this.state.report,
            ...overrides,
        };
        return this;
    }

    build(): RootState {
        return this.state;
    }
}
