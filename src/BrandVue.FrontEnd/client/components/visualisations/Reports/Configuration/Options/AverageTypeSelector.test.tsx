import "@testing-library/jest-dom";
import { render, screen } from "@testing-library/react";
import { Provider } from "react-redux";
import AverageTypeSelector from "./AverageTypeSelector";
import { AverageType, MainQuestionType } from "../../../../../BrandVueApi";
import { ProductConfigurationContext } from '../../../../../ProductConfigurationContext';
import { Metric } from "../../../../../metrics/metric";
import { GetUnderlyingMetric } from "../../../../../components/visualisations/Variables/VariableModal/Utils/VariableComponentHelpers";
import { useMetricStateContext } from "../../../../../metrics/MetricStateContext";
import { store } from "client/state/store";
import { MetricSet } from "../../../../../metrics/metricSet";

const MockProductConfigurationProvider = ({ children, productConfiguration }) => {
    return (
        <ProductConfigurationContext.Provider value={ productConfiguration }>
            {children}
        </ProductConfigurationContext.Provider>
    );
}

let props = {
    selectedAverages: [],
    toggleAverage: jest.fn(),
    disabledMessage: undefined,
    metric: new Metric(undefined),
    displayMeanValues: false,
    toggleDisplayMeanValues: jest.fn(),
    metrics: [],
    toggleStandardDeviation: jest.fn(),
}

jest.mock("../../../AverageHelper", () => ({
    ...jest.requireActual("../../../AverageHelper"),
    getVerifiedAverageType: jest.fn(() => AverageType.EntityIdMean),
}));

jest.mock("../../../../../components/visualisations/Variables/VariableModal/Utils/VariableComponentHelpers", () => ({
    ...jest.requireActual("../../../../../components/visualisations/Variables/VariableModal/Utils/VariableComponentHelpers"),
    GetUnderlyingMetric: jest.fn(() => new Metric({Name: "test"})),
}));

jest.mock('../../../../../metrics/MetricStateContext', () => ({
    useMetricStateContext: jest.fn(),
}));

const setUpSelector = (selectedAverages, displayMeanValues, isMultiChoiceMetric, displayStandardDeviation) => {
    const isFeatureEnabledMock = jest.fn().mockReturnValue(true);
    const isSurveyVueMock = jest.fn().mockReturnValue(true);

    const questionTypeLookup = new Proxy({}, {
        get: () => isMultiChoiceMetric ? MainQuestionType.MultipleChoice : MainQuestionType.SingleChoice,
    });

    const config = {
        productConfiguration: {
            isFeatureEnabled: isFeatureEnabledMock,
            isSurveyVue: isSurveyVueMock
        }
    };

    (useMetricStateContext as jest.Mock).mockReturnValue({
        questionTypeLookup,
        selectableMetricsForUser: [],
        enabledMetricSet: new MetricSet({metrics: []})
    });

    const { container } = render(
        <Provider store={store}>
            <MockProductConfigurationProvider productConfiguration={config}>
                <AverageTypeSelector
                    {...props}
                    selectedAverages={selectedAverages}
                    displayMeanValues={displayMeanValues}
                    displayStandardDeviation={displayStandardDeviation}
                    supportsStandardDeviation={true}
                />
            </MockProductConfigurationProvider>
        </Provider>
    );

    expect(container).toBeInTheDocument();
    return container;
}

describe("AverageTypeSelector", () => {
    it("renders with four checkboxes", () => {
        setUpSelector([], false, true, false);
        const checkboxCount = screen.getAllByRole('checkbox').length;
        expect(checkboxCount).toBe(5);
    });

    it("renders with three checkboxes", () => {
        setUpSelector([], false, false, false);
        const checkboxCount = screen.getAllByRole('checkbox').length;
        expect(checkboxCount).toBe(4);
    });

    it("sets only the mean checkbox as checked when mean average selected", () => {
        (GetUnderlyingMetric as jest.Mock).mockReturnValueOnce(new Metric({Name:"test"}));
        setUpSelector([AverageType.Mean], false, true, false);

        const meanCheckbox = screen.getByRole('checkbox', { name: "Mean" });
        const medianCheckbox = screen.getByRole('checkbox', { name: "Median" });
        const mentionsCheckbox = screen.getByRole('checkbox', { name: "Mentions" });
        expect(meanCheckbox).toBeChecked();
        expect(medianCheckbox).not.toBeChecked();
        expect(mentionsCheckbox).not.toBeChecked();
    });

    it("always renders the 'Show scale values' checkbox for multiple-choice metrics", () => {
        setUpSelector([], false, true, false);
        const showScaleCheckbox = screen.getByRole('checkbox', { name: "Show scale values" });
        expect(showScaleCheckbox).toBeInTheDocument();
    });

    it("always renders the 'Show scale values' checkbox for single-choice metrics", () => {
        setUpSelector([], false, false, false);
        const showScaleCheckbox = screen.getByRole('checkbox', { name: "Show scale values" });
        expect(showScaleCheckbox).toBeInTheDocument();
    });
});