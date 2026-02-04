import { mockOverTimeResults } from "../helpers/MockApi";
import {
    LlmInsight,
    LlmInsightRelatedHeadline,
    LlmInsightResults,
    LlmInsightUserFeedback,
    MultiEntityRequestModel
} from "../BrandVueApi";
import React from "react";
import LlmInsightsPanel from "./LlmInsightsPanel";
import { act, render, RenderResult, screen, waitFor } from '@testing-library/react';
import { Provider } from "react-redux";
import { setOverTimeResults } from "../state/resultsSlice";
import { setupStore } from "../state/store";
import fetchMock from 'jest-fetch-mock';
import '@testing-library/jest-dom';
import { MixPanel } from "./mixpanel/MixPanel";
import { MixPanelModel } from "./mixpanel/MixPanelHelper";
import { PageDescriptor } from "../BrandVueApi";
import * as PagesHelper from "./helpers/PagesHelper";
import { IPageContext } from "./helpers/PagesHelper";
import { ViewType, ViewTypeEnum } from "./helpers/ViewTypeHelper";
import { MockStoreBuilder } from "../helpers/MockStore";

jest.mock('react-router-dom', () => ({
    useNavigate: jest.fn(),
    useLocation: jest.fn()
}));

const mockInsights: LlmInsightResults = {
    id: "abcdef-12345",
    userFeedback: new LlmInsightUserFeedback({
        created: new Date("2024-02-27"),
        userComment: 'Great Stuff!',
        isUseful: true,
    }),
    aiSummary: [
        new LlmInsight(
            {
                segmentId: 1,
                title: "Insight 1",
                insight: "Big Insight",
                significance: 9,
                userFeedbackSegmentCorrectness: true,
                relatedHeadlines: [new LlmInsightRelatedHeadline({
                    headline: 'Headline 1',
                    date: '2020-01-01',
                    source: 'ACME News'
                })]
            }),
        new LlmInsight(
            {
                segmentId: 2,
                title: "Insight 2",
                insight: "Small Insight",
                significance: 3,
                relatedHeadlines: []
            }
        )
    ],
    init: function (_data?: any): void {
        throw new Error("Function not implemented.");
    },
    toJSON: function (data?: any) {
        throw new Error("Function not implemented.");
    }
}

const mockResults = {
    results: {
        [1]: {
            results: mockOverTimeResults(["Brand Metric"]),
            request: new MultiEntityRequestModel(),
            requested: new Date('2020-01-01'),
        }
    }
};

const mockMixPanelClient = {
    init: jest.fn(),
    identify: jest.fn(),
    track: jest.fn(),
    reset: jest.fn(),
    setPeople: jest.fn()
};

const mockPageInfo: IPageContext = {
    page: new PageDescriptor(),
    pagePart: "/brand-attention",
    activeViews: [
        new ViewType(ViewTypeEnum.Competition, "Competition", "multiline_chart"),
    ]
};

fetchMock.enableMocks();

jest.spyOn(PagesHelper, "getCurrentPageInfo").mockImplementation(() => mockPageInfo);

