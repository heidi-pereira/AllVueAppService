import React, { ReactElement } from "react";
import { enrichSession, MockApplication, SessionEnrichmentOptions } from '../../helpers/MockApp';
import * as BrandVueApi from "../../BrandVueApi";
import {
    createMockSession,
    createMockApplicationConfiguration
} from '../../helpers/MockSession';
import { dsession } from "../../dsession";
import FixedPeriodDatePicker from "./FixedPeriodDatePicker";
import moment from "moment";
import { getDateInUtc } from "../helpers/PeriodHelper";
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from "@testing-library/user-event";
import "@testing-library/jest-dom";
import FixedPeriodUnitDescriptions from "../helpers/FixedPeriodUnitDescriptions";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { MixPanel } from "../mixpanel/MixPanel";
import { MixPanelClientTest } from "../mixpanel/MixPanelClientTest";
import { MixPanelModel } from "../mixpanel/MixPanelHelper";
import {MockRouter} from "../../helpers/MockRouter";
import { mock } from "jest-mock-extended";
import { IGoogleTagManager } from "../../googleTagManager";
import { QueryStringParamNames, useReadVueQueryParams, useWriteVueQueryParams } from "../helpers/UrlHelper";
import { useLocation, useNavigate } from "react-router-dom";
import { Provider } from "react-redux";
import { setupStore } from "client/state/store";

const mockAddEvent = jest.fn();

const mockTagManagerInstance = {
    addEvent: mockAddEvent,
};
jest.mock('../../googleTagManager', () => ({
    useGoogleTagManager: function() {
        return mockTagManagerInstance;
    }
}));

// Mock the readVueQueryParams module
jest.mock("../helpers/UrlHelper", () => {
    const original = jest.requireActual("../helpers/UrlHelper");
    return {
        ...original,
        QueryStringParamNames: {
            end: "End",
            start: "Start",
            range: "Range",
            now: "Now",
            average: "Average"
        },
        useReadVueQueryParams: jest.fn(() => ({
            getQueryParameter: jest.fn(() => undefined)
        })),
        useWriteVueQueryParams: jest.fn()
    };
});

jest.mock("react-router-dom", () => {
    const original = jest.requireActual("react-router-dom");
    return {
        ...original,
        useLocation: jest.fn(),
        useNavigate: jest.fn()
    };
});
const mockSetQueryParameters = jest.fn();

const getTestingComponent = (session: dsession, googleTagManager: IGoogleTagManager, applicationConfiguration: ApplicationConfiguration, showRollingAverages: boolean): JSX.Element => (
    <FixedPeriodDatePicker pageHandler={session.pageHandler}
        activeMetrics={session.activeView.activeMetrics}
        curatedFilters={session.activeView.curatedFilters}
        userVisibleAverages={session.averages}
        applicationConfiguration={applicationConfiguration}
        googleTagManager={googleTagManager}
        showRollingAverages={showRollingAverages}
        readVueQueryParams={useReadVueQueryParams()}
        writeVueQueryParams={useWriteVueQueryParams(useNavigate(), useLocation())}
    />
);

