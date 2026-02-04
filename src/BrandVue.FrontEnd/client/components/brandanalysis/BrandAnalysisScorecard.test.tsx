import {
    DataSubsetMock
} from "../../helpers/MockSession";
import { DataSubsetManager } from "../../DataSubsetManager";
import "@testing-library/jest-dom";
import { BRAND_QUESTION } from "../../helpers/MockApp";
import { act, render, RenderResult, screen } from "@testing-library/react";
import {
    createMockComponent,
    mockScorecardPerformanceCompetitorResults,
    setMockDataClient
} from "../../helpers/MockApi";
import { Provider } from "react-redux";
import { setupStore } from '../../state/store';
import { MockStoreBuilder } from "client/helpers/MockStore";

jest.mock("../../DataSubsetManager");
DataSubsetManager.selectedSubset = DataSubsetMock.selectedSubset;
DataSubsetManager.filterMetricByCurrentSubset = DataSubsetMock.filterMetricByCurrentSubset;
DataSubsetManager.supportsDataSubset = DataSubsetMock.supportsDataSubset;
DataSubsetManager.filterMetricByCurrentSubset = DataSubsetMock.filterMetricByCurrentSubset;
DataSubsetManager.parseSupportedSubsets = DataSubsetMock.parseSupportedSubsets;
jest.mock("history", () => ({
   createBrowserHistory: () => ({replace: jest.fn()}) 
}))
jest.mock("../../googleTagManager")

let partConfig = {
    paneId: 'BrandAdvocacy',
    partType: 'BrandAnalysisScorecard',
    spec1: BRAND_QUESTION,
    spec2: 'Advocacy',
    spec3: '',
    defaultSplitBy: '',
    helpText: 'Advocacy is a measure of how many of your customers would recommend your brand to friends or family. It is shaped by experience, and impacts the buzz generated around your brand.',
}

describe("brand analysis card rendering", () => {
    let root: RenderResult|null = null;
    beforeEach(() => {
        setMockDataClient({});
    });

    it("renders title", async () => {
        act(() => {
            root = render(<Provider store={setupStore(new MockStoreBuilder().build())}>{createMockComponent(partConfig)}</Provider>);
        });
        var title = await screen.findByText("Advocacy");
        expect(title).not.toBeEmptyDOMElement();
    });
    it("shows roundel", async () => {
        act(() => {
            root = render(<Provider store={setupStore(new MockStoreBuilder().build())}>{createMockComponent(partConfig)}</Provider>);
        });
        var roundel = await screen.findByRole("roundel");
        expect(roundel).toHaveTextContent("50");
    });
    it("shows average difference", async () => {
        act(() => {
            root = render(<Provider store={setupStore(new MockStoreBuilder().build())}>{createMockComponent(partConfig)}</Provider>);
        });
        var aboveText = await screen.findByRole("sub-text");
        expect(aboveText).toHaveTextContent("above");
    });
    it("can have bad average difference", async () => {
        var results = mockScorecardPerformanceCompetitorResults([BRAND_QUESTION]);
        results.metricResults[0].competitorData = [results.metricResults[0].competitorData.find(x=>x.result.weightedResult > 0.5)!];
        results.metricResults[0].competitorAverage = 0.6;
        setMockDataClient({"ScorecardPerformanceCompetitorResults": results});
        
        act(() => {
            root = render(<Provider store={setupStore(new MockStoreBuilder().build())}>{createMockComponent(partConfig)}</Provider>);
        });
        var aboveText = await screen.findByRole("sub-text");
        expect(aboveText).toHaveTextContent("below");
    });
});

