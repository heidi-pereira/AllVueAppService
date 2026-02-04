import { IPageContext } from "../helpers/PagesHelper";
import { IAverageDescriptor, MakeUpTo, PageDescriptor } from "../../BrandVueApi";
import { ViewType, ViewTypeEnum } from "../helpers/ViewTypeHelper";
import { dsession } from "../../dsession";
import ScorecardFilters from "./ScorecardFilters";
import * as PagesHelper from "../helpers/PagesHelper";
import React from "react";
import { MockApplication, SessionEnrichmentOptions } from "../../helpers/MockApp";
import { createMockSession, createMockApplicationConfiguration } from "../../helpers/MockSession";
import { getDateInUtc } from "../helpers/PeriodHelper";
import moment from "moment/moment";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import "@testing-library/jest-dom";
import { ApplicationConfiguration } from "../../ApplicationConfiguration";
import { MockRouter } from "../../helpers/MockRouter";
import { Provider } from "react-redux";
import { setupStore } from "client/state/store";
import { MockStoreBuilder } from "../../helpers/MockStore";

const mockTrackEvent = jest.fn();
const mockTrackPageView = jest.fn();

const mockTagManagerInstance = {
    trackEvent: mockTrackEvent,
    trackPageView: mockTrackPageView,
};
jest.mock("../../googleTagManager", () => ({
    useGoogleTagManager: function () {
        return mockTagManagerInstance;
    },
}));

const mockSetQueryParameters = jest.fn();
const mockSetQueryParameter = jest.fn();

jest.mock("../helpers/UrlHelper", () => ({
    ...jest.requireActual("../helpers/UrlHelper"),
    useWriteVueQueryParams: () => ({
        setQueryParameters: mockSetQueryParameters,
        setQueryParameter: mockSetQueryParameter,
    }),
}));

const mockPageInfo: IPageContext = {
    page: new PageDescriptor(),
    pagePart: "/summary/summary-scorecard",
    activeViews: [
        new ViewType(ViewTypeEnum.Performance, "Performance", "multiline_chart"),
        new ViewType(ViewTypeEnum.PerformanceVsPeers, "Performance vs Key competitors", "multiline_chart"),
    ],
};

const getTestingComponent = (session: dsession, applicationConfiguration: ApplicationConfiguration): JSX.Element => (
    <ScorecardFilters session={session} applicationConfiguration={applicationConfiguration} pageHandler={session.pageHandler} averages={session.averages} />
);

const additionalSessionOptions: ((session: dsession) => void)[] = [
    (session: dsession) => {
        jest.spyOn(PagesHelper, "getPageInfo").mockImplementation((location?: string) => mockPageInfo);
    },
];

const averageFilter = (average: IAverageDescriptor) =>
    average.makeUpTo === MakeUpTo.MonthEnd || average.makeUpTo === MakeUpTo.QuarterEnd || average.makeUpTo === MakeUpTo.CalendarYearEnd;

const ANNUAL = "Annual";
const QUARTERLY = "Quarterly";
const MONTHLY = "Monthly";

