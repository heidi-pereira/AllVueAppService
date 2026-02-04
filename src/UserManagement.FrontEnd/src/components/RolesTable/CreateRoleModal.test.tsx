import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { CreateRoleModal } from './CreateRoleModal';
import '@testing-library/jest-dom';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { PermissionFeatureDto } from '../../rtk/apiSlice';

// Mock useNavigate from react-router-dom
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => jest.fn(),
}));

jest.mock('@/rtk/api/enhancedApi', () => ({
    userManagementApi: {
        usePostApiRolesMutation: () => [
            () => Promise.resolve({}),
            { isLoading: false },
        ],
        usePutApiRolesByIdMutation: () => [
            () => Promise.resolve({}),
            { isLoading: false },
        ],
  useGetApiUsercontextQuery: () => ({
    data: {
      ownerCompanyId: 'test-company-123',
      username: 'test-user'
    },
    isLoading: false,
    error: null
  }),
    },
}));

const mockUser = { userId: 'test-user-id', name: 'Test User' };
const mockUserDetailsReducer = (state = { user: mockUser }, action: any) => state;

const renderWithRedux = (ui: React.ReactElement, { initialState = {}, store = configureStore({
    reducer: { userDetailsReducer: mockUserDetailsReducer },
    preloadedState: { userDetailsReducer: { user: mockUser }, ...initialState },
}) } = {}) => {
    return render(<Provider store={store}>{ui}</Provider>);
};

describe('CreateRoleModal', () => {
  const permissionGroups: PermissionFeatureDto[] = [
    {
      id: 1,
      systemKey: 'group1',
      name: 'Group 1',
      options: [
        { id: 1, name: 'Permission 1' },
        { id: 2, name: 'Permission 2' },
      ],
    },
    {
      id: 2,
      systemKey: 'group2',
      name: 'Group 2',
      options: [
        { id: 3, name: 'Permission 3' },
      ],
    },
  ];

  const setup = (props = {}) =>
      renderWithRedux(
      <CreateRoleModal
        open={true}
        onClose={jest.fn()}
        permissionGroups={permissionGroups}
        allRoles={[]}
        {...props}
      />
    );

  beforeAll(() => {
    // Mock window.location.reload to avoid jsdom error
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: { reload: jest.fn() }
    });
  });

  it('renders when open', () => {
      renderWithRedux(
      <CreateRoleModal
        open={true}
        onClose={jest.fn()}
        permissionGroups={permissionGroups}
        allRoles={[]}
      />
    );
    expect(screen.getByText('Create role')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('E.g. Special user')).toBeInTheDocument();
    expect(screen.getByText('Features')).toBeInTheDocument();
  });

  it('does not render when closed', () => {
      const { container } = renderWithRedux(
      <CreateRoleModal
        open={false}
        onClose={jest.fn()}
        permissionGroups={permissionGroups}
        allRoles={[]}
      />
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('disables Create role button if role name is empty', () => {
    setup();
    expect(screen.getByText('Create role')).toBeDisabled();
  });

  it('shows error if role name is empty and input is blurred', async () => {
    setup();
    const input = screen.getByPlaceholderText('E.g. Special user');
    fireEvent.change(input, { target: { value: 'd' } });
    fireEvent.change(input, { target: { value: '' } });
    fireEvent.blur(input);
    await expect(
      screen.findByText('Role name is required.')
    ).resolves.toBeInTheDocument();
  });

  it('shows error if role name exceeds 35 characters', () => {
    setup();
    const input = screen.getByPlaceholderText('E.g. Special user');
    fireEvent.change(input, { target: { value: 'a'.repeat(36) } });
    expect(screen.getByText('Role name must be 35 characters or less.')).toBeInTheDocument();
    expect(screen.getByText('Create role')).toBeDisabled();
  });

  it('shows error if role name is a duplicate of an existing role', async () => {
    const allRoles = [
        { id: 1, roleName: 'Admin', organisationId: 'test-company-123', options: [], updatedByUserId: 'user1', updatedDate: new Date() },
        { id: 2, roleName: 'Manager', organisationId: 'test-company-123', options: [], updatedByUserId: 'user2', updatedDate: new Date() }
    ];
    setup({ allRoles });

    const input = screen.getByPlaceholderText('E.g. Special user');
    fireEvent.change(input, { target: { value: 'Admin' } });
    fireEvent.blur(input);

    await waitFor(() => {
        expect(screen.getByText('Role name already in use. Please use a different name')).toBeInTheDocument();
        expect(screen.getByText('Create role')).toBeDisabled();
    });
  });

  it('enables Create role button when valid role name and permission selected', () => {
    setup();
    const input = screen.getByPlaceholderText('E.g. Special user');
    fireEvent.change(input, { target: { value: 'Valid Name' } });
    fireEvent.click(screen.getByText('Group 1'));
    fireEvent.click(screen.getByLabelText('Permission 1'));
    expect(screen.getByText('Create role')).not.toBeDisabled();
  });

  it('calls onClose when Cancel is clicked', () => {
    const onClose = jest.fn();
      renderWithRedux(
      <CreateRoleModal
        open={true}
        onClose={onClose}
        permissionGroups={permissionGroups}
        allRoles={[]}
      />
    );
    fireEvent.click(screen.getByText('Cancel'));
    expect(onClose).toHaveBeenCalled();
  });
});
