import React from 'react';
import { render, fireEvent, screen } from '@testing-library/react';
import CrossMeasureInstanceSelector from './CrossMeasureInstanceSelector';
import type { ICrossMeasureInstanceSelectorProps } from './CrossMeasureInstanceSelector';
import { generateEntityType, getMetricWithEntityCombinations } from 'client/helpers/ReactTestingLibraryHelpers';
import '@testing-library/jest-dom';

const instanceLabelOne = 'Instance 1';
const instanceLabelTwo = 'Instance 2';

jest.mock('../../../../entity/EntityConfigurationStateContext', () => ({
    useEntityConfigurationStateContext: () => ({
        entityConfiguration: {
            getAllEnabledInstancesForTypeOrdered: () => [
                { id: 1, name: instanceLabelOne },
                { id: 2, name: instanceLabelTwo }
            ]
        }
    })
}));
jest.mock('../../../../ProductConfigurationContext', () => ({
    ProductConfigurationContext: React.createContext({ productConfiguration: { isSurveyVue: true } })
}));
jest.mock('../../../../metrics/MetricStateContext', () => ({
    useMetricStateContext: () => ({
        questionTypeLookup: { metric1: 1 }
    })
}));
jest.mock('../../../helpers/SurveyVueUtils', () => ({
    canUseFilterValueMappingAsBreak: () => false,
    getAvailableCrossMeasureFilterInstances: () => [],
    shouldUseFilterValueMappingAsBreak: () => false
}));
jest.mock('../../../mixpanel/MixPanel', () => ({
    MixPanel: { track: jest.fn() }
}));

const mockSetCrossMeasures = jest.fn();

const mockCrossMeasure = {
    measureName: 'TestMeasure',
    filterInstances: [],
    multipleChoiceByValue: false,
    childMeasures: [],
    init: jest.fn(),
    toJSON: jest.fn()
};

const baseProps : ICrossMeasureInstanceSelectorProps ={
    selectedCrossMeasure: mockCrossMeasure,
    selectedMetric: getMetricWithEntityCombinations(2),
    activeEntityType: generateEntityType(1),
    setCrossMeasures: mockSetCrossMeasures,
    disabled: false,
    includeSelectAll: true
};

describe('CrossMeasureInstanceSelector', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('renders checkboxes for entity instances', () => {
        render(<CrossMeasureInstanceSelector {...baseProps} />);
        expect(screen.getByText(instanceLabelOne)).toBeInTheDocument();
        expect(screen.getByText(instanceLabelTwo)).toBeInTheDocument();
    });

    it('renders entity instances in correct id order', () => {
        render(<CrossMeasureInstanceSelector {...baseProps} />);
        const instanceLabels = screen.getAllByRole('checkbox').slice(1); // skip "Select All"
        // Get the text content of the labels associated with checkboxes
        const labelTexts = instanceLabels.map(cb => cb.parentElement?.textContent?.trim());
        expect(labelTexts).toEqual([instanceLabelOne, instanceLabelTwo]);
    });

    it('calls setCrossMeasures when an instance is toggled', () => {
        render(<CrossMeasureInstanceSelector {...baseProps} />);
        // Find the checkbox by its title attribute (which matches instance.displayName)
        const label = screen.getByTitle(instanceLabelOne);
        fireEvent.click(label);
        expect(mockSetCrossMeasures).toHaveBeenCalled();
    });

    it('selects all when Select All is clicked', () => {
        render(<CrossMeasureInstanceSelector {...baseProps} />);
        // Find the "Select All" checkbox by role and click it (first checkbox)
        const checkboxes = screen.getAllByRole('checkbox');
        fireEvent.click(checkboxes[0]);
        expect(mockSetCrossMeasures).toHaveBeenCalled();
    });

    it('disables checkboxes when disabled prop is true', () => {
        render(<CrossMeasureInstanceSelector {...baseProps} disabled={true} />);
        // Find the checkbox input by role and check its disabled state
        const checkboxes = screen.getAllByRole('checkbox');
        // The first checkbox is "Select All", the rest are instances
        expect(checkboxes[1]).toBeDisabled(); // Instance 1
        expect(checkboxes[0]).toBeDisabled(); // Select All
    });
});
