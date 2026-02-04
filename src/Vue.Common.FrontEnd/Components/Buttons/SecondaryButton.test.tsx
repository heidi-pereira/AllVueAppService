import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import SecondaryButton from './SecondaryButton';
import '@testing-library/jest-dom';

describe('SecondaryButton', () => {
  it('renders with the given name', () => {
    render(<SecondaryButton name="Test Button" onClick={() => {}} />);
    expect(screen.getByRole('button', { name: /test button/i })).toBeInTheDocument();
  });

  it('calls onClick when clicked', () => {
    const handleClick = jest.fn();
    render(<SecondaryButton name="Click Me" onClick={handleClick} />);
    fireEvent.click(screen.getByRole('button', { name: /click me/i }));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('is disabled when disabled prop is true', () => {
    render(<SecondaryButton name="Disabled" onClick={() => {}} disabled />);
    const button = screen.getByRole('button', { name: /disabled/i });
    expect(button).toBeDisabled();
  });

  it('does not call onClick when disabled', () => {
    const handleClick = jest.fn();
    render(<SecondaryButton name="Disabled" onClick={handleClick} disabled />);
    fireEvent.click(screen.getByRole('button', { name: /disabled/i }));
    expect(handleClick).not.toHaveBeenCalled();
  });
});
