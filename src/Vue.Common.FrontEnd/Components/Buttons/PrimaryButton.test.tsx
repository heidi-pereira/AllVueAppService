import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import PrimaryButton from './PrimaryButton';
import '@testing-library/jest-dom';

describe('PrimaryButton', () => {
  it('renders with the given name', () => {
    render(<PrimaryButton name="Save" />);
    expect(screen.getByRole('button', { name: /save/i })).toBeInTheDocument();
  });

  it('calls onClick when clicked', () => {
    const handleClick = jest.fn();
    render(<PrimaryButton name="Click me" onClick={handleClick} />);
    fireEvent.click(screen.getByRole('button', { name: /click me/i }));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('is disabled when disabled prop is true', () => {
    render(<PrimaryButton name="Disabled" disabled />);
    expect(screen.getByRole('button', { name: /disabled/i })).toBeDisabled();
  });

  it('does not call onClick when disabled', () => {
    const handleClick = jest.fn();
    render(<PrimaryButton name="No click" onClick={handleClick} disabled />);
    fireEvent.click(screen.getByRole('button', { name: /no click/i }));
    expect(handleClick).not.toHaveBeenCalled();
  });
});
