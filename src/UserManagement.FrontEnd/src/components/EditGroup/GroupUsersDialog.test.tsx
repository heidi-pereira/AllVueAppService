import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import GroupUsersDialog, { User } from './GroupUsersDialog';

interface ITestProps {
    users: Array<User>,
    onCancel?: () => void;
    onChange?: () => void;
    onClose?: () => void;
}

const renderComponent = (props: ITestProps) => {
    return render(<GroupUsersDialog {...props} />);
};

const mockUsers: Array<User> = [
    { name: 'User 1', email: 'user1@example.com', isSelected: false },
    { name: 'User 2', email: 'user2@example.com', isSelected: true },
];

const mockOnCancel = jest.fn();
const mockOnChange = jest.fn();
const mockOnClose = jest.fn();

const defaultProps = {
    users: mockUsers,
    onCancel: mockOnCancel,
    onChange: mockOnChange,
    onClose: mockOnClose
};

describe('GroupUsersDialog', () => {
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
      const checkbox = screen.getByRole('checkbox', { name: /Users/i });
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
      const checkbox = screen.getByRole('checkbox', { name: /Users/i });
      await user.click(checkbox);
  
      const addButton = screen.getByRole('button', { name: /Update/i });
      await user.click(addButton);
      expect(mockOnChange).toHaveBeenCalledWith(expect.any(Array));
      expect(mockOnClose).toHaveBeenCalled();
    });
});