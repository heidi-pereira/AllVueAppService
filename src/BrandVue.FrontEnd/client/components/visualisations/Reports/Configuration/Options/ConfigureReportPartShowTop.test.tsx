import React from "react";
import { fireEvent, render, screen } from '@testing-library/react';
import { PartDescriptor, ReportOrder } from "../../../../../BrandVueApi";
import { PartType } from '../../../../panes/PartType';
import ConfigureReportPartShowTop from "./ConfigureReportPartShowTop";

const entityInstanceCount = 10;
const defaultSortOrder = ReportOrder.ScriptOrderDesc;

describe("When a user changes top n", () => {
    const savePartChanges = jest.fn();

    const shouldUpdateSortingOrderTestCases = [
        [PartType.ReportsCardChart],
        [PartType.ReportsCardDoughnut],
        [PartType.ReportsCardMultiEntityMultipleChoice],
        [PartType.ReportsTable],
    ];

    const getPartDescriptor = (partType: PartType, showTop: number | undefined) => {
        const testingPartDescriptor = new PartDescriptor();
        testingPartDescriptor.partType = partType as string;
        testingPartDescriptor.reportOrder = defaultSortOrder;
        testingPartDescriptor.showTop = showTop;

        return testingPartDescriptor
    }

    const getComponent = (partType: PartType, showTop: number | undefined) => {
        return (
            <ConfigureReportPartShowTop
                part={getPartDescriptor(partType, showTop)}
                entityInstanceCount={entityInstanceCount}
                savePartChanges={savePartChanges}
            />
        )
    }

    test.each(shouldUpdateSortingOrderTestCases)("should set sorting order to result order when selected", async (partType) => {
        render(getComponent(partType, undefined));

        const checkbox = await screen.findByTestId('enable-top-x-checkbox');
        fireEvent.click(checkbox);

        expect(savePartChanges).toHaveBeenCalledWith(
            expect.objectContaining({ reportOrder: ReportOrder.ResultOrderDesc })
        );
    });

    test.each(shouldUpdateSortingOrderTestCases)("should return sorting order to default when deselected", async (partType) => {
        render(getComponent(partType, 7));

        const checkbox = await screen.findByTestId('enable-top-x-checkbox');
        fireEvent.click(checkbox);

        expect(savePartChanges).toHaveBeenCalledWith(
            expect.objectContaining({ reportOrder: defaultSortOrder })
        );
    });

    const shouldNotUpdateSortingOrderTestCases = [
        PartType.ReportsCardLine,
        PartType.ReportsCardStackedMulti,
        PartType.ReportsCardText,
        PartType.ReportsCardHeatmapImage,
    ];

    test.each(shouldNotUpdateSortingOrderTestCases)("top n should not be present for part type", async (partType) => {
        render(getComponent(partType, 7));
        const checkbox = screen.queryByTestId('enable-top-x-checkbox');
        expect(checkbox).toBeNull();
    });

    test('Should increase range input value', async () => {
        render(getComponent(PartType.ReportsCardChart, 3));
                
        const rangeInput = await screen.findByTestId('topn-range-input');
        fireEvent.change(rangeInput, { target: { value: '4' } });
        expect(savePartChanges).toHaveBeenCalledWith(expect.objectContaining({
            showTop: 4,
        }));
    });
});
