import { render, fireEvent, screen } from "@testing-library/react";
import '@testing-library/jest-dom';
import { FilterInstancePicker } from "./FilterInstancePicker";
import { IEntityType } from "client/BrandVueApi";
import { EntityInstance } from "../../../../../entity/EntityInstance";

const mockEntityType: IEntityType = { identifier: "brand", name: "Brand" } as any;
const mockInstances: EntityInstance[] = [
    { id: 1, name: "Instance 1" } as any,
    { id: 2, name: "Instance 2" } as any,
    { id: 3, name: "Instance 3" } as any,
];

const mockConfig = {
    filterByEntityTypes: [
        { type: "brand", instance: 2 }
    ]
} as any;

describe("FilterInstancePicker", () => {
    it("shows the selected instance label", () => {
        render(
            <FilterInstancePicker
                entityType={mockEntityType}
                selectedInstances={[mockInstances[1]]}
                allInstances={mockInstances}
                config={mockConfig}
                updatePartWithConfig={jest.fn()}
            />
        );
        expect(screen.getByTestId("dropdown-label")).toHaveTextContent("Instance 2");
    });

    it("shows all options in the dropdown", () => {
        render(
            <FilterInstancePicker
                entityType={mockEntityType}
                selectedInstances={[mockInstances[1]]}
                allInstances={mockInstances}
                config={mockConfig}
                updatePartWithConfig={jest.fn()}
            />
        );
        fireEvent.click(screen.getByTestId("dropdown-toggle"));
        mockInstances.forEach(instance => {
            expect(screen.getByTestId(`dropdown-item-${instance.id}`)).toBeInTheDocument();
        });
    });

    it("calls selectFilterInstances with the correct instance when an option is clicked", () => {
        const onSelect = jest.fn();
        render(
            <FilterInstancePicker
                entityType={mockEntityType}
                selectedInstances={[mockInstances[1]]}
                allInstances={mockInstances}
                config={mockConfig}
                updatePartWithConfig={onSelect}
            />
        );
        fireEvent.click(screen.getByTestId("dropdown-toggle"));
        fireEvent.click(screen.getByTestId("dropdown-item-3"));
        expect(onSelect).toHaveBeenCalledTimes(1);
        expect(onSelect).toHaveBeenCalledWith(
            expect.objectContaining({
                filterByEntityTypes: expect.arrayContaining([
                    expect.objectContaining({ type: "brand", instance: 3 })
                ])
            })
        );
    });
});