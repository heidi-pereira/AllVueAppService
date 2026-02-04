import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import '@testing-library/jest-dom';
import { QuestionEditModal, QuestionEditModalProps } from "./QuestionEditModal";
import toast from "react-hot-toast";
import { generateMetric } from "../../../helpers/ReactTestingLibraryHelpers";
import { Provider } from "react-redux"; // Adjust the import path if using a different state management library
import { setupStore } from "../../../state/store"; // <-- import setupStore
import { MockRouter } from '../../../helpers/MockRouter';
import { MixPanelModel } from "../../mixpanel/MixPanelHelper";
import { MixPanelClientTest } from "../../mixpanel/MixPanelClientTest";
import { MixPanel } from "../../mixpanel//MixPanel";
import * as BrandVueApi from "../../../BrandVueApi";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import * as actionTypes from '../../../metrics/metricsActionTypeConstants';

jest.mock('react-hot-toast')
const mockedToast = toast as jest.Mocked<typeof toast>
const errorMessage = "Error message";
jest.mock("../../../BrandVueApi", () => {
    const originalModule = jest.requireActual("../../../BrandVueApi");
    return {
        ...originalModule,
        SwaggerException: class extends Error  {
            constructor(message, status, response, headers, result) {
                super(message);
                const error = { message, status, response, headers, result, name: "SwaggerException" };
                return error
            }
            static isSwaggerException(error) {
                return true;
            }
        }
    };
});

jest.mock('../../../metrics/MetricStateContext', () => ({
    useMetricStateContext: jest.fn(),
}));

const createMockMetric = () => {
    var metric = generateMetric("Test metric");
    metric.primaryFieldDependencies = [];
    metric.entityCombination = [];
    metric.helpText = "Help text";
    return metric;
}

const mockProps: QuestionEditModalProps = {
    isOpen: true,
    setIsOpen: jest.fn(),
    metric: createMockMetric(),
    variableDefinition: new BrandVueApi.QuestionVariableDefinition(),
    subsetId: "subset-1",
    canEditDisplayName: true
};

const saveButton = "Save";
const questionName = "Question name";
const questionText = "Question text";
const updatedHelpText = "New help text";
const updateName = "New name";
const cancelButton = "Cancel";

const renderComponent = (props: QuestionEditModalProps) => {
    const store = setupStore({ subset: { subsetId: 'all', subsetConfigurations: [] } });
    return render(
        <Provider store={store}>
            <MockRouter>
                <QuestionEditModal {...props} />
            </MockRouter>
        </Provider>
    );
}

describe("QuestionEditModal", () => {
    beforeEach(() => {
        (useMetricStateContext as jest.Mock).mockReturnValue({
            metricsDispatch: jest.fn(),
        });
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
    });

    test("renders correctly with given props", () => {
        renderComponent(mockProps);
        expect(screen.getByLabelText(questionName)).toHaveValue(mockProps.metric.displayName);
        expect(screen.getByLabelText(questionText)).toHaveValue(mockProps.metric.helpText);
    });

    test("updates state when props change", () => {
        renderComponent(mockProps);
        fireEvent.change(screen.getByLabelText(questionName), { target: { value: updateName } });
        fireEvent.change(screen.getByLabelText(questionText), { target: { value: updatedHelpText } });
        expect(screen.getByLabelText(questionName)).toHaveValue(updateName);
        expect(screen.getByLabelText(questionText)).toHaveValue(updatedHelpText);
    });

    test("calls setIsOpen(false) on closeHandler", () => {
        renderComponent(mockProps);
        fireEvent.click(screen.getByText(cancelButton));
        expect(mockProps.setIsOpen).toHaveBeenCalledWith(false);
    });

    test("handles saveHandler correctly", async () => {
        const { metricsDispatch } = useMetricStateContext();

        renderComponent(mockProps);
        fireEvent.change(screen.getByLabelText(questionName), { target: { value: updateName } });
        fireEvent.change(screen.getByLabelText(questionText), { target: { value: updatedHelpText } });
        fireEvent.click(screen.getByText(saveButton));

        await waitFor(() => {
            expect(metricsDispatch).toHaveBeenCalledWith({
                type: actionTypes.UPDATE_MODAL_DATA,
                data: { metric: createMockMetric(), newHelpText: updatedHelpText, newDisplayName: updateName, newEntityMeanMap: undefined },
            });
            expect(mockProps.setIsOpen).toHaveBeenCalledWith(false);
        });
    });

    test("handles errors correctly in handleError", async () => {
        const response = JSON.stringify({ message: errorMessage })
        mockedToast.error = jest.fn();
        console.log = jest.fn();
        const mockPropsThrowException: QuestionEditModalProps = {
            isOpen: true,
            setIsOpen: jest.fn(),
            metric: createMockMetric(),
            variableDefinition: new BrandVueApi.QuestionVariableDefinition(),
            subsetId: "subset-1",
            canEditDisplayName: true,
        };
        (useMetricStateContext as jest.Mock).mockReturnValue({
            metricsDispatch: jest.fn().mockImplementation(() => {
                throw new BrandVueApi.SwaggerException(errorMessage, 500, response, [], null);
            })
        });
        
        renderComponent(mockPropsThrowException);
        fireEvent.change(screen.getByLabelText(questionName), { target: { value: updateName } });
        fireEvent.change(screen.getByLabelText(questionText), { target: { value: updatedHelpText } });
        fireEvent.click(screen.getByText(saveButton));
        await waitFor(() => {
            expect(mockedToast.error).toHaveBeenCalledTimes(1);
        });
    });
});