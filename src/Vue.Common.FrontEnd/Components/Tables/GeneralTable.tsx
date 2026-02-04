import React from 'react';
import styled from 'styled-components';
import { TableContainer, Paper } from '@mui/material';

const StyledTableContainer = styled(TableContainer)`
  max-width: 100%;
  border-radius: 0px;
  box-shadow: none;

  table {
    width: 100%;
    border-collapse: collapse;
    border: 1px solid #e0e2e5 !important;
    color: #333 !important;

    thead,
    tfoot {
      background-color: #f9f9f9;

      th,
      td {
        background-color: #f9f9f9;
        background-clip: padding-box;
      }
    }

    thead th {
      position: sticky;
      top: 0;
      min-width: 125px;
      font-weight: bold;
    }

    tfoot th,
    tfoot td {
      position: sticky;
      bottom: 0;
      z-index: 1;
    }

    tbody tr {
      border-bottom: 1px solid #e0e2e5 !important;

      &:hover td,
      &:hover th {
        background-color: #f1f6fb;
      }

      td,
      th {
        background: white;
        background-clip: padding-box;
        padding: 10px;
        text-align: left;
        font-weight: 400;
        vertical-align: top;
      }

      th:first-child,
      td:first-child {
        text-align: left;
        min-width: 20rem;
      }
    }
  }
`;

const NoBorderPaper = styled(Paper)`
  box-shadow: none !important;
  border: none !important;
`;

const GeneralTable: React.FC<{
  children: React.ReactNode;
}> = ({ children }) => {
  return <StyledTableContainer component={NoBorderPaper}>{children}</StyledTableContainer>;
};

export default GeneralTable;