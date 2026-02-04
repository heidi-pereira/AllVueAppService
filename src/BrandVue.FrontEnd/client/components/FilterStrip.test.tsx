import Comparison from "./visualisations/MetricComparison/Comparison";
import { EntityInstance } from "../entity/EntityInstance";
import { IGoogleTagManager } from "../googleTagManager";
import { createMockEntityConfiguration, createMockSession, createMockApplicationConfiguration } from "../helpers/MockSession";
import { PageDescriptor, CrossMeasure, IEntityType } from "../BrandVueApi";
import { MetricSet } from "../metrics/metricSet";
import { viewBase } from "../core/viewBase";
import { PageHandler } from "./PageHandler";
import { FilterInstance } from "../entity/FilterInstance";
import { filterSet } from "../filter/filterSet";
import { MockApplication, MockCuratedFilters } from "../helpers/MockApp";
import { mock } from "jest-mock-extended";
import { act, render, RenderResult } from "@testing-library/react";
import "@testing-library/jest-dom";
import FilterStrip from "./FilterStrip";
import { screen } from '@testing-library/dom';
import { getDateInUtc } from "./helpers/PeriodHelper";
import { ComparisonContext, IComparisonContextState } from "./helpers/ComparisonContext";
import {MockRouter} from "../helpers/MockRouter";
import { setupStore } from "../state/store";
import { MockStoreBuilder } from "../helpers/MockStore";
import { Provider } from "react-redux";
import { CuratedFilters } from "../filter/CuratedFilters";

const mockTrackEvent = jest.fn();
const mockTrackPageView = jest.fn();

const mockTagManagerInstance = {
    trackEvent: mockTrackEvent,
    trackPageView: mockTrackPageView,
};
jest.mock('../googleTagManager', () => ({
    useGoogleTagManager: function() {
        return mockTagManagerInstance;
    }
}));

describe("FilterStrip ", () => {

    // Needed to set up DatasetManager to be used by the createMock... methods
    var mockSession = createMockSession();

    let filterStripProps = {
        activeDashPage: new PageDescriptor(),
        filters: new filterSet(),
        metrics: new MetricSet(),
        entityConfiguration: createMockEntityConfiguration(),
        averageRequests: null,
        coreViewType: 1,
        activeView: {
            curatedFilters: MockCuratedFilters as any as CuratedFilters,
            activeBrand: MockApplication.mainBrand,
            activeMetrics: MockApplication.allMetrics.metrics,
            getEntityCombination: () => []
        } as viewBase,
        overridingPaneType: "",
        googleTagManager: mock<IGoogleTagManager>(),
        pageHandler: mock<PageHandler>(),
        entitySet: MockApplication.defaultBrandSet,
        additionalInstances: new Array<EntityInstance>(),
        filterInstance: new FilterInstance(mock<IEntityType>(), mock<EntityInstance>()),
        breaks: new CrossMeasure(),
        showFilterButton: false,
        saveImageButtonText: "Save Image",
        categoryExportResults: [],
        applicationConfiguration: createMockApplicationConfiguration(getDateInUtc(2017, 7, 1), getDateInUtc(2018, 8, 31))
    };

    const mockComparisonState: IComparisonContextState = {
        getComparisons: () => [],
        setComparisons: (comparisons: Comparison[]) => { },
        addComparison: (comparison: Comparison) => { },
        clearComparisons: () => { }
    }
    
    let filterStrip = <ComparisonContext.Provider value={ mockComparisonState }>
        <Provider store={setupStore(new MockStoreBuilder().build())}>
            <MockRouter initialEntries={['/']}>
                <FilterStrip {...filterStripProps} />
            </MockRouter>
        </Provider>
    </ComparisonContext.Provider>;

    it("renders", async () => {
        let root: RenderResult | null = null;
        act(() => {
            root = render(filterStrip);
        });
        expect(root).toBeDefined();
    });

    it("does not render a Filter button when the switch is false", async () => {
        let root: RenderResult | null = null;
        act(() => {
            root = render(filterStrip);
        });
        const filterButton = await screen.queryByText('Filters');
        expect(filterButton).toBeNull();
    });

    //TODO: Add tests for the following:
    // - shouldShowExcelDownload
    // - shouldShowSaveChart
    // - shouldShowFilterButton

});