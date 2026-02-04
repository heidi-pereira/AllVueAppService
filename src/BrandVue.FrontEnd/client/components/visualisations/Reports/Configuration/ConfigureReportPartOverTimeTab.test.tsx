import React from "react";
import { render } from '@testing-library/react';
import "@testing-library/jest-dom";
import { PartDescriptor, MultipleEntitySplitByAndFilterBy, ReportWaveConfiguration, ReportWavesOptions, Report, CrossMeasure, PageDescriptor, ReportType, ReportOrder, CrosstabSignificanceType, BaseDefinitionType, DisplaySignificanceDifferences, SigConfidenceLevel } from "../../../../BrandVueApi";
import { PartType } from '../../../panes/PartType';
import { PartWithExtraData } from '../ReportsPageDisplay';
import { Metric } from "../../../../metrics/metric";
import { getMetricWithEntityCombinations } from "../../../../helpers/ReactTestingLibraryHelpers";
import ConfigureReportPartOverTimeTab, { IConfigureReportPartOverTimeTabProps } from "./ConfigureReportPartOverTimeTab";
import { mock } from "jest-mock-extended";
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import reportSlice from 'client/state/reportSlice';
import { initialReportErrorState } from 'client/components/visualisations/shared/ReportErrorState';

const mockReportWavesPicker = jest.fn();
jest.mock("../Components/ReportWavesPicker", () => ({
    __esModule: true,
    default: jest.fn((prop: any) => {
        mockReportWavesPicker(prop);
        return (<div>MOCK</div>);
    })
}));


jest.mock('../../../helpers/FeaturesHelper', () => ({
    isFeatureEnabled: jest.fn().mockReturnValue(true),
}));

const getMockReport = (): Report => {
    return {
        savedReportId: 1,
        pageId: 1,
        breaks: [],
        overTimeConfig: undefined,
        reportType: ReportType.Chart
    } as any as Report;
};

const getMockPage = (): PageDescriptor => {
    return {
        id: 1,
        name: "test-page",
        displayName: "Test Page"
    } as any as PageDescriptor;
};

const getMockStore = () => {
    const report = getMockReport();
    const page = getMockPage();
    
    return configureStore({
        reducer: {
            report: reportSlice
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
            }
        }
    });
};

const getTestingComponent = (props: IConfigureReportPartOverTimeTabProps): JSX.Element => {
    const store = getMockStore();
    return (
        <Provider store={store}>
            <ConfigureReportPartOverTimeTab
                reportPart={props.reportPart}
                reportWaves={props.reportWaves}
                questionTypeLookup={props.questionTypeLookup}
                savePartChanges={props.savePartChanges}
            />
        </Provider>
    );
};

describe("When a user tries to configure waves", () => {
    const getPartWithExtraData = (part: PartDescriptor, metric: Metric) => {
        return {
            part: part,
            metric: metric,
            ref: React.createRef<HTMLDivElement>(),
            selectedEntitySet: undefined,
        };
    };

    const getReportPart = () => {
        const part = new PartDescriptor();
        part.partType = PartType.ReportsCardChart;
        part.breaks = [];
        part.overrideReportBreaks = false;
        part.multipleEntitySplitByAndFilterBy = new MultipleEntitySplitByAndFilterBy({
            splitByEntityType: "",
            filterByEntityTypes: []
        });
        part.waves = new ReportWaveConfiguration({
            waves: undefined,
            wavesToShow: ReportWavesOptions.SelectedWaves,
            numberOfRecentWaves: 3
        });
        part.showOvertimeData = false;
        return part;
    };

    const getDefaultProps = (reportPart: PartWithExtraData): IConfigureReportPartOverTimeTabProps => ({
        reportPart: reportPart,
        reportWaves: undefined,
        questionTypeLookup: {},
        savePartChanges: jest.fn()
    });

    const disabledTestCases = [
        [PartType.ReportsCardDoughnut],
    ];

    test.each(disabledTestCases)("should be disabled for %s", (partType) => {
        const reportPart = getReportPart();
        reportPart.partType = partType;
        const props = getDefaultProps(getPartWithExtraData(reportPart, getMetricWithEntityCombinations(1)));
        const component = getTestingComponent(props);
        const { container } = render(component);

        expect(container.getElementsByClassName("warning-box").length).toEqual(1);
        expect(mockReportWavesPicker).toHaveBeenLastCalledWith(expect.objectContaining({isDisabled: true}));
    });

    const enabledTestCases = [
        [PartType.ReportsCardChart],
        [PartType.ReportsCardLine],
        [PartType.ReportsCardMultiEntityMultipleChoice],
        [PartType.ReportsCardStackedMulti],
        [PartType.ReportsCardText],
    ];

    test.each(enabledTestCases)("should be enabled for %s", async (partType) => {
        const reportPart = getReportPart();
        reportPart.partType = partType;
        const props = getDefaultProps(getPartWithExtraData(reportPart, getMetricWithEntityCombinations(1)));
        const component = getTestingComponent(props);
        const { container } = render(component);

        expect(container.getElementsByClassName("warning-box").length).toEqual(0);
        expect(mockReportWavesPicker).toHaveBeenLastCalledWith(expect.objectContaining({ isDisabled: false }));
    });

    const disabledWaveBreakCombinationTestCases = [
        [PartType.ReportsCardFunnel],
    ];

    test.each(disabledWaveBreakCombinationTestCases)("should be disabled for %s when breaks in use", (partType) => {
        const reportPart = getReportPart();
        reportPart.partType = partType;
        reportPart.breaks = [mock<CrossMeasure>()];
        reportPart.overrideReportBreaks = true;
        const props = getDefaultProps(getPartWithExtraData(reportPart, getMetricWithEntityCombinations(1)));
        const component = getTestingComponent(props);
        const { container } = render(component);

        expect(container.getElementsByClassName("warning-box").length).toEqual(1);
        expect(mockReportWavesPicker).toHaveBeenLastCalledWith(expect.objectContaining({ isDisabled: false }));
    });
});