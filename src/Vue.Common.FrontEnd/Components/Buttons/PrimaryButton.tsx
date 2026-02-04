import React from 'react';
import styled from 'styled-components';

interface PrimaryButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  name: string;
  disabled?: boolean;
  onClick?: React.MouseEventHandler<HTMLButtonElement>;
}

const StyledPrimaryButton = styled.button<{ disabled?: boolean }>`
  display: flex;
  flex-direction: row;
  align-items: center;
  font-size: 14px;
  background-color: #1376cd;
  color: #fff;
  padding: 10px 20px;
  line-height: 16px;
  border: 1px solid #1376cd;
  border-radius: 4px;
  outline: 0;
  flex-shrink: 0;
  transition: all 175ms ease;
  justify-content: center;
  width: 120px;

  .material-symbols-outlined {
    font-size: 16px;
  }

  &:hover {
    background-color: #0e4b81;
    border-color: #0e4b81;
  }

  &:active,
  &:focus,
  &[aria-expanded="true"] {
    box-shadow: 0px 0px 3px 3px rgba(19, 118, 205, 0.32);
    outline: 0;
  }

  &:disabled {
    opacity: 0.3;
    cursor: not-allowed;
    background: #1376cd;
    border-color: #1376cd;

    &:active,
    &:focus {
      box-shadow: none;
    }
  }
`;

const PrimaryButton: React.FC<PrimaryButtonProps> = ({ name, onClick, ...props }) => (
  <StyledPrimaryButton onClick={onClick} {...props}>{name}</StyledPrimaryButton>
);

export default PrimaryButton;
