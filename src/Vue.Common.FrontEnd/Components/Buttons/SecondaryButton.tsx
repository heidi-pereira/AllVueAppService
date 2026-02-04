import React from 'react';
import styled from 'styled-components';

interface SecondaryButtonProps {
  name: string;
  onClick: () => void;
  disabled?: boolean;
}

const StyledSecondaryButton = styled.button`
  display: flex;
  flex-direction: row;
  align-items: center;
  font-size: 14px;
  background-color: #eeeff0;
  color: #42484d;
  padding: 10px 20px;
  line-height: 16px;
  border: 1px solid #eeeff0;
  border-radius: 4px;
  outline: 0;
  flex-shrink: 0;
  transition: all 175ms ease;
  width: 120px;
  justify-content: center;
  gap: 5px;

  &:hover {
    background-color: #cbcfd3;
    color: #26292c;
    border-color: #cbcfd3;
  }

  &:active,
  &:focus,
  &[aria-expanded="true"] {
    background-color: #cbcfd3;
    color: #26292c;
    border-color: #cbcfd3;
    box-shadow: 0px 0px 3px 3px rgba(224, 226, 229, 0.48);
    outline: 0;
  }

  &:disabled {
    opacity: 0.3;
    cursor: not-allowed;
    background: #eeeff0;
    border-color: #eeeff0;

    &:hover {
      background: #eeeff0;
      border-color: #eeeff0;
    }

    &:active,
    &:focus {
      box-shadow: none;
      background: #eeeff0;
      border-color: #eeeff0;
    }
  }
`;

const SecondaryButton: React.FC<SecondaryButtonProps> = ({ name, onClick, disabled }) => (
  <StyledSecondaryButton onClick={onClick} disabled={disabled}>
    {name}
  </StyledSecondaryButton>
);

export default SecondaryButton;
