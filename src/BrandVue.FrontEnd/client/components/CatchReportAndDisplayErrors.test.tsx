import React from "react";
import { CatchReportAndDisplayErrors as CatchReportAndDisplayErrors } from "./CatchReportAndDisplayErrors";
import { dsession } from "../dsession";
import { getMockApiCalls } from '../helpers/MockBrandVueApi';
import { NoDataError } from "../NoDataError";
import { createMockSession, createMockApplicationConfiguration } from '../helpers/MockSession';
import { MockActiveView, MockCuratedFilters } from '../helpers/MockApp';
import defineProperty from "../helpers/defineProperty";
import { act, render, screen } from '@testing-library/react';
import userEvent from "@testing-library/user-event";
import "@testing-library/jest-dom";
import { ApplicationConfiguration } from "../ApplicationConfiguration";
import { getDateInUtc } from "./helpers/PeriodHelper";
import { MockRouter } from "../helpers/MockRouter";
import { Provider } from "react-redux";
import { setupStore } from "client/state/store";

const OuterComponentTestId: string = "OuterComponent";
const ExpectedStartPageName: string = "Welcome";

const Outer = (props: { session: dsession, applicationConfiguration: ApplicationConfiguration, expectedErrorMessage: string, shouldDependOnData: boolean}) => {
    return (
        <div id="outerComponent" data-testid={OuterComponentTestId}>
            <CatchReportAndDisplayErrors
                applicationConfiguration={props.applicationConfiguration}
                childInfo={{}}
                url={"/"}
                startPagePath={"/ui/welcome"}
                startPageName={ExpectedStartPageName}>
                <Inner session={props.session} applicationConfiguration={props.applicationConfiguration} expectedErrorMessage={props.expectedErrorMessage} shouldDependOnData={props.shouldDependOnData} />
            </CatchReportAndDisplayErrors>
        </div>
    );
}

const InnerComponentTestId: string = "InnerComponent";
const ThrowGenericErrorButtonId: string = "ThrowGenericError";
const ThrowNoDataErrorButtonId: string = "ThrowNoDataError";

const Inner = (props: { session: dsession, applicationConfiguration: ApplicationConfiguration, expectedErrorMessage: string, shouldDependOnData: boolean }) => {

    const [shouldThrowGenericError, setShouldThrowGenericError] = React.useState(false);
    const [shouldThrowNoDataError, setShouldThrowNoDataError] = React.useState(false);
    const [isFirstRender, setIsFirstRender] = React.useState(true);

    const throwError = () => {
        setShouldThrowGenericError(true);
    }

    const throwNoDataError = () => {
        setShouldThrowNoDataError(true);
    }


    if (!props.applicationConfiguration.hasLoadedData && props.shouldDependOnData) {
        // Avoid oddity (bug?) where component won't be rendered if it throws immediately on first render
        if (isFirstRender) {
            Promise.resolve().then(() => setIsFirstRender(false));
        } else {
            console.info("Please ignore the error below printed by the console. It is required for the tests.");
            throw new Error();
        }
    }

    if (shouldThrowGenericError) {
        console.info("Please ignore the error below printed by the console. It is required for the tests.");
        throw new Error(props.expectedErrorMessage);
    }

    if (shouldThrowNoDataError) {
        console.info("Please ignore the error below printed by the console. It is required for the tests.");
        throw new NoDataError();
    }
    
    return <div id="innerComponent" data-testid={InnerComponentTestId}>
        <button onClick={() => throwError()} data-testid="ThrowGenericError">Throw error</button>
        <button onClick={() => throwNoDataError()} data-testid="ThrowNoDataError">Throw no data error</button>
        Should not be rendered
    </div>;
}

type MockApplicationFlagsForErrorTesting = { shouldDependOnData: boolean }

const getTestingComponent = (
    session: dsession,
    applicationConfiguration: ApplicationConfiguration,
    expectedErrorMessage: string,
    mockMountApplicationFlagsForErrorTesting: MockApplicationFlagsForErrorTesting
): JSX.Element => (
    <Outer
        session={session}
        applicationConfiguration={applicationConfiguration}
        expectedErrorMessage={expectedErrorMessage}
        shouldDependOnData={mockMountApplicationFlagsForErrorTesting.shouldDependOnData}
    />
);


describe("Encapsulating intentionally broken component", () => {
    const expectedErrorMessage = 'Intentional error to be caught by component under test';

    beforeAll(async () => {
        const session = createMockSession();
        const applicationConfiguration = createMockApplicationConfiguration(getDateInUtc(2017, 7, 1), getDateInUtc(2018, 8, 31));
        const mockAppConfig = { shouldDependOnData: false };
        const store = setupStore({ subset: { subsetId: 'all', subsetConfigurations: [] } });
        const outerComponent = getTestingComponent(session, applicationConfiguration, expectedErrorMessage, mockAppConfig);
        render(
            <Provider store={store}>
                <MockRouter initialEntries={['/']}>
                    {outerComponent}
                </MockRouter>
            </Provider>
        );
    });

    //getMockApiCalls needs this to be separate to the render() call
    it("Should display error component", async () => {

        const user = userEvent.setup();
        await user.click(screen.getByTestId(ThrowGenericErrorButtonId));

        //Does not render inner component when there is an error
        expect(screen.queryByTestId(InnerComponentTestId)).toBeNull();

        //Stops errors propagating to break parent
        expect(screen.getByTestId(OuterComponentTestId)).toBeVisible();

        //Displays link to back to start page
        expect(screen.getByRole('link', {name: ExpectedStartPageName})).toBeVisible();
        
        //Reports errors to server
        const calls = getMockApiCalls();
        const errorCalls = calls.filter(call => call.requestUrl.endsWith('/LogError'));
        expect(errorCalls.length).toEqual(1);
        expect(errorCalls[0].body).toContain(expectedErrorMessage);
    });
});

test("Given a component with no data, should show no data page", async () => {
    const session = createMockSession();
    const applicationConfiguration = createMockApplicationConfiguration(getDateInUtc(2017, 7, 1), getDateInUtc(2018, 8, 31));
    defineProperty(session, new MockActiveView(), 'activeView');
    const mockAppConfig = { shouldDependOnData: false };
    const outerComponent = getTestingComponent(session, applicationConfiguration, "", mockAppConfig);
    const store = setupStore({ subset: { subsetId: 'all', subsetConfigurations: [] } });

    await act(async () => {
        render(
            <Provider store={store}>
                <MockRouter initialEntries={['/']}>
                    {outerComponent}
                </MockRouter>
            </Provider>
        );
    });

    await act(async () => {
        const user = userEvent.setup();
        await user.click(screen.getByTestId(ThrowNoDataErrorButtonId));
    });

    expect(screen.queryByTestId(InnerComponentTestId)).toBeNull();
    expect(screen.getByTestId(OuterComponentTestId)).toBeVisible();
    expect(screen.getByText('reset your filters', { exact: false })).toBeVisible();
});