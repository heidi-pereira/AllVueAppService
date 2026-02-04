import React from "react";
import { render, screen } from '@testing-library/react';
import userEvent from "@testing-library/user-event";
import "@testing-library/jest-dom";
import { FilterPopupMetricFilter } from "./FilterPopupMetricFilter";
import { PageHandler } from "../PageHandler";
import { IGoogleTagManager } from "../../googleTagManager";
import { MetricFilterState } from "../../filter/metricFilterState";
import { EntityType } from "../../BrandVueApi";
import { mock } from "jest-mock-extended";

jest.mock("../../googleTagManager");

const mockPageHandler = {} as PageHandler;

const FilterPopupTestWrapper = ({ initialMetricFilterState, updateMetricFilters }: { initialMetricFilterState: MetricFilterState; updateMetricFilters: (newState: MetricFilterState) => void; }) => {
    const [metricFilterState, setMetricFilterState] = React.useState(initialMetricFilterState);
    return (

        <form aria-label={"test form"}>
            <FilterPopupMetricFilter
                pageHandler={mockPageHandler}
                googleTagManager={mock<IGoogleTagManager>()}
                metricFilterState={metricFilterState}
                updateMetricFilters={newState => {
                    updateMetricFilters(newState as MetricFilterState);
                    setMetricFilterState(newState as MetricFilterState);
                }}
            />
        </form>
    );

};


const renderComponent = (metricFilterState: MetricFilterState) => {
    const updateMetricFilters = jest.fn();
    return {
        user: userEvent.setup(),
        updateMetricFilters,
        ...
        render(
            <FilterPopupTestWrapper initialMetricFilterState={metricFilterState} updateMetricFilters={updateMetricFilters} />
        )
    }
};

describe("FilterPopupMetricFilter", () => {

    beforeEach(() => {
        jest.clearAllMocks();
    });
    const entityType = new EntityType({
        identifier: "entityType",
        displayNameSingular: "Entity",
        displayNamePlural: "Entities",
        isProfile: false,
        isBrand: false,
    });

    const createMetricFilterState = (filterValueMapping: { text: string; values: string[] }[], numericValues: number[]) => {
        const metricFilterState = new MetricFilterState();
        metricFilterState.metric = {
            name: "metric - dropdown",
            entityCombination: [entityType],
            filterValueMapping: filterValueMapping,
        } as any;
        metricFilterState.isRange = false;
        metricFilterState.entityInstances = { entityType: numericValues };
        metricFilterState.values = numericValues;
        return metricFilterState;
    }

    it("should set values in radio case for legacyEntityMapping", () => {
        const metricFilterState = createMetricFilterState([{ text: "Option1", values: ["4"] }, { text: "Option2", values: ["2"] }], []);
        const { updateMetricFilters } = renderComponent(metricFilterState);

        const option1Radio = screen.getByLabelText(metricFilterState.metric.filterValueMapping[0].text);
        option1Radio.click();

        expect(updateMetricFilters).toHaveBeenCalledWith(expect.objectContaining({
            values: [4],
            entityInstances: { entityType: [4] }
        }));
    });

    it("should select the radio button for non-legacyEntityMapping", async () => {
        const metricFilterState = createMetricFilterState([{ text: "Option1", values: ["4"] }, { text: "Option2", values: ["2"] }], [4]);

        renderComponent(metricFilterState);

        const option1Radio = screen.getByLabelText(metricFilterState.metric.filterValueMapping[0].text);
        expect(option1Radio).toBeChecked();
    });


    it("should set the values in dropdown case for legacyEntityMapping", async () => {

        const metricFilterState = createMetricFilterState([{ text: "Option3", values: ["3"] }, { text: "Option4", values: ["4"] }, { text: "Option5", values: ["5", "6", "7"] }], []);

        const { user, updateMetricFilters } = renderComponent(metricFilterState);

        const optionText = metricFilterState.metric.filterValueMapping[0].text;

        const dropdown = screen.getByRole("combobox", { name: /Option drop down/i });
        await user.click(dropdown);

        const optionToSelect = screen.getByText(optionText);
        await user.click(optionToSelect);

        expect(updateMetricFilters).toHaveBeenCalledWith(expect.objectContaining({
            values: [3],
            entityInstances: { entityType: [3] }
        }));
    });
});
