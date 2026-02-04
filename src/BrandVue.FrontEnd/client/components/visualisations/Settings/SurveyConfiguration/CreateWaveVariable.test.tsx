// TypeScript (React, Jest, React Testing Library)
import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom';
import userEvent from "@testing-library/user-event";
import CreateWaveVariableButton from "./CreateWaveVariable";
import { VariableContext } from "../../Variables/VariableModal/Utils/VariableContext";
import { BaseVariableContext } from "../../Variables/BaseVariableContext";
import { setupStore } from "client/state/store";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { createMockApplicationConfiguration, SubSetMock } from "../../../../helpers/MockSession";
import { mock } from "jest-mock-extended";
import { PageHandler } from "../../../PageHandler";
import { Provider } from "react-redux";
import { MockRouter } from "client/helpers/MockRouter";
import { MockStoreBuilder } from "client/helpers/MockStore";
import * as SurveyVueUtils from "client/components/helpers/SurveyVueUtils";

// Mocks
jest.mock("../../Variables/VariableModal/Utils/VariableComponentHelpers", () => ({
    ...jest.requireActual("../../Variables/VariableModal/Utils/VariableComponentHelpers"),
    getGroupCountAndSample: jest.fn(() => Promise.resolve([{ count: 1, sample: 1 }])),
}));
jest.mock("../../Variables/VariableModal/Utils/VariableDefinitionCreationService");
jest.mock("../../Variables/VariableModal/Utils/VariableCreationService");

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

const defaultProps = {
    applicationConfiguration: createMockApplicationConfiguration(new Date(2020,0,1), new Date(2020,11,31), true),
    allSubsets: [ SubSetMock ],
    onCreatedMetricName: jest.fn(),
};

function renderComponent(props = {}) {
    const testStore = setupStore(new MockStoreBuilder().build());
    const mockBaseVariableDispatch = jest.fn();

    return render(
        <Provider store={testStore}>
            <MockRouter>
                <VariableContext.Provider value={mockedVariableContext}>
                    <BaseVariableContext.Provider value={{ baseVariables: [], baseVariableDispatch: mockBaseVariableDispatch, baseVariablesLoading: false }}>
                        <CreateWaveVariableButton {...defaultProps} {...props} />
                    </BaseVariableContext.Provider>
                </VariableContext.Provider>
            </MockRouter>
        </Provider>
    );
}

describe("CreateWaveVariableButton", () => {
    beforeEach(() => {
        jest.clearAllMocks();
        // spy on the shared handleError so we can assert it was called
        jest.spyOn(SurveyVueUtils, 'handleError').mockImplementation(() => {});
    });

    it("renders the button", () => {
        renderComponent();
        expect(screen.getByRole("button")).toBeInTheDocument();
        expect(screen.getByRole("button")).not.toBeDisabled();
    });

    it("calls onHandleError if data is not loaded", async () => {
        const user = userEvent.setup();
        renderComponent({
            applicationConfiguration: { ...defaultProps.applicationConfiguration, hasLoadedData: false },
        });
        const button = screen.getByRole("button");
        await user.click(button);
        expect(SurveyVueUtils.handleError).toHaveBeenCalled();
    });

    it("calls onHandleError if date range is invalid", async () => {
        const user = userEvent.setup();
        renderComponent({
            applicationConfiguration: {
                ...defaultProps.applicationConfiguration,
                dateOfFirstDataPoint: new Date(2021, 0, 1),
                dateOfLastDataPoint: new Date(2020, 0, 1),
            },
        });
        const button = screen.getByRole("button");
        await user.click(button);
        expect(SurveyVueUtils.handleError).toHaveBeenCalled();
    });

    it("calls onCreatedMetricName after successful creation", async () => {
        const user = userEvent.setup();
        const createVariableMock = jest.fn(() =>
            Promise.resolve({ urlSafeMetricName: "Wave" })
        );
        const VariableCreationService = require("../../Variables/VariableModal/Utils/VariableCreationService");
        VariableCreationService.VariableCreationService.mockImplementation(() => ({
            createVariable: createVariableMock,
        }));

        const VariableDefinitionCreationService = require("../../Variables/VariableModal/Utils/VariableDefinitionCreationService");
        VariableDefinitionCreationService.VariableDefinitionCreationService.mockImplementation(() => ({
            createWaveDefinition: () => ({
                groups: [{}],
            }),
        }));

        renderComponent();
        const button = screen.getByRole("button");
        await user.click(button);
        expect(defaultProps.onCreatedMetricName).toHaveBeenCalledWith("Wave");
    });

    const dateRangeTests = [
        ["2025-05-06", "2025-06-25", 2],
        ["2025-01-06", "2025-12-25", 12],
        ["2024-06-06", "2025-05-25", 12],
        ["2024-02-29", "2024-05-01", 4],
    ];

    test.each(dateRangeTests)("for a survey starting %s and ending %s it creates %s months in the wave, all unique and with ids greater than zero", async (startDate, endDate, expectedCount) => {
        const user = userEvent.setup();
        const createVariableMock: jest.Mock<Promise<{ urlSafeMetricName: string; }>, [string, any, string]> = jest.fn((wave: string, groups: any, type: string) =>
            Promise.resolve({ urlSafeMetricName: "Wave" })
        );
        const VariableCreationService = require("../../Variables/VariableModal/Utils/VariableCreationService");
        VariableCreationService.VariableCreationService.mockImplementation(() => ({
            createVariable: createVariableMock,
        }));

        const VariableDefinitionCreationService = require("../../Variables/VariableModal/Utils/VariableDefinitionCreationService");
        VariableDefinitionCreationService.VariableDefinitionCreationService.mockImplementation(() => ({
            createWaveDefinition: () => ({
                groups: [{}],
            }),
        }));

        renderComponent({
            applicationConfiguration: {
                ...defaultProps.applicationConfiguration,
                dateOfFirstDataPoint: new Date(startDate),
                dateOfLastDataPoint: new Date(endDate),
            },
        });
        const button = screen.getByRole("button");
        await user.click(button);
        expect(createVariableMock).toHaveBeenCalledTimes(1);
        
        const firstCall = createVariableMock.mock.calls[0];
        const [arg1, arg2, arg3] = firstCall;
        expect(arg2.groups.length).toEqual(expectedCount);

        const names = arg2.groups.map(group => group.toEntityInstanceName);
        const uniqueNames = new Set(names);
        expect(uniqueNames.size).toEqual(expectedCount);

        const idsGreaterThanZero = arg2.groups.map(group => group.toEntityInstanceId).filter(id => id > 0);
        expect(idsGreaterThanZero.length).toEqual(expectedCount);
    });
});