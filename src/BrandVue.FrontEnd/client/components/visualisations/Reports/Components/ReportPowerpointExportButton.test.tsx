import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import "@testing-library/jest-dom";
import { UserContext } from '../../../../GlobalContext';
import { useAsyncExportContext } from '../Utility/AsyncExportContext';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { isFeatureEnabled } from '../../../helpers/FeaturesHelper';
import ReportPowerpointExportButton, { IReportPowerpointExportButtonProps } from './ReportPowerpointExportButton';
import {mockApplicationUser, getMetrics, mockReport, generateMetric, getEntitySet, AriaRoles} from '../../../../helpers/ReactTestingLibraryHelpers';
import { ReportWithPage } from '../ReportsPage';
import { PageDescriptor, PartDescriptor } from 'client/BrandVueApi';
import { CuratedFilters } from 'client/filter/CuratedFilters';
import { PartWithExtraData } from '../ReportsPageDisplay';
import { store, useAppSelector } from 'client/state/store';
import { Provider } from 'react-redux';

jest.mock('client/state/store', () => {
    const actual = jest.requireActual('client/state/store');
    return {
        ...actual,
        useAppSelector: jest.fn(),
    };
});

jest.mock('../Utility/AsyncExportContext', () => ({
    useAsyncExportContext: jest.fn(),
}));

jest.mock('../../../../entity/EntityConfigurationStateContext', () => ({
    useEntityConfigurationStateContext: jest.fn(),
}));

jest.mock('../../../helpers/FeaturesHelper', () => ({
    isFeatureEnabled: jest.fn(),
}));

const mockuseAppSelector = useAppSelector as jest.Mock;
const mockUseAsyncExportContext = useAsyncExportContext as jest.Mock;
const mockUseEntityConfigurationStateContext = useEntityConfigurationStateContext as jest.Mock;
const mockIsFeatureEnabled = isFeatureEnabled as jest.Mock;
const mockPartWithExtraData: PartWithExtraData = {
    part: {
    } as PartDescriptor,
    metric: generateMetric("TestMetric"),
    ref: React.createRef<HTMLDivElement>(),
    selectedEntitySet: getEntitySet("TestEntitySet"),
};
const mockReportPowerpointExportButtonProps: IReportPowerpointExportButtonProps = {
    metrics: getMetrics(2),
    curatedFilters: {} as CuratedFilters,
    overTimeFilters: {} as CuratedFilters,
    isDataInSyncWithDatabase: true,
    reportPart: mockPartWithExtraData
};

const renderComponent = (props: IReportPowerpointExportButtonProps) => {
    return render(
        <Provider store={store}>
            <UserContext.Provider value={mockApplicationUser}>
                <ReportPowerpointExportButton {...props} />
            </UserContext.Provider>
        </Provider>
    );
}

describe('ReportPowerpointExportButton', () => {
    beforeEach(() => {
        const mockPage: PageDescriptor = {
            id: 1,
            name: 'test-page',
            displayName: 'Test Page'
        } as PageDescriptor;
        
        // Mock useAppSelector to return different values based on the selector function
        mockuseAppSelector.mockImplementation((selector: any) => {
            // Handle state.report.errorState selector
            if (typeof selector === 'function') {
                const mockState = {
                    report: {
                        errorState: { isError: false },
                        allReports: [mockReport],
                        currentReportId: mockReport.savedReportId,
                        reportsPageOverride: mockPage
                    },
                    subset: { subsetId: 'all' },
                    timeSelection: { scorecardPeriod: 'Monthly' },
                    average: { allAverages: [{ averageId: 'Monthly', isDefault: true }] }
                };
                const result = selector(mockState);
                return result;
            }
            return { isError: false };
        });
        mockUseAsyncExportContext.mockReturnValue({ pendingExports: [], exportDispatch: jest.fn() });
        mockUseEntityConfigurationStateContext.mockReturnValue({ entityConfiguration: {} });
        mockIsFeatureEnabled.mockReturnValue(false);
    });

    test('renders without crashing', () => {
        renderComponent(mockReportPowerpointExportButtonProps);
        const button = screen.getByRole(AriaRoles.BUTTON, { name: 'Export' });
        expect(button).toBeInTheDocument();
    });

    test('disables export button when there is an error', () => {
        const mockPage: PageDescriptor = {
            id: 1,
            name: 'test-page',
            displayName: 'Test Page'
        } as PageDescriptor;
        
        mockuseAppSelector.mockImplementation((selector: any) => {
            if (typeof selector === 'function') {
                const mockState = {
                    report: {
                        errorState: { isError: true },
                        allReports: [mockReport],
                        currentReportId: mockReport.savedReportId,
                        reportsPageOverride: mockPage
                    },
                    subset: { subsetId: 'all' },
                    timeSelection: { scorecardPeriod: 'Monthly' },
                    average: { allAverages: [{ averageId: 'Monthly', isDefault: true }] }
                };
                const result = selector(mockState);
                return result;
            }
            return { isError: true };
        });
        renderComponent(mockReportPowerpointExportButtonProps);
        const button = screen.getByRole(AriaRoles.BUTTON, { name: 'Export' });
        expect(button).toBeDisabled();
    });
});