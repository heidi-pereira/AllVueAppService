import React from 'react';
import styled from 'styled-components';

interface ActionButtonProps {
  onClick: () => void;
  label: string;
  icon?: string;
  className?: string;
}

const StyledButton = styled.button`
  display: flex;
  align-items: center;
  justify-content: flex-start;
  flex-wrap: nowrap;
  flex-shrink: 0;

  background: #fff;
  color: #1376cd;
  border: 1px solid #1376cd;
  border-radius: 4px;

  padding: 8px 10px;
  font-size: 14px;
  line-height: 16px;
  cursor: pointer;
  outline: 0;
  transition: all 175ms ease;

  margin-bottom: 10px;

  i.material-symbols-outlined {
    font-size: 20px;
    margin-right: 4px;
  }

  .action-button-text {
    transform: translateY(1px);
  }

  &:hover {
    background-color: #e0e4f8;
    border-color: #0048b1;
  }

   &:focus {
    outline: none;
    border-color: #0048b1; /* Maintain original color */
  }

  &:not(:disabled) {
    cursor: pointer;
  }
`;

const ActionButton: React.FC<ActionButtonProps> = ({
  onClick,
  label,
  icon = 'add',
  className,
}) => (
  <StyledButton onClick={onClick} className={className}>
    <i className="material-symbols-outlined">{icon}</i>
    <div className="action-button-text">{label}</div>
  </StyledButton>
);

export default ActionButton;
