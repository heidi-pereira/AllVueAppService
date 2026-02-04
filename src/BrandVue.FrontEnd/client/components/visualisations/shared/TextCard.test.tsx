import  React from "react";
import "@testing-library/jest-dom";
import {act, render, screen } from "@testing-library/react";
import { mock } from "jest-mock-extended";
import { DataSubsetMock } from "../../../helpers/MockSession";
import { DataSubsetManager } from "../../../DataSubsetManager";
import { CuratedFilters } from '../../../filter/CuratedFilters';
import TextCard from "./TextCard";
import { PageHandler } from "../../PageHandler";
import { IGoogleTagManager } from "../../../googleTagManager";
import { createMockEntityConfiguration } from "../../../helpers/MockSession";
import { MockApplication } from "../../../helpers/MockApp";
import { IAverageDescriptor, AverageDescriptor } from "../../../BrandVueApi";
import { MockRouter } from "../../../helpers/MockRouter";
import { Provider } from "react-redux";
import { setupStore } from '../../../state/store';
import { MockStoreBuilder } from "client/helpers/MockStore";

jest.mock("../../../DataSubsetManager");
DataSubsetManager.selectedSubset = DataSubsetMock.selectedSubset;
DataSubsetManager.filterMetricByCurrentSubset = DataSubsetMock.filterMetricByCurrentSubset;
DataSubsetManager.supportsDataSubset = DataSubsetMock.supportsDataSubset;
DataSubsetManager.filterMetricByCurrentSubset = DataSubsetMock.filterMetricByCurrentSubset;
DataSubsetManager.parseSupportedSubsets = DataSubsetMock.parseSupportedSubsets;
jest.mock("history", () => ({
    createBrowserHistory: () => ({ replace: jest.fn() })
}))

const coOptions = {
    endDate: new Date(),
    startDate: new Date(),
    average: new AverageDescriptor(undefined),
    comparisonPeriodSelection: undefined
}

const curactedFilers = CuratedFilters.createWithOptions(coOptions, createMockEntityConfiguration());
const metric = MockApplication.allMetrics.metrics[0];
const gtm = {
    addEvent: jest.fn(),
    addConfigurationEvent: jest.fn()
} as IGoogleTagManager;
const renderTextCard = ({ fullWith }: { fullWith?: boolean } | any = {}): React.ReactElement => {
    let enriched = false;
    return (
        <Provider store={setupStore(new MockStoreBuilder().build())}>            
            <MockRouter initialEntries={['/']}>
                <TextCard
                    googleTagManager={gtm}
                    pageHandler={mock<PageHandler>()}
                    metric={metric}
                    getDescriptionNode={() => <div data-testid="custom-description-node" />}
                    filterInstances={[]}
                    curatedFilters={curactedFilers}
                    baseExpressionOverride={undefined}
                    setDataState={(_) => { }}
                    setIsLowSample={(_) => { }}
                    setCanDownload={(_) => { }}
                    fullWidth={fullWith ?? true}
                    lowSampleThreshold={75}
                    entityConfiguration={undefined} />
            </MockRouter>
        </Provider>
    );
};

describe('textCard', () => {
    describe('fullWith=true (uses getTextCardDescription)', () => {
        test.each([
            { ailaEnabled: true, description: 'when aila is enabled' },
            { ailaEnabled: false, description: 'when aila is disabled' },
        ])('tabs are not shown $description', async ({ ailaEnabled, description }) => {
            act(() => { render(renderTextCard({ ailaEnabled: ailaEnabled, fullWith: true })); });
            var summary = await screen.queryByText("Summary");
            expect(summary).toBeNull();
        });

        describe('AilaSummariseButton', () => {
            const buttonText = "Summarise";

            it('is shown', async () => {
                act(() => { render(renderTextCard({ fullWith: true })); });
                var summary = await screen.queryByText(buttonText);
                expect(summary).not.toBeNull();
            });
        });
    });

    describe('fullWith=false (uses props.getDescriptionNode)', () => {
        it('uses custom description node', async () => {
            act(() => { render(renderTextCard({ fullWith: false })); });
            var customDescriptionNode = await screen.queryByTestId("custom-description-node");
            expect(customDescriptionNode).not.toBeNull();
        });
    });
});