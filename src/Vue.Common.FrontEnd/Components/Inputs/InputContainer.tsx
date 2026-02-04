import React from 'react';
import styled from 'styled-components';
import MuiInputBase from '@mui/material/InputBase';

interface InputContainerProps {
  label: string;
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  children?: React.ReactNode;
  errorMessage?: string;
  disabled?: boolean;
}

const Container = styled.div`
  display: flex;
  flex-direction: column;
  width: 100%;
  position: relative;
  margin-bottom: 8px;
`;

const LabelRow = styled.div`
  position: relative;
  width: 100%;
  margin-bottom: 4px;
  display: flex;
  align-items: center;
  justify-content: space-between;
`;

const Label = styled.label`
  color: #212529;
  font-size: 1rem;
  line-height: 1.5;
`;

const Input = styled(MuiInputBase)<{ $invalid?: boolean; disabled?: boolean }>`
  background-color: ${({ disabled }) => (disabled ? '#f8f9fa' : '#fff')} !important;
  border: 1px solid
    ${({ $invalid, disabled }) => 
      disabled ? '#dee2e6' : ($invalid ? '#dc3545' : '#cbcfd3')} !important;
  border-radius: 4px !important;
  outline: none !important;
  padding: 7px !important;
  width: 100%;
  font-family: inherit !important;
  font-size: inherit !important;
  line-height: inherit !important;
  margin: 0 !important;
  cursor: ${({ disabled }) => (disabled ? 'not-allowed' : 'text')};

  & input {
    padding: 0 !important;
    font-family: inherit !important;
    font-size: inherit !important;
    line-height: inherit !important;
    color: ${({ disabled }) => (disabled ? '#6c757d' : 'inherit')} !important;
    cursor: ${({ disabled }) => (disabled ? 'not-allowed' : 'text')};
  }

  & input::placeholder {
    color: #6e7881 !important;
    opacity: 1 !important;
  }

  &:hover {
    border-color: ${({ $invalid, disabled }) => 
      disabled ? '#dee2e6' : ($invalid ? '#dc3545' : '#cbd5e1')} !important;
  }

  &.Mui-focused {
    border-color: ${({ $invalid, disabled }) => 
      disabled ? '#dee2e6' : ($invalid ? '#dc3545' : '#4682b4')} !important;
    box-shadow: ${({ disabled }) => disabled ? 'none' : '0px 0px 2px 2px'} 
      ${({ $invalid, disabled }) =>
        disabled ? '' : ($invalid ? 'rgba(220,53,69,0.15)' : 'rgba(182,216,247,0.56)')} !important;
  }
`;

const ErrorText = styled.div`
  color: #dc3545;
  font-size: 0.95rem;
  margin-top: 4px;
  margin-bottom: 2px;
`;

const InputContainer: React.FC<InputContainerProps> = ({
  label,
  value,
  onChange,
  placeholder,
  children,
  errorMessage,
  disabled = false,
}) => (
  <Container>
    <LabelRow>
      <Label>{label}</Label>
      {children}
    </LabelRow>
    <Input
      placeholder={placeholder}
      value={value}
      onChange={onChange}
      $invalid={!!errorMessage}
      aria-invalid={!!errorMessage}
      disabled={disabled}
    />
    {errorMessage && <ErrorText>{errorMessage}</ErrorText>}
  </Container>
);

export default InputContainer;
