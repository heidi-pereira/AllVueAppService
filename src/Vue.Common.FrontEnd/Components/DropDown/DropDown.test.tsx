import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import DropDown from './DropDown';

interface ITestProps {
    id: string;
    label?: string;
    emptyValueLabel?: string;
    selectValue: string;
    handleChange: (value: string) => void;
    items: Array<{ value: string; label: string }>;
    loading: boolean;
}

const mockHandleChange = jest.fn();
const defaultProps: ITestProps = {
    id: 'test-dropdown',
    label: 'Test Dropdown',
    emptyValueLabel: 'Select an option',
    selectValue: '',
    handleChange: mockHandleChange,
    items: [
        { value: 'item1', label: 'Item 1' },
        { value: 'item2', label: 'Item 2' },
        { value: 'item3', label: 'Item 3' }
    ],
    loading: false
};

const renderComponent = (props: ITestProps) => {
    return render(<DropDown {...props} />);
};

describe('DropDown Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('should render a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('should render an All option when no value is selected', async () => {
        const user = new userEvent.setup();
        const emptyValueLabel = 'All Values';
        const props = { ...defaultProps, emptyValueLabel };

        const {container} = renderComponent(props);
        const dropdown = screen.getByRole('combobox');
        await user.click(dropdown);
        const allOption = screen.getByText(emptyValueLabel);
        expect(allOption).toBeInTheDocument();
    });

    it('should render a label if provided', () => {
        const {container} = renderComponent(defaultProps);
        const labels = container.getElementsByTagName('label');
        expect(labels.length).toBeGreaterThan(0);
        expect(labels[0].textContent).toBe(defaultProps.label);
    });

    it('should render the options from the items prop', async () => {
        const user = new userEvent.setup();
        const {container} = renderComponent(defaultProps);
        const dropdown = screen.getByRole('combobox');
        await user.click(dropdown);
        const options = screen.getAllByRole('option');
        expect(options.length).toBe(defaultProps.items.length + 1); // +1 for the empty value option
        defaultProps.items.forEach(item => {
            expect(screen.getByText(item.label)).toBeInTheDocument();
        });
    });

    it('should call handleChange with the correct value when an option is selected', async () => {
        const user = new userEvent.setup();
        const {container} = renderComponent(defaultProps);
        const dropdown = screen.getByRole('combobox');
        await user.click(dropdown);
        const option = screen.getByText(defaultProps.items[0].label);
        await user.click(option);
        expect(mockHandleChange).toHaveBeenLastCalledWith(defaultProps.items[0].value);
    });
});