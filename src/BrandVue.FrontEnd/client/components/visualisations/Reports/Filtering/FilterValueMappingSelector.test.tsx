import { render, fireEvent, screen } from '@testing-library/react';
import FilterValueMappingSelector from './FilterValueMappingSelector';
import { MainQuestionType } from '../../../../BrandVueApi';
import { MetricFilterState } from '../../../../filter/metricFilterState';
import { getMetricWithEntityCombinations } from '../../../../helpers/ReactTestingLibraryHelpers';
import '@testing-library/jest-dom';

// Mock contexts
jest.mock('../../../../entity/EntityConfigurationStateContext', () => ({
    useEntityConfigurationStateContext: () => ({
        entityConfiguration: {
            getAllEnabledInstancesForTypeOrdered: () => [
                { id: '1', name: 'Entity 1' },
                { id: '2', name: 'Entity 2' }
            ]
        }
    })
}));
jest.mock('../../../../metrics/MetricStateContext', () => ({
    useMetricStateContext: () => ({
        questionTypeLookup: { 'metric1': MainQuestionType.MultipleChoice }
    })
}));
jest.mock('./FilterHelper', () => ({
    getMetricFilter: (metric: any, filter: any, entityInstances: any) => ({
        invert: false,
        treatPrimaryValuesAsRange: false,
        values: filter.values,
        entityInstances: entityInstances || {}
    })
}));
jest.mock('../../../helpers/ArrayHelper', () => ({
    ArrayHelper: {
        isEqual: (a: any, b: any) => JSON.stringify(a) === JSON.stringify(b)
    }
}));

let mockMetric = getMetricWithEntityCombinations(2);
const YesText = 'Yes';
mockMetric.filterValueMapping = [
    { text: YesText, fullText: YesText, values: ['1'] },
    { text: 'No', fullText: 'No', values: ['0'] },
    { text: 'Maybe', fullText: 'Maybe', values: ['2'] }
];

const mockMappingFilterState = {
    metric: mockMetric,
    name: 'Mock Metric',
    entityInstances: {},
    values: [],
    invert: false,
    treatPrimaryValuesAsRange: false,
    isAdvanced: false,
    isRange: false,
    isEnabled: jest.fn(),
    valueToString: jest.fn(),
    filterDescription: jest.fn(),
    description: jest.fn(),
    withCleared: jest.fn(),
    withInstance: jest.fn(),
    withRange: jest.fn(),
    withConstantValues: jest.fn(),
    withValues: jest.fn(),
    getRangeFilterDescription: jest.fn(),
    getMultiFilterDescription: jest.fn(),
    getSimpleFilterDescription: jest.fn(),
    getInstanceDescription: jest.fn(),
    withInstances: jest.fn(),
} as any;

describe('FilterValueMappingSelector', () => {
    it('renders filter value mapping checkboxes', () => {
        render(
            <FilterValueMappingSelector
                id="test"
                selectedMetric={mockMetric}
                selectedFilters={[]}
                setSelectedFilters={jest.fn()}
            />
        );
        expect(screen.getByText(YesText)).toBeInTheDocument();
        expect(screen.getByText('No')).toBeInTheDocument();
        expect(screen.getByText('Maybe')).toBeInTheDocument();
    });

    it('calls setSelectedFilters when a filter is toggled', () => {
        const setSelectedFilters = jest.fn();
        render(
            <FilterValueMappingSelector
                id="test"
                selectedMetric={mockMetric}
                selectedFilters={[]}
                setSelectedFilters={setSelectedFilters}
            />
        );
        fireEvent.click(screen.getByLabelText(YesText));
        expect(setSelectedFilters).toHaveBeenCalled();
    });

    it('calls setSelectedFilters with empty array when select none is clicked', () => {
        const setSelectedFilters = jest.fn();
        render(
            <FilterValueMappingSelector
                id="test"
                selectedMetric={mockMetric}
                selectedFilters={[]}
                setSelectedFilters={setSelectedFilters}
                selectNoneText="None"
            />
        );
        fireEvent.click(screen.getByText('None'));
        expect(setSelectedFilters).toHaveBeenCalledWith([]);
    });

    it('calls onApply when apply button is clicked', () => {
        const onApply = jest.fn();
        render(
            <FilterValueMappingSelector
                id="test"
                selectedMetric={mockMetric}
                selectedFilters={[mockMappingFilterState as MetricFilterState]}
                setSelectedFilters={jest.fn()}
                showApplyButtons={true}
                onApply={onApply}
            />
        );
        fireEvent.click(screen.getByText('Apply'));
        expect(onApply).toHaveBeenCalled();
    });

    it('calls close when cancel button is clicked', () => {
        const close = jest.fn();
        render(
            <FilterValueMappingSelector
                id="test"
                selectedMetric={mockMetric}
                selectedFilters={[mockMappingFilterState as MetricFilterState]}
                setSelectedFilters={jest.fn()}
                showApplyButtons={true}
                close={close}
            />
        );
        fireEvent.click(screen.getByText('Cancel'));
        expect(close).toHaveBeenCalled();
    });
});
