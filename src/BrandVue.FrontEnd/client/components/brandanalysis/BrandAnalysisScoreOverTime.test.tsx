import {
    DataSubsetMock
} from "../../helpers/MockSession";
import { DataSubsetManager } from "../../DataSubsetManager";
import React from "react";
import "@testing-library/jest-dom";
import { BRAND_QUESTION } from "../../helpers/MockApp";
import { act, render, RenderResult } from "@testing-library/react";
import {
    createMockComponent,
    setMockDataClient
} from "../../helpers/MockApi";
import { Provider } from "react-redux";
import { setupStore } from "client/state/store";
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
jest.mock("../visualisations/DashBox", () => ({
    __esModule: true,
    default: jest.fn((prop: any) => <div>MOCK</div>),
    legendPosition: {},
}))


let partConfig = {
    paneId: 'BrandAdvocacy',
    partType: 'BrandAnalysisScoreOverTime',
    spec1: BRAND_QUESTION,
    spec2: 'Advocacy',
    spec3: '{"metrics":[{"key":"IsBrandGood","metricName":"IsBrandGood","requestType":"scorecardPerformance"},{"key":"ExperienceRating","metricName":"ExperienceRating","requestType":"scorecardPerformance"}]}',
    defaultSplitBy: '',
    helpText: 'Advocacy is a measure of how many of your customers would recommend your brand to friends or family. It is shaped by experience, and impacts the buzz generated around your brand.',
}

describe("brand analysis score over time rendering", () => {
    let root: RenderResult|null = null;
    beforeEach(() => {
        setMockDataClient({});
    });

    it("renders", async () => {
        const store = setupStore(new MockStoreBuilder().build());
        await act(async () => {
            root = render(
                <Provider store={store}>
                    {createMockComponent(partConfig)}
                </Provider>
            );
        });
        expect(root?.baseElement).not.toBeEmptyDOMElement();
    });
});

