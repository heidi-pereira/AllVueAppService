import React from "react";
import { render, fireEvent, screen } from "@testing-library/react";
import '@testing-library/jest-dom';
import FilterMultiInstancePicker from "./FilterMultiInstancePicker";
import { IEntityType } from "client/BrandVueApi";
import { EntityInstance } from "../../../../../entity/EntityInstance";

jest.mock("client/components/mixpanel/MixPanel", () => ({
    MixPanel: { track: jest.fn() }
}));

const mockEntityType: IEntityType = { identifier: "brand", name: "Brand" } as any;
const mockInstances: EntityInstance[] = [
    { id: 1, name: "Instance 1" } as any,
    { id: 2, name: "Instance 2" } as any,
    { id: 3, name: "Instance 3" } as any,
];

const getConfig = (selectedIds: number[]) => ({
    filterByEntityTypes: selectedIds.map(id => ({ type: "brand", instance: id }))
}) as any;

describe("FilterMultiInstancePicker", () => {
    it("adds a new entity to the selected list when checked", () => {
        const onSelect = jest.fn();
        render(
            <FilterMultiInstancePicker
                entityType={mockEntityType}
                selectedInstances={[mockInstances[0]]}
                allInstances={mockInstances}
                config={getConfig([1])}
                updatePartWithConfig={onSelect}
            />
        );
        fireEvent.click(screen.getByText("Instance 2"));
        expect(onSelect).toHaveBeenCalledWith(
            expect.objectContaining({
                filterByEntityTypes: expect.arrayContaining([
                    expect.objectContaining({ type: "brand", instance: 2 })
                ])
            })
        );
    });

    it("removes an entity from the selected list when unchecked", () => {
        const onSelect = jest.fn();
        render(
            <FilterMultiInstancePicker
                entityType={mockEntityType}
                selectedInstances={[mockInstances[0], mockInstances[1]]}
                allInstances={mockInstances}
                config={getConfig([1, 2])}
                updatePartWithConfig={onSelect}
            />
        );
        fireEvent.click(screen.getByText("Instance 2"));
        expect(onSelect).toHaveBeenCalledWith(
            expect.objectContaining({
                filterByEntityTypes: expect.not.arrayContaining([
                    expect.objectContaining({ type: "brand", instance: 2 })
                ])
            })
        );
    });

    it("selects only the first entity when Clear is clicked", () => {
        const onSelect = jest.fn();
        render(
            <FilterMultiInstancePicker
                entityType={mockEntityType}
                selectedInstances={[mockInstances[1], mockInstances[2]]}
                allInstances={mockInstances}
                config={getConfig([2, 3])}
                updatePartWithConfig={onSelect}
            />
        );
        fireEvent.click(screen.getByText("Clear"));
        expect(onSelect).toHaveBeenCalledWith(
            expect.objectContaining({
                filterByEntityTypes: expect.arrayContaining([
                    expect.objectContaining({ type: "brand", instance: 1 })
                ])
            })
        );
    });

    it("selects all entities when Select All is clicked", () => {
        const onSelect = jest.fn();
        render(
            <FilterMultiInstancePicker
                entityType={mockEntityType}
                selectedInstances={[]}
                allInstances={mockInstances}
                config={getConfig([])}
                updatePartWithConfig={onSelect}
            />
        );
        fireEvent.click(screen.getByText("Select All"));
        expect(onSelect).toHaveBeenCalledWith(
            expect.objectContaining({
                filterByEntityTypes: expect.arrayContaining([
                    expect.objectContaining({ type: "brand", instance: 1 }),
                    expect.objectContaining({ type: "brand", instance: 2 }),
                    expect.objectContaining({ type: "brand", instance: 3 }),
                ])
            })
        );
    });
});
