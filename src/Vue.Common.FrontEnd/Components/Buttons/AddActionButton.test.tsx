import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import ActionButton from './AddActionButton';

describe('AddActionButton', () => {
  it('renders with default icon and label', () => {
    render(<ActionButton onClick={() => {}} label="Add Item" />);
    expect(screen.getByText('Add Item')).toBeInTheDocument();
    expect(screen.getByText('add')).toBeInTheDocument();
  });

  it('renders with custom icon', () => {
    render(<ActionButton onClick={() => {}} label="Create" icon="create" />);
    expect(screen.getByText('create')).toBeInTheDocument();
  });

  it('calls onClick when clicked', () => {
    const handleClick = jest.fn();
    render(<ActionButton onClick={handleClick} label="Add" />);
    fireEvent.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('applies custom className', () => {
    render(<ActionButton onClick={() => {}} label="Add" className="my-class" />);
    expect(screen.getByRole('button')).toHaveClass('my-class');
  });
});
