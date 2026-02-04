import { DataSubsetMock } from "../../helpers/MockSession";
import { DataSubsetManager } from "../../DataSubsetManager";
import "@testing-library/jest-dom";
import {
    BRAND_QUESTION, IMAGE_METRIC,
    IS_FUN_QUESTION,
    IS_GOOD_QUESTION,
    IS_PURPLE_QUESTION,
    MockApplication
} from "../../helpers/MockApp";
import { act, render, screen, waitFor } from "@testing-library/react";
import {
    createMockComponent,
    setMockDataClient
} from "../../helpers/MockApi";
import { DataClient } from "../../BrandVueApi";
import { Provider } from "react-redux";
import { setupStore, store } from '../../state/store';
import { updateSubset } from "client/state/subsetSlice";
import { MockStoreBuilder } from "client/helpers/MockStore";

jest.mock("../../DataSubsetManager");
DataSubsetManager.selectedSubset = DataSubsetMock.selectedSubset;
DataSubsetManager.filterMetricByCurrentSubset = DataSubsetMock.filterMetricByCurrentSubset;
DataSubsetManager.supportsDataSubset = DataSubsetMock.supportsDataSubset;
DataSubsetManager.filterMetricByCurrentSubset = DataSubsetMock.filterMetricByCurrentSubset;
DataSubsetManager.parseSupportedSubsets = DataSubsetMock.parseSupportedSubsets;
jest.mock("../../googleTagManager");
jest.mock("../helpers/UrlHelper", () => ({
    useReadVueQueryParams: jest.fn(),
    useWriteVueQueryParams: jest.fn(),
}));
jest.mock("../helpers/PagesHelper", () =>
    ({
        ...jest.requireActual("../helpers/PagesHelper"),
        "getMetricNamesForPanes": jest.fn(() => {
            return [IMAGE_METRIC];
        }),
        "getUrlForPageDisplayName": jest.fn(() => ""),
        "getUrlForPageName": jest.fn(() => ""),
        "getPageTreeForDisplayName": jest.fn(() => []),
        "getActivePage": jest.fn(() => []),
    }
));

jest.mock("../../metrics/MetricStateContext", () => ({
    useMetricStateContext: () => ({ enabledMetricSet: MockApplication.allMetrics })
}));

let partConfig = {
    paneId: 'BrandAdvocacy',
    partType: 'BrandAnalysisWhereNext',
    spec1: BRAND_QUESTION,
    spec2: 'Advocacy',
    spec3: `{
        "buttonLink":"${IMAGE_METRIC}"
    }`,
    defaultSplitBy: '',
    helpText: 'Advocacy is a measure of how many of your customers would recommend your brand to friends or family. It is shaped by experience, and impacts the buzz generated around your brand.',
    pageMetricConfiguration: {
        shortUserDescription: 'brand advocates',
    }
}

describe("brand analysis where next rendering", () => {
    let dataClient: DataClient;
    beforeEach(() => {
        dataClient = setMockDataClient({});
    });

    it("shows the two largest metric changes",
        async () => {
            act(() => {
                render(<Provider store={setupStore(new MockStoreBuilder().build())}><>{createMockComponent(partConfig)}</></Provider>);
            });
            var up = await screen.findByText(BRAND_QUESTION);
            var down = await screen.findByText(IS_GOOD_QUESTION);
            expect(up.parentNode!.textContent).toMatch(/has increased/i);
            expect(down.parentNode!.textContent).toMatch(/has fallen/i);
            expect(screen.queryByText(IS_FUN_QUESTION)).toBeNull();
            expect(screen.queryByText(IS_PURPLE_QUESTION)).toBeNull();
        });

    it("shows three highest metrics", async () => {
        act(() => {
            render(<Provider store={setupStore(new MockStoreBuilder().build())}>{createMockComponent(partConfig)}</Provider>);
        });
        var increases = await screen.findByText(/The top Image associations amongst/);
        expect(increases.textContent).toMatch(BRAND_QUESTION);
        expect(increases.textContent).toMatch(IS_FUN_QUESTION);
        expect(increases.textContent).toMatch(IS_PURPLE_QUESTION);
        
    });
    
    it.each([{title:"Buzz", text:"those who talk positively about your brand"}, 
        {title:"Advocacy", text:"brand Advocates"}, 
        {title:"Usage", text:"current users/buyers"}, 
        {title:"Love", text:"brand Lovers"}])
    ('shows correct descriptions', async (config) => {
        await act(() => {
            var testConfig = {...partConfig};
            testConfig.spec2 = config.title;
            render(<Provider store={setupStore(new MockStoreBuilder().build())}>{createMockComponent(testConfig)}</Provider>);
        });
        await waitFor(async () => {
            expect(dataClient.getStackedResultsForMultipleEntities).toHaveBeenCalledTimes(1);
            var insights = await screen.findByRole("insights-content");
            expect(insights.textContent?.includes(config.text)).toBeTruthy();
        })
    });
});

