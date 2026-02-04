import React from 'react';
import { render, screen, fireEvent, within } from '@testing-library/react';
import '@testing-library/jest-dom';
import RolePermissionTable from './RolePermissionTable';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';

// Mock child components
jest.mock('@shared/Buttons/AddActionButton', () => (props: any) => (
  <button onClick={props.onClick}>{props.label}</button>
));
jest.mock('../shared/PageTitle', () => (props: any) => <div>{props.title}</div>);
jest.mock('./CreateRoleModal', () => (props: any) => (
    <div id="modal-title">{props.role ? 'Edit Role' : 'Create Role'}</div>
));

// Mock RTK Query hooks
jest.mock('../../rtk/apiSlice', () => ({
  useGetApiFeaturesQuery: jest.fn(),
  useGetApiUsersGetcompaniesQuery: jest.fn(),
  useDeleteApiRolesByIdMutation: jest.fn(() => [jest.fn(), { isLoading: false }]),
}), { virtual: true });

// Create a mock store
const createMockStore = (initialState = {}) => {
  return configureStore({
    reducer: {
      userDetailsReducer: (state = { user: {
          userOrganisation: 'test-company',
          isSystemAdministrator: true,
      } }) => state,
    },
    preloadedState: initialState,
  });
};

jest.mock('@/rtk/api/enhancedApi', () => ({
    userManagementApi: {
        useGetApiRolesByCompanyIdQuery: jest.fn(),
    },
}));

const mockFeatures = [
    {
      id: 1,
      name: 'FeatureA',
      options: [
      { id: 1, name: 'Read' },
      { id: 2, name: 'Write' },
    ],
  },
    {
      id: 2,
      name: 'FeatureB',
      options: [
      { id: 3, name: 'Execute' },
    ],
  },
];

const mockRoles = [
  {
    id: 'role1',
    roleName: 'AdminRole',
    permissions: [{ id: 1, name: 'Read' }, { id: 3, name: 'Execute' }],
  },
  {
    id: 'role2',
    roleName: 'UserRole',
    permissions: [{ id: 1, name: 'Read' }],
  },
];

const mockCompanies = [
  {
    id: 'company-123',
    shortCode: 'test-company',
    name: 'Test Company'
  },
];

// Helper function to render with Redux provider
const renderWithProvider = (component: React.ReactElement, initialState = {}) => {
  const store = createMockStore(initialState);
  return render(
    <Provider store={store}>
      {component}
    </Provider>
  );
};

