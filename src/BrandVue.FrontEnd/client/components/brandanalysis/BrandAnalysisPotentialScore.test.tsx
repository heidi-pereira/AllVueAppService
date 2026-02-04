import { DataSubsetMock } from "../../helpers/MockSession";
import { DataSubsetManager } from "../../DataSubsetManager";
import "@testing-library/jest-dom";
import { BRAND_QUESTION, IS_FUN_QUESTION, IS_GOOD_QUESTION, IS_PURPLE_QUESTION } from "../../helpers/MockApp";
import { act, render, RenderResult, screen } from "@testing-library/react";
import {
    createMockComponent,
    mockScorecardPerformanceResultsWithPrevious,
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
}));
jest.mock("../../googleTagManager");

let partConfig = {
    paneId: 'BrandAdvocacy',
    partType: 'BrandAnalysisPotentialScore',
    spec1: BRAND_QUESTION,
    spec2: 'Advocacy',
    spec3: `{"metrics": []}`,
    defaultSplitBy: '',
    helpText: 'Advocacy is a measure of how many of your customers would recommend your brand to friends or family. It is shaped by experience, and impacts the buzz generated around your brand.',
}

describe("brand analysis potential score rendering", () => {
    let root: RenderResult|null = null;
    beforeEach(() => {
        setMockDataClient({"ScorecardPerformanceResults": mockScorecardPerformanceResultsWithPrevious([BRAND_QUESTION, IS_GOOD_QUESTION, IS_FUN_QUESTION, IS_PURPLE_QUESTION], [0.2,-0.2, 0.1, -0.05])});
    });

    it("renders", async () => {
        await act(async () => {
            root = render(
                <Provider store={setupStore(new MockStoreBuilder().build())}>
                    {createMockComponent(partConfig)}
                </Provider>
            );
        });
        expect(root?.baseElement).not.toBeEmptyDOMElement();
    });
});