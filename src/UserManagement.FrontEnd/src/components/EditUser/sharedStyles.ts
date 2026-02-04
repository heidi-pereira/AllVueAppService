import styled from 'styled-components';
import { Box,Alert } from '@mui/material';

export const StyledButtonContainer = styled(Box)`
    display: flex;
    gap: 16px;
    margin-top: 32px;
    justify-content: flex-start;
`;

export const StyledRoleContainer = styled.div`
    display: flex;
    flex-direction: column;
    width: 100%;
    position: relative;
    margin-bottom: 8px;
`;

export const StyledRoleLabel = styled.label`
    color: #212529;
    font-size: 1rem;
    line-height: 1.5;
    margin-bottom: 4px;
    display: block;
`;
export const StyledNameContainer = styled(Box)`
    display: flex;
    gap: 16px;
    margin-bottom: 16px;
`;

export const StyledEmailRoleContainer = styled(Box)`
    display: flex;
    gap: 16px;
    margin-bottom: 16px;
`;
export const StyledAlert = styled(Alert)`
    margin-bottom: 16px;
`;
