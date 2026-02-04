import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom'; // Importing jest-dom for the toBeInTheDocument matcher
import GeneralTable from './GeneralTable';

describe('GeneralTable', () => {
  it('renders children inside the styled table container', () => {
    render(
      <GeneralTable>
        <table>
          <thead>
            <tr>
              <th>Header</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Cell</td>
            </tr>
          </tbody>
        </table>
      </GeneralTable>
    );
    expect(screen.getByText('Header')).toBeInTheDocument();
    expect(screen.getByText('Cell')).toBeInTheDocument();
  });

  it('applies custom styles to the container', () => {
    const { container } = render(
      <GeneralTable>
        <table>
          <tbody>
            <tr>
              <td>Test</td>
            </tr>
          </tbody>
        </table>
      </GeneralTable>
    );
    // Check for styled-components class on TableContainer
    const tableContainer = container.querySelector('.MuiTableContainer-root');
    expect(tableContainer).toBeTruthy();
    // Check for styled Paper component
    const paper = container.querySelector('.MuiPaper-root');
    expect(paper).toBeTruthy();
  });
});
