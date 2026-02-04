import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import GroupFilterSelectedOptionsDialog from './GroupFilterSelectedOptionsDialog';

interface ITestProps {
    filters: Array<{
        index: number;
        searchText: string;
        name: string;
        description: string;
        options: Array<{
            name: string;
            isSelected: boolean;
            percent: number;
        }>;
    }>;
    onCancel?: () => void;
    onChange?: () => void;
    totalCountOfRespondents: number;
    filterCountOfRespondents: number;

}

const renderComponent = (props: ITestProps) => {
    return render(<GroupFilterSelectedOptionsDialog {...props} />);
};

const mockFilters = [
    {
        index: 0,
        searchText: 'filter 1 description of filter 1',
        name: 'Filter 1',
        description: 'Description of Filter 1',
        options: [
            { name: 'Option 1', isSelected: false, percent: 20 },
            { name: 'Option 2', isSelected: true, percent: 80 }
        ]
    }
];

const mockOnRemove = jest.fn();
const mockOnClose = jest.fn();

const defaultProps = {
    filters: mockFilters,
    onRemove: mockOnRemove,
    onClose: mockOnClose,
    totalCountOfRespondents: 200,
    filterCountOfRespondents: 100
};

describe('GroupFilterSelectedOptionsDialog', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('renders a component', () => {
        const { container } = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should show only the selected options', async () => {
        renderComponent(defaultProps);
        expect(screen.getByText('Filter 1')).toBeInTheDocument();
        expect(screen.getByText('Option 2')).toBeInTheDocument();
        expect(screen.queryByText('Option 1')).not.toBeInTheDocument();
    });

    it('calls onCancel when the back link is clicked', async () => {
        renderComponent(defaultProps);
        const user = userEvent.setup();
        const backButton = screen.getByRole('button', { name: /Back/i });
        await user.click(backButton);
        expect(mockOnClose).toHaveBeenCalled();
    });

    it('calls onRemove when an option is removed', async () => {
        renderComponent(defaultProps);
        const user = userEvent.setup();
        const removeButton = screen.getByRole('button', { name: /Remove Option 2/i });
        await user.click(removeButton);
        expect(mockOnRemove).toHaveBeenCalledWith(0, 1); // Assuming the first filter and second option
    });
});