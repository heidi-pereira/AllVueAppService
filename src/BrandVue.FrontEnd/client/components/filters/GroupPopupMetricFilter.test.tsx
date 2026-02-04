import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { GroupPopupMetricFilter } from './GroupPopupMetricFilter';
import { PageHandler } from '../PageHandler';
import { GoogleTagManager } from '../../googleTagManager';
import { GroupFilterConfiguration } from '../../filter/GroupFilterConfiguration';
import { EntityInstance } from '../../entity/EntityInstance';
import { Metric } from '../../metrics/metric';
import { FilterValueMapping } from '../../metrics/metricSet';

jest.mock('../PageHandler', () => ({
    PageHandler: jest.fn(() => ({})),
}));

jest.mock('../../googleTagManager', () => ({
    GoogleTagManager: jest.fn(() => ({ addEvent: jest.fn() })),
}));

describe('GroupPopupMetricFilter', () => {
    let mockProps;

    beforeEach(() => {
        const groupFilterState = new GroupFilterConfiguration();
        mockProps = {
            pageHandler: new PageHandler({} as any),
            googleTagManager: new (GoogleTagManager as any)() as GoogleTagManager,
            groupFilterName: 'TestGroup',
            groupFilterStates: [groupFilterState],
            allBrands: [new EntityInstance(1, 'Brand1'), new EntityInstance(2, 'Brand2')],
            updateGroupedMetricFilters: jest.fn(),
        };

        const metric = new Metric({});
        metric.name = 'TestGroup: TestMetric';
        metric.entityCombination = [];
        metric.filterValueMapping = [new FilterValueMapping("TestMetric", "1:Yes", ["1"])];
        groupFilterState.metric = metric;
        groupFilterState.state = { entityInstances: { testIdentifier: [1] }, values: [1], invert: false, treatPrimaryValuesAsRange: false };
        groupFilterState.name = "TestGroup";
    });

    it('should render without crashing', () => {
        const { getByText } = render(<GroupPopupMetricFilter {...mockProps} />);
        expect(getByText('TestGroup: TestMetric')).toBeInTheDocument();
    });

    it('should clear filter on clear button click', () => {
        const { getByTitle } = render(<GroupPopupMetricFilter {...mockProps} />);
        const clearButton = getByTitle("Clear 'TestGroup: TestMetric' filter");

        fireEvent.click(clearButton);
        expect(mockProps.updateGroupedMetricFilters).toHaveBeenCalledWith(
            'TestGroup',
            mockProps.groupFilterStates.map((state) => state.name),
            { values: [], entityInstances: {}, invert: false, treatPrimaryValuesAsRange: false }
        );
    });
});