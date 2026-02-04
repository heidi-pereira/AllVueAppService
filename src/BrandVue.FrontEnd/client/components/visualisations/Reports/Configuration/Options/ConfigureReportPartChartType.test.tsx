import React from "react";
import { render, screen } from '@testing-library/react';
import userEvent from "@testing-library/user-event";
import "@testing-library/jest-dom";
import ConfigureReportPartChartType from "./ConfigureReportPartChartType";
import { MainQuestionType, PartDescriptor, ReportType, Report, PageDescriptor } from "../../../../../BrandVueApi";
import { PartType } from '../../../../panes/PartType';
import { PartWithExtraData } from '../../ReportsPageDisplay';
import { AriaRoles, getMetricWithEntityCombinations, getPartWithExtraData } from "../../../../../helpers/ReactTestingLibraryHelpers";
import { MixPanelModel } from "../../../../../components/mixpanel/MixPanelHelper";
import { MixPanel } from "../../../../../components/mixpanel/MixPanel";
import { IGoogleTagManager } from "../../../../../googleTagManager";
import { mock } from "jest-mock-extended";
import { PageHandler } from "../../../../PageHandler";
import { Provider } from "react-redux";
import { configureStore } from '@reduxjs/toolkit';
import reportSlice from 'client/state/reportSlice';
import variableConfigurationReducer from 'client/state/variableConfigurationsSlice';
import { initialReportErrorState } from 'client/components/visualisations/shared/ReportErrorState';

const getMockStore = (reportType: ReportType) => {
    const report = {
        savedReportId: 1,
        pageId: 1,
        breaks: [],
        overTimeConfig: undefined,
        reportType: reportType
    } as any as Report;
    
    const page = {
        id: 1,
        name: "test-page",
        displayName: "Test Page"
    } as any as PageDescriptor;
    
    return configureStore({
        reducer: {
            report: reportSlice,
            variableConfiguration: variableConfigurationReducer
        },
        preloadedState: {
            report: {
                allReports: [report],
                currentReportId: report.savedReportId,
                reportErrorState: undefined,
                isSettingsChange: false,
                isDataInSyncWithDatabase: true,
                reportsPageOverride: page,
                defaultReportId: undefined,
                errorState: initialReportErrorState,
                isLoading: false
            },
            variableConfiguration: {
                variables: [],
                loading: false,
                error: null
            }
        }
    });
};

const getTestingComponent = (
    reportPart: PartWithExtraData,
    reportType: ReportType,
    savePartChanges: (newPart: PartDescriptor) => void,
    isUsingOverTime: boolean,
    isUsingWaves: boolean,
    isUsingBreaks: boolean): JSX.Element =>
{
    const store = getMockStore(reportType);
    return (
        <Provider store={store}>
            <VariableContext.Provider value={mockedVariableContext}>
                <ConfigureReportPartChartType
                    reportPart={reportPart}
                    savePartChanges={savePartChanges}
                    isUsingOverTime={isUsingOverTime}
                    isUsingWaves={isUsingWaves}
                    isUsingBreaks={isUsingBreaks}
                />
            </VariableContext.Provider>
        </Provider>
    );
};

jest.mock('../../../../helpers/FeaturesHelper', () => ({
    isFeatureEnabled: jest.fn().mockReturnValue(true),
}));

jest.mock("../../../../../metrics/MetricStateContext", () => ({
    __esModule: true,
    useMetricStateContext: jest.fn(() => ({
        questionTypeLookup: new Proxy({}, {
            get: () => MainQuestionType.SingleChoice,
        }),
        metrics: jest.fn().mockReturnValue([]),
    }))
}));

const mockedVariableContext = {
    user: null,
    nonMapFileSurveys: [],
    questionTypeLookup: {},
    googleTagManager: mock<IGoogleTagManager>(),
    pageHandler: mock<PageHandler>(),
    currentReportPage: undefined,
    shouldSetQueryParamOnCreate: undefined,
    isSurveyGroup: false,
    selectedPart: undefined,
    variables: [],
    isVariablesLoading: false,
    variablesLoadingError: null,
    variablesDispatch: jest.fn(),
};

jest.mock('../../../Variables/VariableModal/Utils/VariableContext', () => {
    const originalModule = jest.requireActual('../../../Variables/VariableModal/Utils/VariableContext');
    return {
        __esModule: true,
        ...originalModule,
        useVariableContext: jest.fn(() => mockedVariableContext),
    };
});

