import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import ProjectsTable from './ProjectsTable';

// Mock RTK Query hooks
jest.mock('@/rtk/api/enhancedApi', () => ({
  userManagementApi: {
    useGetApiProjectsQuery: jest.fn(),
    useGetApiUsersGetcompaniesQuery: jest.fn(),
  },
}), { virtual: true });

const mockDataGridProps = jest.fn();
jest.mock('@shared/DataGridView/DataGridView', () => {
  return {
    __esModule: true,
    DataGridView: jest.fn((props: any) => {
        mockDataGridProps(props);
        return <div data-testid="mock-data-grid-view">
            {props.toolbar_lhs}
            {props.toolbar_rhs}
        </div>;
    })
  };
});

jest.mock('react-router-dom', () => {
  const actual = jest.requireActual('react-router-dom');
  return {
    __esModule: true,
    ...actual,
    useNavigate: () => jest.fn(),
    NavLink: jest.fn(({ children, ...props }) => <a {...props}>{children}</a>),
  };
});

jest.mock('../shared/ProjectAccessStatus', () => {
  return jest.fn(({ accessLevel }) => <span>{accessLevel}</span>);
});

const mockCompanyName = "Company A";
const mockCompanyId = "companya";

jest.mock('../shared/CompanyDropDown', () => {
  return jest.fn(({ selectedCompany, onChange }) => (
    <select data-testid="company-dropdown" onChange={(e) => onChange(e.target.value)}>
      <option value="">All companies</option>
      <option value={mockCompanyId}>{mockCompanyName}</option>
      <option value="companyb">Company B</option>
    </select>
  ));
});

const mockProjects = [
  {
    id: 'project1',
    name: 'Project 2',
    userAccess: "None",
    companyName: 'Company A',
    dataGroupCount: 0
  },
  {
    id: 'project2',
    name: 'Project 2',
    userAccess: "None",
    companyName: 'Company A',
    dataGroupCount: 2
  },
];

const mockCompanies = [
  { id: 'company1', shortCode: 'parent', displayName: 'Parent Company' },
  { id: 'company2', shortCode: 'child1', displayName: 'Child Company 1' },
  { id: 'company3', shortCode: 'child2', displayName: 'Child Company 2' },
];

describe('ProjectsTable', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders a component', () => {
  const { userManagementApi } = require('@/rtk/api/enhancedApi');
  userManagementApi.useGetApiProjectsQuery.mockReturnValue({isLoading: true});
  userManagementApi.useGetApiUsersGetcompaniesQuery.mockReturnValue({isLoading: true});
    const {container} = render(<ProjectsTable />);
    expect(container).toBeInTheDocument();
  });

  it('shows a company dropdown if there are multiple companies', () => {
  const { userManagementApi } = require('@/rtk/api/enhancedApi');
  userManagementApi.useGetApiProjectsQuery.mockReturnValue({isLoading: false, data: mockProjects});
  userManagementApi.useGetApiUsersGetcompaniesQuery.mockReturnValue({isLoading: false, data: mockCompanies});
    render(<ProjectsTable />);
    const dropdowns = screen.queryAllByRole('combobox');
    expect(dropdowns.length).toBe(1);
  });

  it('requests the filtered data from the api when a company is selected', async () => {
    const user = new userEvent.setup();
    const { userManagementApi } = require('@/rtk/api/enhancedApi');
    userManagementApi.useGetApiProjectsQuery.mockReturnValue({isLoading: false, data: mockProjects});
    userManagementApi.useGetApiUsersGetcompaniesQuery.mockReturnValue({isLoading: false, data: mockCompanies});
    render(<ProjectsTable />);
    const dropdown = screen.getByTestId('company-dropdown');
    await user.selectOptions(dropdown, mockCompanyId);
    expect(userManagementApi.useGetApiProjectsQuery).toHaveBeenCalledWith({companyId: mockCompanyId});
  });

  it('passes the projects to the data grid', () => {
    const { userManagementApi } = require('@/rtk/api/enhancedApi');
    userManagementApi.useGetApiProjectsQuery.mockReturnValue({ data: mockProjects, isLoading: false });
    render(<ProjectsTable />);
    expect(mockDataGridProps).toHaveBeenCalledWith(
      expect.objectContaining({
        rows: mockProjects
      })
    );
  });
});