const getMockApplication = (averageType: string, showRollingAverages?: boolean): { app: MockApplication, component: ReactElement  } => {
    const mockGetQueryParameter = jest.fn().mockImplementation((paramName: string) => {
        if(paramName === QueryStringParamNames.average) {
            return averageType;
        } else if (paramName === QueryStringParamNames.end) {
            return "2018-08-31";
        }
    });
   
    (useReadVueQueryParams as jest.Mock).mockReturnValue({
        getQueryParameter: mockGetQueryParameter
    });

    (useWriteVueQueryParams as jest.Mock).mockReturnValue({
        setQueryParameters: mockSetQueryParameters
    });
    
    const session = createMockSession();
    const sessionOptions : SessionEnrichmentOptions = {
        averageType: averageType,
    }
    const googleTagManager = mock<IGoogleTagManager>();
    const mixPanelModelInstance: MixPanelModel = {
        userId: "userIdTest",
        projectId: "mixPanelTokenTest",
        client: new MixPanelClientTest(),
        isAllVue: false,
        productName: "BrandVue",
        project: "subProductIdTest",
        kimbleProposalId: "",
    };
    MixPanel.init(mixPanelModelInstance);
    enrichSession(session, sessionOptions);
    const applicationConfiguration = createMockApplicationConfiguration(getDateInUtc(2017, 7, 1), getDateInUtc(2018, 8, 31));
    const store = setupStore({
        subset: { subsetId: 'all', subsetConfigurations: [] }
    });
    const fixedPeriodDataPicker = getTestingComponent(session, googleTagManager, applicationConfiguration, showRollingAverages ?? true);

    return {
        app: new MockApplication(session),
        component: (
            <Provider store={store}>
                <MockRouter initialEntries={['/']}>
                    {fixedPeriodDataPicker}
                </MockRouter>
            </Provider>
        )
    };
}

describe("Period picker renders correct UI based on average type", () => {
    afterEach(() => {
        cleanup();
    });
    
    it("should render month and year pickers for Monthly average", () => {
        const mockApp = getMockApplication("Monthly");
        render(mockApp.component);
        
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)})).toBeVisible();
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.CalendarYearEnd)})).toBeVisible();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.QuarterEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.HalfYearEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.WeekEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.Day)})).toBeNull();
    });

    it("should render month and year pickers for Monthly (over 3 months)", () => {
        const mockApp = getMockApplication("MonthlyOver3Months");
        render(mockApp.component);
        
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)})).toBeVisible();
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.CalendarYearEnd)})).toBeVisible();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.QuarterEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.HalfYearEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.WeekEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.Day)})).toBeNull();
    });

    it("should render quarter and year pickers for Quarterly", () => {
        const mockApp = getMockApplication("Quarterly");
        render(mockApp.component);
        
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.QuarterEnd)})).toBeVisible();
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.CalendarYearEnd)})).toBeVisible();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.HalfYearEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.WeekEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.Day)})).toBeNull();
    });

    it("should render half yearly and year pickers for HalfYearly", () => {
        const mockApp = getMockApplication("HalfYearly");
        render(mockApp.component);
        
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.HalfYearEnd)})).toBeVisible();
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.CalendarYearEnd)})).toBeVisible();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.QuarterEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.WeekEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.Day)})).toBeNull();
    });

    it("should render just year picker for Annual", () => {
        const mockApp = getMockApplication("Annual");
        render(mockApp.component);
        
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.CalendarYearEnd)})).toBeVisible();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.QuarterEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.HalfYearEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.WeekEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.Day)})).toBeNull();
    });

    it("should render just day picker for Weekly", () => {
        const mockApp = getMockApplication("Weekly");
        render(mockApp.component);
        
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.Day)})).toBeVisible();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.QuarterEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.HalfYearEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.WeekEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.CalendarYearEnd)})).toBeNull();
    });

    it("should render just day picker for Daily", () => {
        const mockApp = getMockApplication("Daily");
        render(mockApp.component);
        
        expect(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.Day)})).toBeVisible();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.QuarterEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.HalfYearEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.WeekEnd)})).toBeNull();
        expect(screen.queryByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.CalendarYearEnd)})).toBeNull();
    });
});

