import MetricListItem, { IMetricListItemProps } from "./MetricListItem";
import React from 'react';
import { render} from "@testing-library/react";
import { generateMetric } from "../../../helpers/ReactTestingLibraryHelpers";
import { Metric } from "../../../metrics/metric";
import { IEntityType } from "../../../BrandVueApi";
import { IGoogleTagManager } from "../../../googleTagManager";
import { PageHandler } from "../../PageHandler";
import { createMockProductConfiguration } from "../../../helpers/MockSession";
import { mock } from "jest-mock-extended";
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";
import { VariableType } from "./VariableType";

const mockProductConfig = createMockProductConfiguration();
mockProductConfig.user.isSystemAdministrator = false;

const getDefaultMetricDropdownMenuProps = (metric: Metric) => {
    const defaultProps: IMetricListItemProps = {
        googleTagManager: mock<IGoogleTagManager>(),
        pageHandler: mock<PageHandler>(),
        variableListItem: {metric, variable: undefined, variableType: VariableType.Custom},
        splitByEntityType: undefined,
        isSelected: false,
        canEditMetrics: false,
        showHamburger: true,
        groupCustomVariables: true,
        subsetId: "All",
        selectMetric: () => { console.log("selectMetric not implemented") },
        setEligibleForCrosstabOrAllVue: (isEligible: boolean) => new Promise(() => { console.log("setEligibleForCrosstabOrAllVue not implemented") }),
        setMetricEnabled: (isEligible: boolean) => new Promise(() => { console.log("setMetricEnabled not implemented") }),
        setFilterForMetricEnabled: (isEligible: boolean) => new Promise(() => { console.log("setFilterForMetricEnabled not implemented") }),
        setMetricDefaultSplitBy: (entityType: IEntityType) => new Promise(() => { console.log("setMetricDefaultSplitBy not implemented") }),
        setConvertCalculationTypeModalVisible: () => new Promise(() => { console.log("setConvertCalculationTypeModalVisible not implemented") }),
    }

    return defaultProps;
}

const renderMetricListItemWithContext = (props: IMetricListItemProps) => {
    return render(
        <ProductConfigurationContext.Provider value={{ productConfiguration: mockProductConfig }}>
            <MetricListItem {...props} />
        </ProductConfigurationContext.Provider >
    );
}

describe(MetricListItem, () => {
    it("should display a box for the metric", () => {
        const metric = generateMetric("Test metric");
        const metricDropdownMenuProps = getDefaultMetricDropdownMenuProps(metric);

        const { container } = renderMetricListItemWithContext(metricDropdownMenuProps);

        const listedMetric = container.getElementsByClassName("name-container");
        expect(listedMetric.length).toBe(1);
    })

    it("should display display name as title when no help text", () => {
        const metric = generateMetric("Test metric");
        const metricDropdownMenuProps = getDefaultMetricDropdownMenuProps(metric);

        const { container } = renderMetricListItemWithContext(metricDropdownMenuProps);

        const listedTitles = container.getElementsByClassName("title");
        expect(listedTitles.length).toBe(1);

        const listedTitle = listedTitles.item(0);
        expect(listedTitle).toBeDefined();

        expect(listedTitle?.textContent).toBe(metric.displayName);
    })

    it("should display help text as title when available", () => {
        const metric = generateMetric("Test metric");
        metric.helpText = "Test help text";
        const metricDropdownMenuProps = getDefaultMetricDropdownMenuProps(metric);

        const { container } = renderMetricListItemWithContext(metricDropdownMenuProps);

        const listedTitles = container.getElementsByClassName("title");
        expect(listedTitles.length).toBe(1);

        const listedTitle = listedTitles.item(0);
        expect(listedTitle).toBeDefined();

        expect(listedTitle?.textContent).toBe(metric.helpText);
    })
})
