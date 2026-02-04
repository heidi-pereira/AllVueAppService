import { render, screen } from "@testing-library/react";
import '@testing-library/jest-dom';
import { PartType } from "client/components/panes/PartType";
import { DataSortOrder, PartDescriptor } from "../../../../../BrandVueApi";
import ConfigureReportPartFilterInstance from "./ConfigureReportPartFilterInstance";

jest.mock("./FilterInstancePicker", () => ({
    FilterInstancePicker: (props: any) => (
        <div data-testid="filter-instance-picker" data-entity-type={props.entityType.identifier}>
            {props.entityType.identifier}
        </div>
    ),
}));
jest.mock("./FilterMultiInstancePicker", () => ({
    FilterMultiInstancePicker: (props: any) => (
        <div data-testid="filter-multi-instance-picker" data-entity-type={props.entityType.identifier}>
            {props.entityType.identifier}
        </div>
    ),
}));
jest.mock("../../../../../entity/EntityConfigurationStateContext", () => ({
    useEntityConfigurationStateContext: () => ({
        entityConfiguration: {
            getAllEnabledInstancesForTypeOrdered: () => [
                { id: 1, name: "Instance 1" },
                { id: 2, name: "Instance 2" }
            ]
        }
    }),
}));
jest.mock("../../../../helpers/SurveyVueUtils", () => ({
    getSplitByAndFilterByEntityTypesForPart: () => ({
        filterByEntityTypes: [
            { identifier: "brand" },
            { identifier: "region" }
        ]
    }),
}));

function createTestPartDescriptor(partType: string) {
    return new PartDescriptor({
        id: 1,
        fakeId: "fake",
        paneId: "pane",
        partType: partType,
        spec1: "",
        spec2: "",
        spec3: "",
        defaultSplitBy: "",
        helpText: "",
        defaultAverageId: "",
        autoMetrics: [],
        autoPanes: [],
        ordering: [],
        orderingDirection: DataSortOrder.Ascending,
        colours: [],
        filters: [],
        xAxisRange: { min: 0, max: 1 },
        yAxisRange: { min: 0, max: 1 },
        sections: [],
        breaks: [],
        overrideReportBreaks: false,
        multipleEntitySplitByAndFilterBy: {
            splitByEntityType: "",
            filterByEntityTypes: [
                { type: "brand", instance: 1 }
            ]
        },
        averageTypes: [],
        displayMeanValues: false,
        displayStandardDeviation: false,
        subset: [],
        environment: [""],
        roles: [],
        disabled: false,
    });
}

describe("ConfigureReportPartFilterInstance", () => {
    const renderComponent = (partType: string) => {
        const part = createTestPartDescriptor(partType);
        const reportPart: any = { part };
        return render(
            <ConfigureReportPartFilterInstance
                reportPart={reportPart}
                canPickFilterInstances={true}
                savePartChanges={jest.fn()}
            />
        );
    };

    it("renders FilterMultiInstancePicker for partType=ReportsCardStackedMulti", () => {
        renderComponent(PartType.ReportsCardStackedMulti);
        expect(screen.getAllByTestId("filter-multi-instance-picker")).toHaveLength(2);
        expect(screen.queryByTestId("filter-instance-picker")).not.toBeInTheDocument();
    });

    it("renders FilterInstancePicker for any other partType", () => {
        renderComponent(PartType.ReportsCardLine);
        expect(screen.getAllByTestId("filter-instance-picker")).toHaveLength(2);
        expect(screen.queryByTestId("filter-multi-instance-picker")).not.toBeInTheDocument();
    });
});