describe('LlmInsightsPanel', () => {
    beforeEach(() => {
        fetchMock.resetMocks();
    });

    it('should render loading state', () => {
        let root: RenderResult | null = null;
        const loadingStore = setupStore(
            new MockStoreBuilder()
                .setLlmInsights({ loading: true, results: null, requested: null })
                .setResults(mockResults)
                .build()
        );

        act(() => {
            root = render(
                <Provider store={loadingStore}>
                    <LlmInsightsPanel partId={1}/>
                </Provider>
            );
        });

        expect(screen.getByRole('loading')).toBeInTheDocument();
    });

    it('should render error state', async () => {
        const errorStore = setupStore(
            new MockStoreBuilder()
                .setLlmInsights({
                    error: 'An error occurred',
                    loading: false,
                    results: null,
                    requested: new Date('2020-01-01')
                })
                .setResults({})
                .build()
        );

        act(() => {
            render(
                <Provider store={errorStore}>
                    <LlmInsightsPanel partId={1}/>
                </Provider>)
        });

        expect(screen.getByText("An error occurred")).toBeInTheDocument();
    });

    it('should render results', async () => {
        const resultsStore = setupStore(
            new MockStoreBuilder()
                .setLlmInsights({
                    results: mockInsights,
                    requested: new Date('2020-01-02'),
                    loading: false,
                })
                .setResults(mockResults)
                .build()
        );

        const mixPanelModelInstance: MixPanelModel = {
            userId: "userIdTest",
            projectId: "mixPanelTokenTest",
            client: mockMixPanelClient,
            isAllVue: false,
            productName: "BrandVue",
            project: "subProductIdTest",
            kimbleProposalId: "",
        };

        MixPanel.init(mixPanelModelInstance);

        act(() => {
            render(
                <Provider store={resultsStore}>
                    <LlmInsightsPanel partId={1}/>
                </Provider>
            );
        });

        expect(screen.getByText('Insight 1')).toBeInTheDocument();
        expect(screen.getByText('Big Insight')).toBeInTheDocument();
        expect(screen.getByText('Insight 2')).toBeInTheDocument();
        expect(screen.getByText('Small Insight')).toBeInTheDocument();

        const copyButtons = screen.getAllByRole('copy');
        expect(copyButtons).toHaveLength(2);

        expect(mockMixPanelClient.track).toHaveBeenCalledWith('Swys Opened', {
            "Category": "BrandVue",
            "ChartType": "Competition",
            "Part": "/brand-attention",
            "Product": "BrandVue",
            "SubCategory": "SayWhatYouSee",
            "Subset": undefined,
            "Tag": "Internal",
            "Project": "subProductIdTest",
            "KimbleProposalId":"",
        });
    });

    it('should fetch insights on mount', async () => {
        const fetchStore = setupStore(
            new MockStoreBuilder()
                .setLlmInsights({ loading: false, results: null, requested: null })
                .setResults({
                    results: {
                        [1]: {
                            results: mockOverTimeResults(["Brand Metric"]),
                            request: new MultiEntityRequestModel(),
                            requested: new Date('2020-01-01'),
                        },
                    },
                })
                .build()
        );

        act(() => {
            render(<Provider store={fetchStore}>
                <LlmInsightsPanel partId={1}/>
            </Provider>);
        });

        await waitFor(() => {
            expect(fetchMock).toHaveBeenCalledTimes(1);
            expect(fetchMock.mock.calls[0][0]).toContain('/insights/OverTimeRequestData');
        });
    });

    it.each([undefined, {}])('should fetch insights when averages are empty or null', async (x) => {
        const fetchStore = setupStore(
            new MockStoreBuilder()
                .setLlmInsights({ loading: false, results: null, requested: null })
                .setResults({
                    results: {
                        [1]: {
                            results: mockOverTimeResults(["Brand Metric"]),
                            request: new MultiEntityRequestModel(),
                            requested: new Date('2020-01-01'),
                        },
                    },
                    averages: x
                })
                .build()
        );

        act(() => {
            render(<Provider store={fetchStore}>
                <LlmInsightsPanel partId={1}/>
            </Provider>);
        });

        await waitFor(() => {
            expect(fetchMock).toHaveBeenCalledTimes(1);
            expect(fetchMock.mock.calls[0][0]).toContain('/insights/OverTimeRequestData');
        });
    });

    it('should fetch insights when results are updated', async () => {
        const fetchStore = setupStore(
            new MockStoreBuilder()
                .setLlmInsights({ loading: false, results: null, requested: null })
                .setResults({
                    results: { [1]: { request: {} as any, results: {} as any, requested: new Date() } },
                    averages: {}
                })
                .build()
        );

        act(() => {
            render(<Provider store={fetchStore}>
                <LlmInsightsPanel partId={1}/>
            </Provider>);
        });
        expect(fetchMock).toHaveBeenCalledTimes(0);

        fetchStore.dispatch(setOverTimeResults({
            results: mockOverTimeResults(["Brand Metric"]),
            request: new MultiEntityRequestModel(),
            partId: 1,
            focusedInstanceId: 1
        }));

        await waitFor(() => {
            expect(fetchMock).toHaveBeenCalledTimes(1);
            expect(fetchMock.mock.calls[0][0]).toContain('/insights/OverTimeRequestData');
        });
    });
});