describe("When a user wants to change the average type", () => {
    let mockApp: { app: MockApplication, component: ReactElement };

    beforeEach(() => {
        mockApp = getMockApplication("Monthly");
        render(mockApp.component);
    });

    afterEach(() => {
        cleanup();
        jest.clearAllMocks();
    });

    it("should not be displaying the dropdown menu on load", () => {
        const dropdownElement = screen.queryByRole('menu');
        expect(dropdownElement).toBeNull();
    });

    it("should display dropdown options correctly if a menu is clicked", async () => {
        const user = userEvent.setup();
        await user.click(screen.getByRole('button', {name: "Select a period"}));

        const dropdownElement = screen.getByRole('menu');
        expect(dropdownElement).toBeVisible();
    });

    it("should display rolling averages dropdown options if a menu is clicked", async () => {
        const user = userEvent.setup();
        await user.click(screen.getByRole('button', { name: "Select a period" }));

        const rollingPeriod = screen.queryByRole('menuitem', { name: "14 days" });
        expect(rollingPeriod).toBeVisible();
    });

    it("should update session and query string when user selects a different average", async () => {
        const user = userEvent.setup();
        await user.click(screen.getByRole('button', {name: "Select a period"}));

        await user.click(screen.getByRole('menuitem', {name: "Quarterly"}));

        const dropdownElement = screen.queryByRole('menu');
        expect(dropdownElement).toBeNull();
        
        const newStartDateString = moment.utc(getDateInUtc(2017, 8, 1)).format("YYYY-MM-DD");
        const newMomentEndDateString = moment.utc(getDateInUtc(2018, 6, 30)).format("YYYY-MM-DD");

        expect(mockSetQueryParameters).toHaveBeenCalledWith(expect.arrayContaining([
            { name: QueryStringParamNames.range, value: "" },
            { name: QueryStringParamNames.start, value: newStartDateString },
            { name: QueryStringParamNames.end, value: newMomentEndDateString },
            { name: "Average", value: "Quarterly" }
        ]));
    });
});

describe("When a user wants to change the average type without rolling averages", () => {
    let mockApp: { app: MockApplication, component: ReactElement };

    beforeEach(() => {
        mockApp = getMockApplication("Monthly", false);
        render(mockApp.component);
    });

    afterEach(() => {
        cleanup();
    });

    it("should not display rolling averages dropdown options if a menu is clicked", async () => {
        const user = userEvent.setup();
        await user.click(screen.getByRole('button', {name: "Select a period"}));

        const rollingPeriod = screen.queryByRole('menuitem', { name: "14 days" });
        expect(rollingPeriod).toBeNull();
    });
});

describe("When a user wants to change a FixedPeriodSelector value", () => {
    let mockApp: { app: MockApplication, component: ReactElement };

    beforeEach(() => {
        mockApp = getMockApplication("Monthly");
        render(mockApp.component);
    });

    afterEach(() => {
        cleanup();
    });

    it("should not be displaying the dropdown menu on load", () => {
        const dropdownElement = screen.queryByRole('menu');
        expect(dropdownElement).toBeNull();
    });

    it("should display dropdown options correctly if a menu is clicked", async () => {
        const user = userEvent.setup();
        await user.click(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)}));

        const dropdownElement = screen.getByRole('menu');
        expect(dropdownElement).toBeVisible();
    });

    it("should update session when user selects a different period", async () => {

        const user = userEvent.setup();
        await user.click(screen.getByRole('button', {name: FixedPeriodUnitDescriptions.getPeriodLabel(BrandVueApi.MakeUpTo.MonthEnd)}));

        await user.click(screen.getByRole('menuitem', {name: "January"}));

        const dropdownElement = screen.queryByRole('menu');
        expect(dropdownElement).toBeNull();

        const newStartDateString = moment.utc(getDateInUtc(2017, 8, 1)).format("YYYY-MM-DD");
        const newMomentEndDateString = moment.utc(getDateInUtc(2018, 1, 31)).format("YYYY-MM-DD");
        const newStartDate = moment.utc(newStartDateString).toDate();
        const newEndDate = moment.utc(newMomentEndDateString).toDate();

        expect(mockApp.app.session.activeView.curatedFilters.startDate).toEqual(newStartDate);
        expect(mockApp.app.session.activeView.curatedFilters.endDate).toEqual(newEndDate);
    });
});