describe('RolePermissionTable', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('shows loading state', () => {
    const { useGetApiFeaturesQuery, useGetApiUsersGetcompaniesQuery } = require('../../rtk/apiSlice');
    const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
    useGetApiFeaturesQuery.mockReturnValue({ isLoading: true });
    useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: true });
    useGetApiUsersGetcompaniesQuery.mockReturnValue({ isLoading: true });
    renderWithProvider(<RolePermissionTable />);
    expect(screen.getByText(/Loading features/i)).toBeInTheDocument();
  });

  it('shows error state', () => {
    const { useGetApiFeaturesQuery, useGetApiUsersGetcompaniesQuery } = require('../../rtk/apiSlice');
    const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
    useGetApiFeaturesQuery.mockReturnValue({ isLoading: false, error: 'err', data: null });
    useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: false, error: 'rolesErr', data: null });
    useGetApiUsersGetcompaniesQuery.mockReturnValue({ isLoading: false, error: 'companiesErr', data: mockCompanies });
    renderWithProvider(<RolePermissionTable />);
    // Check for error messages separately
    expect(screen.getByText('Error: err')).toBeInTheDocument();
    expect(screen.getByText('Error: rolesErr')).toBeInTheDocument();
  });

  it('renders table with roles and features', () => {
    const { useGetApiFeaturesQuery, useGetApiUsersGetcompaniesQuery } = require('../../rtk/apiSlice');
    const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
    useGetApiFeaturesQuery.mockReturnValue({ isLoading: false, error: null, data: mockFeatures });
    useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: false, error: null, data: mockRoles });
    useGetApiUsersGetcompaniesQuery.mockReturnValue({ isLoading: false, error: null, data: mockCompanies });
    renderWithProvider(<RolePermissionTable />);
    expect(screen.getByText('Manage roles')).toBeInTheDocument();
    expect(screen.getByText('Create role')).toBeInTheDocument();
    expect(screen.getByText('FeatureA')).toBeInTheDocument();
    expect(screen.getByText('FeatureB')).toBeInTheDocument();
    expect(screen.getByText('AdminRole')).toBeInTheDocument();
    expect(screen.getByText('UserRole')).toBeInTheDocument();
    // Permissions: check for correct cell values
    // Admin: FeatureA = Read, FeatureB = Execute
    expect(screen.getAllByText('Read')[0]).toBeInTheDocument();
    expect(screen.getByText('Execute')).toBeInTheDocument();
    // User: FeatureA = Read, FeatureB = No access
    expect(screen.getAllByText('Read')[1]).toBeInTheDocument();
    expect(screen.getAllByText('No access')[0]).toBeInTheDocument();
  });

  it('calls handleCreateRole on button click', () => {
    const { useGetApiFeaturesQuery, useGetApiUsersGetcompaniesQuery } = require('../../rtk/apiSlice');
    const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
    useGetApiFeaturesQuery.mockReturnValue({ isLoading: false, error: null, data: mockFeatures });
    useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: false, error: null, data: mockRoles });
    useGetApiUsersGetcompaniesQuery.mockReturnValue({ isLoading: false, error: null, data: mockCompanies });
    window.alert = jest.fn();
    renderWithProvider(<RolePermissionTable />);
    fireEvent.click(screen.getByText('Create role'));
    // Modal should be open with the title "Create Role"
    const modalTitles = screen.getAllByText("Create Role");
    // Find the modal title by id
    expect(
      modalTitles.some(el => el.id === "modal-title")
    ).toBe(true);
  });

    it('shows Edit menu item for roles', async () => {
        const { useGetApiFeaturesQuery, useGetApiUsersGetcompaniesQuery } = require('../../rtk/apiSlice');
        const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
        useGetApiFeaturesQuery.mockReturnValue({ isLoading: false, error: null, data: mockFeatures });
        useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: false, error: null, data: mockRoles });
        useGetApiUsersGetcompaniesQuery.mockReturnValue({ isLoading: false, error: null, data: mockCompanies });
        renderWithProvider(<RolePermissionTable />);
        const adminRow = screen.getByRole('row', { name: /adminrole/i });
        const menuButton = within(adminRow).getByRole('button');
        fireEvent.click(menuButton);
        expect(await screen.findByText('Edit')).toBeInTheDocument();
    });

    it('shows Delete menu item for roles', async () => {
        const { useGetApiFeaturesQuery } = require('../../rtk/apiSlice');
        const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
        useGetApiFeaturesQuery.mockReturnValue({ isLoading: false, error: null, data: mockFeatures });
        useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: false, error: null, data: mockRoles });
        renderWithProvider(<RolePermissionTable />);
        const adminRow = screen.getByRole('row', { name: /adminrole/i });
        const menuButton = within(adminRow).getByRole('button');
        fireEvent.click(menuButton);
        expect(await screen.findByText('Delete')).toBeInTheDocument();
    });

    it('opens modal in edit mode when Edit is clicked', async () => {
        const { useGetApiFeaturesQuery, useGetApiUsersGetcompaniesQuery } = require('../../rtk/apiSlice');
        const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
        useGetApiFeaturesQuery.mockReturnValue({ isLoading: false, error: null, data: mockFeatures });
        useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: false, error: null, data: mockRoles });
        useGetApiUsersGetcompaniesQuery.mockReturnValue({ isLoading: false, error: null, data: mockCompanies });
        renderWithProvider(<RolePermissionTable />);
        const adminRow = screen.getByRole('row', { name: /adminrole/i });
        const menuButton = within(adminRow).getByRole('button');
        fireEvent.click(menuButton);
        fireEvent.click(await screen.findByText('Edit'));
        expect(await screen.findByText('Edit Role')).toBeInTheDocument();
    });

    it('opens confirmation dialog when Delete is clicked', async () => {
        const { useGetApiFeaturesQuery } = require('../../rtk/apiSlice');
        const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
        useGetApiFeaturesQuery.mockReturnValue({ isLoading: false, error: null, data: mockFeatures });
        useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: false, error: null, data: mockRoles });
        renderWithProvider(<RolePermissionTable />);
        const adminRow = screen.getByRole('row', { name: /adminrole/i });
        const menuButton = within(adminRow).getByRole('button');
        fireEvent.click(menuButton);
        fireEvent.click(await screen.findByText('Delete'));
        expect(await screen.findByText('Delete role')).toBeInTheDocument();
    });

    it('calls delete mutation when delete is confirmed', async () => {
        const { useGetApiFeaturesQuery, useDeleteApiRolesByIdMutation } = require('../../rtk/apiSlice');
        const { useGetApiRolesByCompanyIdQuery } = require('@/rtk/api/enhancedApi').userManagementApi;
        useGetApiFeaturesQuery.mockReturnValue({ isLoading: false, error: null, data: mockFeatures });
        useGetApiRolesByCompanyIdQuery.mockReturnValue({ isLoading: false, error: null, data: mockRoles, refetch: jest.fn() });
        const mockDelete = jest.fn();
        mockDelete.mockResolvedValue({});
        useDeleteApiRolesByIdMutation.mockReturnValue([mockDelete, { isLoading: false }]);
        renderWithProvider(<RolePermissionTable />);
        const adminRow = screen.getByRole('row', { name: /adminrole/i });
        const menuButton = within(adminRow).getByRole('button');
        fireEvent.click(menuButton);
        fireEvent.click(await screen.findByText('Delete'));
        expect(await screen.findByText('Delete role')).toBeInTheDocument();
        fireEvent.click(await screen.findByText('Delete'));
        expect(mockDelete).toHaveBeenCalledWith({ id: 'role1'});
    });
});