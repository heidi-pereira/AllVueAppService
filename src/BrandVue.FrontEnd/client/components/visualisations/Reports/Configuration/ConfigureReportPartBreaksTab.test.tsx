import React from "react";
import { render } from '@testing-library/react';
import "@testing-library/jest-dom";
import { Provider } from "react-redux";
import { PartDescriptor, ReportType, ReportOrder, MultipleEntitySplitByAndFilterBy, Report, ReportWaveConfiguration, PageDescriptor } from "../../../../BrandVueApi";
import { PartType } from '../../../panes/PartType';
import { PartWithExtraData } from '../ReportsPageDisplay';
import { Metric } from "../../../../metrics/metric";
import { getMetricWithEntityCombinations } from "../../../../helpers/ReactTestingLibraryHelpers";
import ConfigureReportPartBreaksTab, { IConfigureReportPartBreaksTabProps } from "./ConfigureReportPartBreaksTab";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { PageHandler } from "../../../PageHandler";
import { mock } from "jest-mock-extended";
import { configureStore } from '@reduxjs/toolkit';
import reportSlice from 'client/state/reportSlice';
import { initialReportErrorState } from 'client/components/visualisations/shared/ReportErrorState';
import subsetSlice from 'client/state/subsetSlice';

const mockBreaksPicker = jest.fn();
jest.mock("../../BreakPicker/BreaksPicker", () => ({
    __esModule: true,
    default: jest.fn((prop: any) => {
        mockBreaksPicker(prop);
        return (<div>MOCK</div>);
    })
}));

const getMockStore = () => {
    const report = {
        savedReportId: 1,
        pageId: 1,
        breaks: [],
        overTimeConfig: undefined,
        reportType: ReportType.Table
    } as any as Report;
    
    const page = {
        id: 1,
        name: "test-page",
        displayName: "Test Page"
    } as any as PageDescriptor;
    
    return configureStore({
        reducer: {
            report: reportSlice,
            subset: subsetSlice
        },
        preloadedState: {
            report: {
                allReports: [report],
                currentReportId: report.savedReportId,
                reportsPageOverride: page,
                errorState: initialReportErrorState,
                isLoading: false,
                isSettingsChange: false,
                isDataInSyncWithDatabase: true
            },
            subset: {
                subsetId: 'all',
                subsetConfigurations: []
            }
        }
    });
};

const getTestingComponent = (props: IConfigureReportPartBreaksTabProps): JSX.Element => {
    const store = getMockStore();
    return (
        <Provider store={store}>
            <ConfigureReportPartBreaksTab
                reportType={props.reportType}
                reportPart={props.reportPart}
                reportBreaks={props.reportBreaks}
                reportOrderBy={props.reportOrderBy}
                questionTypeLookup={props.questionTypeLookup}
                googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler}
                user={props.user}
                isUsingOverTime={false}
                savePartChanges={props.savePartChanges}
            />
        </Provider>
    );
};

describe("When a user tries to configure breaks", () => {
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
        return part;
    };

    const getDefaultProps = (reportPart: PartWithExtraData): IConfigureReportPartBreaksTabProps => ({
        reportType: ReportType.Table,
        reportPart: reportPart,
        reportBreaks: [],
        reportOrderBy: ReportOrder.ScriptOrderAsc,
        questionTypeLookup: {},
        googleTagManager: mock<IGoogleTagManager>(),
        pageHandler: mock<PageHandler>(),
        user: null,
        isUsingOverTime: false,
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
        expect(mockBreaksPicker).toHaveBeenLastCalledWith(expect.objectContaining({ isDisabled: true }));
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
        expect(mockBreaksPicker).toHaveBeenLastCalledWith(expect.objectContaining({ isDisabled: false }));
    });

    const disabledWaveBreakCombinationTestCases = [
        [PartType.ReportsCardFunnel],
    ];

    test.each(disabledWaveBreakCombinationTestCases)("should be disabled for %s when waves in use", (partType) => {
        const reportPart = getReportPart();
        reportPart.partType = partType;
        reportPart.waves = mock<ReportWaveConfiguration>();
        const props = getDefaultProps(getPartWithExtraData(reportPart, getMetricWithEntityCombinations(1)));
        const component = getTestingComponent(props);
        const { container } = render(component);

        expect(container.getElementsByClassName("warning-box").length).toEqual(1);
        expect(mockBreaksPicker).toHaveBeenLastCalledWith(expect.objectContaining({ isDisabled: true }));
    });
});