const { VariableContext } = require('../../../Variables/VariableModal/Utils/VariableContext');

const mockMixPanelClient = {
    init: jest.fn(),
    identify: jest.fn(),
    track: jest.fn(),
    reset: jest.fn(),
    setPeople: jest.fn()
};

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

describe("When a user wants to select the chart type", () => {
    const savePartChanges = jest.fn();
    const reportPart = new PartDescriptor();

    const ChartType = {
        Bar: 'Bar/column chart',
        Stacked: 'Stacked column chart',
        Line: 'Line chart',
        Doughnut: 'Doughnut chart',
        Funnel: 'Funnel chart',
    };

    const WaveStatus = {
        WithWaves: 'with waves',
        WithoutWaves: 'without waves',
    };

    const BreakStatus = {
        WithBreaks: 'with breaks',
        WithoutBreaks: 'without breaks',
    };

    const MetricType = {
        SingleEntity: 'single-entity',
        MultiEntity: 'multi-entity',
    }

    const ButtonIcon = {
        Bar: "bar_chart",
        Stacked: "stacked_bar_chart",
        Line: "timeline",
        Doughnut: "donut_large",
        Funnel: "filter_alt"
    }

    const FeatureFlag = {
        Enabled: "enabled",
        Disabled: "disabled"
    }

    const multiEntityMetric = getMetricWithEntityCombinations(2);
    const singleEntityMetric = getMetricWithEntityCombinations(1);

    const emptyDOMElementTestCases = [
        [MetricType.MultiEntity, WaveStatus.WithWaves],
        [MetricType.MultiEntity, WaveStatus.WithoutWaves],
        [MetricType.SingleEntity, WaveStatus.WithWaves],
        [MetricType.SingleEntity, WaveStatus.WithoutWaves],
    ];

    test.each(emptyDOMElementTestCases)("should not show for %s Table report %s", async (metricType, hasWaves) => {
        const metric = metricType == MetricType.MultiEntity ? multiEntityMetric : singleEntityMetric;
        const partWithExtraData = getPartWithExtraData(reportPart, metric);

        const component = getTestingComponent(partWithExtraData, ReportType.Table, savePartChanges, false, hasWaves == WaveStatus.WithWaves, false);
        const { container } = render(component);

        expect(container).toBeEmptyDOMElement();
    });

    const buttonCountTestCases = [
        [PartType.ReportsCardChart, MetricType.MultiEntity, WaveStatus.WithWaves, BreakStatus.WithBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Line]],
        [PartType.ReportsCardChart, MetricType.MultiEntity, WaveStatus.WithoutWaves, BreakStatus.WithBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked]],
        [PartType.ReportsCardChart, MetricType.MultiEntity, WaveStatus.WithWaves, BreakStatus.WithoutBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Line]],
        [PartType.ReportsCardChart, MetricType.MultiEntity, WaveStatus.WithoutWaves, BreakStatus.WithoutBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked]],

        [PartType.ReportsCardDoughnut, MetricType.MultiEntity, WaveStatus.WithWaves, BreakStatus.WithBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Doughnut, ButtonIcon.Line]],
        [PartType.ReportsCardDoughnut, MetricType.MultiEntity, WaveStatus.WithoutWaves, BreakStatus.WithBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Doughnut]],
        [PartType.ReportsCardDoughnut, MetricType.MultiEntity, WaveStatus.WithWaves, BreakStatus.WithoutBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Doughnut, ButtonIcon.Line]],

        [PartType.ReportsCardChart, MetricType.SingleEntity, WaveStatus.WithWaves, BreakStatus.WithBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Line]],
        [PartType.ReportsCardChart, MetricType.SingleEntity, WaveStatus.WithoutWaves, BreakStatus.WithBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked]],
        [PartType.ReportsCardChart, MetricType.SingleEntity, WaveStatus.WithWaves, BreakStatus.WithoutBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Line]],
        [PartType.ReportsCardChart, MetricType.SingleEntity, WaveStatus.WithoutWaves, BreakStatus.WithoutBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Doughnut]],

        [PartType.ReportsCardDoughnut, MetricType.SingleEntity, WaveStatus.WithWaves, BreakStatus.WithBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Doughnut, ButtonIcon.Line]],
        [PartType.ReportsCardDoughnut, MetricType.SingleEntity, WaveStatus.WithoutWaves, BreakStatus.WithBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Doughnut]],
        [PartType.ReportsCardDoughnut, MetricType.SingleEntity, WaveStatus.WithWaves, BreakStatus.WithoutBreaks, [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Doughnut, ButtonIcon.Line]],
    ];

    const allButtons = [ButtonIcon.Bar, ButtonIcon.Stacked, ButtonIcon.Doughnut, ButtonIcon.Line, ButtonIcon.Funnel];

    test.each(buttonCountTestCases)("%s %s metric %s and %s should show buttons", async (partType, metricType, hasWaves, hasBreaks, expectedEnabledButtons) => {
        const metric = metricType == MetricType.MultiEntity ? multiEntityMetric : singleEntityMetric;
        const testingPartType = new PartDescriptor(reportPart);
        testingPartType.partType = partType as string;
        const partWithExtraData = getPartWithExtraData(testingPartType, metric);

        const component = getTestingComponent(partWithExtraData, ReportType.Chart, savePartChanges, false, hasWaves == WaveStatus.WithWaves, hasBreaks == BreakStatus.WithBreaks);

        const { container } = render(component);

        expect(container).toBeInTheDocument();
        const buttons = screen.getAllByRole(AriaRoles.BUTTON);
        const enabledButtons = buttons.filter(button => button instanceof HTMLButtonElement && !button.disabled);
        const actualButtonIconLabels = buttons.map(button => button.childNodes[0].textContent).sort();
        const actualEnabledButtonIconLabels = enabledButtons.map(button => button.childNodes[0].textContent).sort();
        const expectedButtonIconLabels = (allButtons as string[]).sort();
        const expectedEnabledButtonIconLabels = (expectedEnabledButtons as string[]).sort();
        expect(actualButtonIconLabels).toEqual(expectedButtonIconLabels);
        expect(actualEnabledButtonIconLabels).toEqual(expectedEnabledButtonIconLabels);
    });

    const buttonClickTestCases = [
        [ChartType.Bar, MetricType.MultiEntity, PartType.ReportsCardMultiEntityMultipleChoice],
        [ChartType.Stacked, MetricType.MultiEntity, PartType.ReportsCardStackedMulti],
        [ChartType.Line, MetricType.MultiEntity, PartType.ReportsCardLine],
        [ChartType.Doughnut, MetricType.SingleEntity, PartType.ReportsCardDoughnut],
    ];

    test.each(buttonClickTestCases)("clicking %s button for a %s metric should set the %s part type", async (clickedButtonName, metricType, expectedPartType) => {
        const hasWaves = clickedButtonName == ChartType.Line;
        const partWithExtraData = getPartWithExtraData(reportPart, metricType == MetricType.MultiEntity ? multiEntityMetric : singleEntityMetric);

        const component = getTestingComponent(partWithExtraData, ReportType.Chart, savePartChanges, false, hasWaves, false);
        const { container } = render(component);
        expect(container).toBeInTheDocument();

        const user = userEvent.setup()

        const expectedReportPart = new PartDescriptor(reportPart);
        expectedReportPart.partType = expectedPartType;
        await user.click(screen.getByRole(AriaRoles.BUTTON, { name: clickedButtonName }));
        expect(savePartChanges).toHaveBeenLastCalledWith(expectedReportPart);
    });

    const selectedButtonTestCases = [
        [PartType.ReportsCardLine, WaveStatus.WithWaves, ChartType.Line],
        [PartType.ReportsCardLine, WaveStatus.WithoutWaves, ChartType.Bar]
    ];

    test.each(selectedButtonTestCases)("should select the correct button for part type %s %s", async (partType, hasWaves, expectedSelectedButtonName) => {
        const testingPartType = new PartDescriptor(reportPart);
        testingPartType.partType = partType;
        const partWithExtraData = getPartWithExtraData(testingPartType, singleEntityMetric);

        const component = getTestingComponent(partWithExtraData, ReportType.Chart, savePartChanges, false, hasWaves == WaveStatus.WithWaves, false);
        const { container } = render(component);
        expect(container).toBeInTheDocument();

        const lineButton = screen.getByRole(AriaRoles.BUTTON, { name: expectedSelectedButtonName });
        expect(lineButton).toHaveAttribute("aria-selected", "true");
    });
});