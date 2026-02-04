import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import InputContainer from './InputContainer';

describe('InputContainer', () => {
  it('renders the label', () => {
    render(
      <InputContainer
        label="Test Label"
        value=""
        onChange={() => {}}
      />
    );
    expect(screen.getByText('Test Label')).toBeInTheDocument();
  });

  it('renders children in the label row', () => {
    render(
      <InputContainer
        label="Label"
        value=""
        onChange={() => {}}
      >
        <span data-testid="child-span">Child Content</span>
      </InputContainer>
    );
    expect(screen.getByTestId('child-span')).toHaveTextContent('Child Content');
  });

  it('renders the input with the correct value', () => {
    render(
      <InputContainer
        label="Label"
        value="Hello"
        onChange={() => {}}
      />
    );
    expect(screen.getByDisplayValue('Hello')).toBeInTheDocument();
  });

  it('calls onChange when input value changes', () => {
    const handleChange = jest.fn();
    render(
      <InputContainer
        label="Label"
        value=""
        onChange={handleChange}
      />
    );
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'abc' } });
    expect(handleChange).toHaveBeenCalled();
  });

  it('renders the placeholder if provided', () => {
    render(
      <InputContainer
        label="Label"
        value=""
        onChange={() => {}}
        placeholder="Type here"
      />
    );
    expect(screen.getByPlaceholderText('Type here')).toBeInTheDocument();
  });
});
