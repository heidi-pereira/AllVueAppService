import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import GroupQuestionsDialog from "./GroupQuestionsDialog";

interface ITestProps {
    questions: Array<{
        title: string;
        description: string;
        percent: number;
        isSelected: boolean;
    }>,
    onCancel?: () => void;
    onChange?: () => void;
    onClose?: () => void;
}

const renderComponent = (props: ITestProps) => {
    return render(<GroupQuestionsDialog {...props} />);
};

const mockQuestions = [
    { title: 'Question 1', description: 'Description 1', percent: 50, isSelected: false },
    { title: 'Question 2', description: 'Description 2', percent: 75, isSelected: true },
];

const mockOnCancel = jest.fn();
const mockOnChange = jest.fn();
const mockOnClose = jest.fn();

const defaultProps = {
    questions: mockQuestions,
    onCancel: mockOnCancel,
    onChange: mockOnChange,
    onClose: mockOnClose
};

describe('GroupQuestionsDialog', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders a component', () => {
    const {container} = renderComponent(defaultProps);
    expect(container).toBeInTheDocument();
  });

  it('has a disabled add button when opened', () => {
    renderComponent(defaultProps);
    const addButton = screen.getByRole('button', { name: /Update/i });
    expect(addButton).toBeDisabled();
  });

  it('has a cancel button when opened', () => {
    renderComponent(defaultProps);
    const cancelButton = screen.getByRole('button', { name: /Cancel/i });
    expect(cancelButton).toBeInTheDocument();
  });

  it('has an enabled add button when changes have been made', async () => {
    const user = userEvent.setup();
    renderComponent(defaultProps);
    const checkbox = screen.getByRole('checkbox', { name: /Questions/i });
    await user.click(checkbox);

    const addButton = screen.getByRole('button', { name: /Update/i });
    expect(addButton).toBeEnabled();
  });

  it('calls onCancel when cancel button is clicked', async () => {
    const user = userEvent.setup();
    renderComponent(defaultProps);
    const cancelButton = screen.getByRole('button', { name: /Cancel/i });
    await user.click(cancelButton);
    expect(mockOnCancel).toHaveBeenCalled();
    expect(mockOnClose).toHaveBeenCalled();
  });

  it('calls onChange with question states when enabled add button is clicked', async () => {
    const user = userEvent.setup();
    renderComponent(defaultProps);
    const checkbox = screen.getByRole('checkbox', { name: /Questions/i });
    await user.click(checkbox);

    const addButton = screen.getByRole('button', { name: /Update/i });
    await user.click(addButton);
    expect(mockOnChange).toHaveBeenCalledWith(expect.any(Array));
    expect(mockOnClose).toHaveBeenCalled();
  });
});