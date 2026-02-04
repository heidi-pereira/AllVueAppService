import React from 'react';
import '@testing-library/jest-dom';
import { render, fireEvent } from '@testing-library/react';

// Mock the helper so the hint text is stable
jest.mock('../../BrandVueOnlyLowSampleHelper', () => ({
    __esModule: true,
    default: { lowSampleForEntity: 3 }
}));

import LowSampleSelector from './LowSampleSelector';

describe('LowSampleSelector', () => {
    test('renders checkbox and numeric input when editing allowed and highlights enabled', () => {
        const handleHighlight = jest.fn();
        const handleThreshold = jest.fn();

        const { getByRole, getByLabelText } = render(
            <LowSampleSelector
                highlightLowSample={true}
                handleHighlightLowSampleChanged={handleHighlight}
                lowSampleThreshold={10}
                handleLowSampleThresholdChange={handleThreshold}
                allowLowSampleThresholdEditing={true}
            />
        );

        const checkbox = getByRole('checkbox', { name: /highlight low sample/i });
        expect(checkbox).toBeInTheDocument();
        expect((checkbox as HTMLInputElement).checked).toBe(true);

        // numeric input is labelled (srOnly) as Low sample threshold
        const numberInput = getByLabelText(/low sample threshold/i) as HTMLInputElement;
        expect(numberInput).toBeInTheDocument();
        expect(numberInput.value).toBe('10');
        expect(numberInput).not.toBeDisabled();

        // toggle checkbox
        fireEvent.click(checkbox);
        expect(handleHighlight).toHaveBeenCalled();

        // change numeric value
        fireEvent.change(numberInput, { target: { value: '15' } });
        expect(handleThreshold).toHaveBeenCalled();
    });

    test('numeric input is disabled when highlightLowSample is false', () => {
        const handleHighlight = jest.fn();
        const handleThreshold = jest.fn();

        const { getByRole, getByLabelText } = render(
            <LowSampleSelector
                highlightLowSample={false}
                handleHighlightLowSampleChanged={handleHighlight}
                lowSampleThreshold={5}
                handleLowSampleThresholdChange={handleThreshold}
                allowLowSampleThresholdEditing={true}
            />
        );

        const checkbox = getByRole('checkbox', { name: /highlight low sample/i });
        expect((checkbox as HTMLInputElement).checked).toBe(false);

        const numberInput = getByLabelText(/low sample threshold/i) as HTMLInputElement;
        expect(numberInput).toBeDisabled();

        // clicking checkbox should call handler
        fireEvent.click(checkbox);
        expect(handleHighlight).toHaveBeenCalled();
    });

    test('renders hint text when editing not allowed', () => {
        const handleHighlight = jest.fn();
        const handleThreshold = jest.fn();

        const { queryByLabelText, getByText } = render(
            <LowSampleSelector
                highlightLowSample={true}
                handleHighlightLowSampleChanged={handleHighlight}
                lowSampleThreshold={7}
                handleLowSampleThresholdChange={handleThreshold}
                allowLowSampleThresholdEditing={false}
            />
        );

        // numeric input should not be rendered
        expect(queryByLabelText(/low sample threshold/i)).toBeNull();

        // hint text should include the mocked low sample value
        expect(getByText(/A low sample warning is shown if any sample sizes are 3 or lower/i)).toBeInTheDocument();
    });
});
