import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import GroupFiltersDialog from './GroupFiltersDialog';

const GroupFilterOptionsDialogTestId = 'mock-group-filter-options-dialog';
const GroupFilterSelectedOptionsDialogTestId = 'mock-group-filter-selected-options-dialog';

jest.mock("./GroupFilterOptionsDialog", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
      return <div data-testid={GroupFilterOptionsDialogTestId}></div>;
    })
  };
});

jest.mock("./GroupFilterSelectedOptionsDialog", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
      return <div data-testid={GroupFilterSelectedOptionsDialogTestId}></div>;
    })
  };
});

interface ITestProps {
  filters: Array<{
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
  filterCountOfRespondents: number;
  totalCountOfRespondents: number;
}

const renderComponent = (props: ITestProps) => {
  return render(<GroupFiltersDialog {...props} />);
};

const mockFilters = [
  {
    name: 'Filter 1',
    description: 'Description of Filter 1',
    options: [
      { name: 'Option 1', isSelected: false, percent: 20 },
      { name: 'Option 2', isSelected: true, percent: 80 }
    ]
  },
  {
    name: 'Filter 2',
    description: 'Description of Filter 2',
    options: [
      { name: 'Option 1', isSelected: false, percent: 20 },
      { name: 'Option 2', isSelected: true, percent: 80 },
      { name: 'Option 3', isSelected: true, percent: 70 }
    ]
  }
];

const mockOnCancel = jest.fn();
const mockOnChange = jest.fn();
const mockOnClose = jest.fn();

const defaultProps = {
  filters: mockFilters,
  onCancel: mockOnCancel,
  onChange: mockOnChange,
  onClose: mockOnClose,
  filterCountOfRespondents:2328,
  totalCountOfRespondents: 5000
};

describe('GroupFiltersDialog', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders a component', () => {
    const { container } = renderComponent(defaultProps);
    expect(container).toBeInTheDocument();
  });

  it('shows the selected filter options dialog when the view button is clicked', async () => {
    renderComponent(defaultProps);
    const user = userEvent.setup();
    const viewButton = screen.getByRole('button', { name: /View Selection/i });
    await user.click(viewButton);
    const optionsDialog = screen.getByTestId(GroupFilterSelectedOptionsDialogTestId);
    expect(optionsDialog).toBeInTheDocument();
  });

  it('shows the filter options dialog when a filter is clicked', async () => {
    renderComponent(defaultProps);
    const user = userEvent.setup();
    const filterButton = screen.getByRole('button', { name: /Filter 1/i });
    await user.click(filterButton);
    const optionsDialog = screen.getByTestId(GroupFilterOptionsDialogTestId);
    expect(optionsDialog).toBeInTheDocument();
  });

  it('should show the correct number of selected options in the view button', () => {
    const selectedOptionsCount = mockFilters.flatMap(filter => filter.options.filter(option => option.isSelected)).length;
    renderComponent(defaultProps);
    const viewButton = screen.getByRole('button', { name: /View Selection/i });
    expect(viewButton).toHaveTextContent(`view ${selectedOptionsCount} selected`);
  });
});