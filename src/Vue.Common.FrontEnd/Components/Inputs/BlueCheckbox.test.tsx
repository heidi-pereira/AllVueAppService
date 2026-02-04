import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import BlueCheckbox, { BlueCheckboxProps } from './BlueCheckbox';
import '@testing-library/jest-dom';

describe('BlueCheckbox', () => {
  const defaultProps: BlueCheckboxProps = {
    checked: false,
    onChange: jest.fn(),
    label: 'Test Label',
  };

  it('renders the label', () => {
    render(<BlueCheckbox {...defaultProps} />);
    expect(screen.getByText('Test Label')).toBeInTheDocument();
  });

  it('renders as checked when checked=true', () => {
    render(<BlueCheckbox {...defaultProps} checked={true} />);
    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).toBeChecked();
  });

  it('renders as unchecked when checked=false', () => {
    render(<BlueCheckbox {...defaultProps} checked={false} />);
    const checkbox = screen.getByRole('checkbox');
    expect(checkbox).not.toBeChecked();
  });

  it('calls onChange when clicked', () => {
    const onChange = jest.fn();
    render(<BlueCheckbox {...defaultProps} onChange={onChange} />);
    const checkbox = screen.getByRole('checkbox');
    fireEvent.click(checkbox);
    expect(onChange).toHaveBeenCalled();
  });
});
