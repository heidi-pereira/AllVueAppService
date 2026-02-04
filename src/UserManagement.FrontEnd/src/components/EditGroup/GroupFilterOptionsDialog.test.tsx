import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import GroupFilterOptionsDialog from './GroupFilterOptionsDialog';


// Mock RTK Query hooks
jest.mock('@/rtk/api/enhancedApi', () => ({
    userManagementApi: {
        usePostApiProjectsByCompanyIdAndProjectTypeProjectIdFilterMutation: jest.fn(() => [
            jest.fn().mockImplementation(() => Promise.resolve({ unwrap: () => Promise.resolve(100) })),
            { isLoading: false }
        ]),
    }
}), { virtual: true });

import { userManagementApi as api } from "@/rtk/api/enhancedApi";

interface ITestProps {
  filters: Array<{
    name: string;
    description: string;
    options: Array<{
      name: string;
      isSelected: boolean;
      percentage: number;
    }>;
  }>;
  filter: {
    name: string;
    description: string;
    options: Array<{
      name: string;
      isSelected: boolean;
      percentage: number;
    }>;
  },
  onCancel?: () => void;
  onChange?: () => void;
  filterCountOfRespondents: number;
  totalCountOfRespondents: number;
}

const renderComponent = (props: ITestProps) => {
  return render(<GroupFilterOptionsDialog {...props} />);
};

const mockFilter = {
  name: 'Filter 1',
  description: 'Description of Filter 1',
  options: [
    { name: 'Option 1', isSelected: false, percent: 20 },
    { name: 'Option 2', isSelected: true, percent: 80 }
  ]
};

const mockOnCancel = jest.fn();
const mockOnChange = jest.fn();
const mockOnClose = jest.fn();

const defaultProps = {
  filters: [mockFilter],
  filter: mockFilter,
  onCancel: mockOnCancel,
  onChange: mockOnChange,
  onClose: mockOnClose,
  filterCountOfRespondents: 20,
  totalCountOfRespondents: 200
};

describe('GroupFilterOptionsDialog', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders a component', () => {
    const { container } = renderComponent(defaultProps);
    expect(container).toBeInTheDocument();
  });

  it('should have a disabled add button on load', () => {
    renderComponent(defaultProps);
    const addButton = screen.getByRole('button', { name: /Add/i });
    expect(addButton).toBeDisabled();
  });

  it('should enable the add button when options are selected', async () => {
    renderComponent(defaultProps);
    const user = userEvent.setup();
    const optionCheckbox = screen.getByRole('checkbox', { name: /Options/i });
    await user.click(optionCheckbox);
    const addButton = screen.getByRole('button', { name: /Add/i });
    expect(addButton).toBeEnabled();
  });

  it('calls onCancel when the back link is clicked', async () => {
    renderComponent(defaultProps);
    const user = userEvent.setup();
    const cancelButton = screen.getByRole('button', { name: /Back/i });
    await user.click(cancelButton);
    expect(mockOnCancel).toHaveBeenCalled();
  });

  it('calls onChange when options are selected and add button is clicked', async () => {
    renderComponent(defaultProps);
    const user = userEvent.setup();
    const optionCheckbox = screen.getByRole('checkbox', { name: /Options/i });
    await user.click(optionCheckbox);
    const addButton = screen.getByRole('button', { name: /Add/i });
    await user.click(addButton);
    expect(mockOnChange).toHaveBeenCalledWith([
      { name: 'Option 1', isSelected: true, percent: 20 },
      { name: 'Option 2', isSelected: true, percent: 80 }
    ]);
  });
});