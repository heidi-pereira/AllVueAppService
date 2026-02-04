import React from 'react';
import styled from 'styled-components';
import MuiCheckbox from '@mui/material/Checkbox';

const CheckboxLabel = styled.label`
  display: flex;
  align-items: center;
  font-size: 0.95rem;
  font-weight: 400;
  color: #334155;
  margin: 8px 0 8px 4px;
  cursor: pointer;
`;

const Checkbox = styled(MuiCheckbox).attrs({
  disableRipple: true,
  color: 'default',
})`
  && {
    width: 16px;
    height: 16px;
    margin-right: 10px;
    padding: 0;
    color: #1376cd;
    position: relative;

    .MuiSvgIcon-root {
      font-size: 16px;
      border-radius: 2px;
      background: #fff;
      border: 1px solid #222;
      box-sizing: border-box;
      fill: transparent;
      transition: border-color 0.2s, background 0.2s;
    }

    &.Mui-checked .MuiSvgIcon-root {
      background: #1376cd !important;
      border: 1px solid #1376cd !important;
      fill: transparent !important;
    }

    &.Mui-checked {
      &::after {
        content: "";
        position: absolute;
        left: 5px;
        top: 0px;
        width: 6px;
        height: 12px;
        border-width: 0 1.5px 1.5px 0;
        border-style: solid;
        border-color: #fff;
        transform: rotate(45deg);
        pointer-events: none;
        z-index: 2;
        box-sizing: border-box;
        display: block;
      }
    }

    &:hover .MuiSvgIcon-root {
      border-color: #1376cd;
    }

    &.Mui-disabled .MuiSvgIcon-root {
      background: #f5f6fa;
      border-color: #cbd5e1;
    }
    &.Mui-disabled {
      color: #cbd5e1 !important;
    }
    &.Mui-disabled .MuiSvgIcon-root {
      background: #f5f6fa;
      border-color: #cbd5e1;
      color: #cbd5e1 !important;
    }
    &.Mui-disabled.Mui-checked .MuiSvgIcon-root {
      background: #cbd5e1 !important;
      border: 1px solid #cbd5e1 !important;
    }
    &.Mui-disabled.Mui-checked::after {
      border-color: #fff;
    }
  }
`;

export interface BlueCheckboxProps {
  checked: boolean;
  onChange: () => void;
  label: string;
  disabled?: boolean;
}

export const BlueCheckbox: React.FC<BlueCheckboxProps> = ({
  checked,
  onChange,
  label,
  disabled = false,
}) => (
  <CheckboxLabel>
    <Checkbox checked={checked} onChange={onChange} disabled={disabled} />
    {label}
  </CheckboxLabel>
);

export default BlueCheckbox;