describe("When a user wants to change the average type or date range", () => {
    let mockApp: MockApplication;

    beforeEach(async () => {
        mockSetQueryParameters.mockClear();
        const session = createMockSession(averageFilter);
        const sessionOptions: SessionEnrichmentOptions = {
            averageType: ANNUAL,
            selectedView: 6,
        };
        const applicationConfiguration = createMockApplicationConfiguration(getDateInUtc(2017, 5, 1), getDateInUtc(2019, 5, 15));
        const fixedPeriodDataPicker = getTestingComponent(session, applicationConfiguration);
        mockApp = new MockApplication(session);
        mockApp.enrichSession(sessionOptions, additionalSessionOptions);
        const store = setupStore(new MockStoreBuilder().setScorecardAverage(ANNUAL).build());
        render(
            <Provider store={store}>
                <MockRouter initialEntries={["/"]}>{fixedPeriodDataPicker}</MockRouter>
            </Provider>
        );
    });

    it("should not be displaying the dropdown menu on load", () => {
        const dropdownElement = screen.queryByRole("menu");
        expect(dropdownElement).toBeNull();
    });

    it("should display dropdown options correctly if a menu is clicked", async () => {
        const user = userEvent.setup();
        await user.click(screen.getByRole("button", { name: ANNUAL }));

        const dropdownElement = screen.queryByRole("menu");
        expect(dropdownElement).toBeVisible();
    });

    it("should advance the dates a year when the right arrow is clicked", async () => {
        mockSetQueryParameters.mockClear();
        const user = userEvent.setup();
        await user.click(screen.getByRole("button", { name: "keyboard_arrow_right" }));

        const expectedEndDate = moment.utc(getDateInUtc(2019, 12, 31)).format("YYYY-MM-DD");
        expect(mockSetQueryParameters).toHaveBeenCalledWith([{ name: "End", value: expectedEndDate }]);
    });

    it("should update session and url when user selects a different average", async () => {
        mockSetQueryParameters.mockClear();
        const user = userEvent.setup();
        await user.click(screen.getByRole("button", { name: ANNUAL }));
        await user.click(screen.getByRole("menuitem", { name: QUARTERLY }));
        const expectedEndDate = moment.utc(getDateInUtc(2018, 12, 31)).format("YYYY-MM-DD");
        expect(mockSetQueryParameters).toHaveBeenCalledWith([{ name: "End", value: expectedEndDate }]);
    });

    it("should update session and url when user goes back a period", async () => {
        const user = userEvent.setup();
        await user.click(screen.getByRole("button", { name: "keyboard_arrow_left" }));

        const expectedEndDate = moment.utc(getDateInUtc(2017, 12, 31)).format("YYYY-MM-DD");
        expect(mockSetQueryParameters).toHaveBeenCalledWith([{ name: "End", value: expectedEndDate }]);
    });
});

describe("When there is no date range set in the Url and 'dateOfLastDataPoint' is the end of a month", () => {
    let mockApp: MockApplication;

    beforeAll(async () => {
        const session = createMockSession(averageFilter);
        const sessionOptions: SessionEnrichmentOptions = {
            averageType: MONTHLY,
            selectedView: 6,
        };
        const applicationConfiguration = createMockApplicationConfiguration(getDateInUtc(2017, 5, 1), getDateInUtc(2019, 10, 31));
        const fixedPeriodDataPicker = getTestingComponent(session, applicationConfiguration);
        mockApp = new MockApplication(session);
        mockApp.enrichSession(sessionOptions, additionalSessionOptions);
        const store = setupStore(new MockStoreBuilder().build());
        render(
            <Provider store={store}>
                <MockRouter initialEntries={["/"]}>{fixedPeriodDataPicker}</MockRouter>
            </Provider>
        );
    });

    it("should set the end date to the same day as 'dateOfLastDataPoint'", () => {
        const expectedEndDate = moment.utc(getDateInUtc(2019, 10, 31)).format("YYYY-MM-DD");
        expect(mockSetQueryParameters).toHaveBeenCalledWith([{ name: "End", value: expectedEndDate }]);
    });
});

describe("When the latest month is into the next quarter", () => {
    let mockApp: MockApplication;
    let store;

    beforeAll(async () => {
        const session = createMockSession(averageFilter);
        const sessionOptions: SessionEnrichmentOptions = {
            averageType: MONTHLY,
            selectedView: 6,
        };
        const applicationConfiguration = createMockApplicationConfiguration(getDateInUtc(2017, 5, 1), getDateInUtc(2019, 10, 31));
        const fixedPeriodDataPicker = getTestingComponent(session, applicationConfiguration);
        mockApp = new MockApplication(session);
        mockApp.enrichSession(sessionOptions, additionalSessionOptions);
        store = setupStore(new MockStoreBuilder().build());
        render(
            <Provider store={store}>
                <MockRouter initialEntries={["/"]}>{fixedPeriodDataPicker}</MockRouter>
            </Provider>
        );
    });

    it("should cap the date at the end of the previous quarter when selecting the quarterly average", async () => {
        const user = userEvent.setup();
        const expectedEndDate = moment.utc(getDateInUtc(2019, 9, 30)).format("YYYY-MM-DD");
        await user.click(screen.getByRole("button", { name: MONTHLY }));
        await user.click(screen.getByRole("menuitem", { name: QUARTERLY }));
        expect(mockSetQueryParameters).toHaveBeenCalledWith([{ name: "End", value: expectedEndDate }]);
        const state = store.getState();
        expect(state.timeSelection.scorecardPeriod).toBe("Quarterly");
    });
});
