import { IMetricDropdownMenuProps, MetricDropdownMenu } from "./MetricDropdownMenu";
import { render, fireEvent, waitFor, act } from "@testing-library/react";
import { Provider } from "react-redux";
import { getMetrics } from "../../../helpers/ReactTestingLibraryHelpers";
import { Metric } from "../../../metrics/metric";
import { setupStore } from "client/state/store";
import { MockStoreBuilder } from "client/helpers/MockStore";

const getDefaultMetricDropdownMenuProps = (metrics: Metric[]) => {
    const defaultProps: IMetricDropdownMenuProps = {
        toggleElement: <></>,
        metrics: metrics,
        selectMetric: (metric: any) => { console.log("selectMetric not implemented") },
        showCreateVariableButton: false,
        disabled: false,
        groupCustomVariables: true,
        shouldCreateWaveVariable: false,
        reportVariableAppendType: undefined,
        selectedReportPart: undefined,
        selectNoneText: undefined
    }

    return defaultProps;
}

const renderComponent = (props: IMetricDropdownMenuProps) =>
     render(<Provider store={setupStore(new MockStoreBuilder().build())}><MetricDropdownMenu {...props} /></Provider>);

describe(MetricDropdownMenu, () => {
    const metrics = getMetrics(3);
    const metricDropdownMenuProps = getDefaultMetricDropdownMenuProps(metrics);

    it("should display a search bar", () => {
        const { container } = renderComponent(metricDropdownMenuProps);

        const searchInput = container.getElementsByClassName("search-input");
        expect(searchInput).toBeDefined;
    })

    it("should initially list all eligible metrics", () => {
        const { container } = renderComponent(metricDropdownMenuProps);

        const listedMetrics = container.getElementsByClassName("name-container");
        expect(listedMetrics.length).toBe(3);
    })

    it("should only display matching options when search bar is used", async () => {
        const { container } = renderComponent(metricDropdownMenuProps);

        const searchInput = container.getElementsByClassName("search-input");
        expect(searchInput).toBeDefined;

        const metricToSearchFor = metrics[1];

        act(() => {
            fireEvent.change(searchInput[0], { target: { value: metricToSearchFor.name } });
        });

        await waitFor(() => {
            const matchedMetrics = container.getElementsByClassName("name-container");
            expect(matchedMetrics.length).toBe(1);

            const matchedMetric = matchedMetrics.item(0);
            expect(matchedMetric).toBeDefined();
            expect(matchedMetric?.children.length).toBe(2);
            expect(matchedMetric?.children.item(0)?.textContent).toBe(metricToSearchFor.displayName);
        })
    })
})
