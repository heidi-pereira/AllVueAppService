import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import '@testing-library/jest-dom';
import { toast } from 'mui-sonner';

// Mock useNavigate from react-router-dom
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
  useParams: () => ({ userId: 'test-user-id-123' }),
}));

const mockLocation = {
  href: '',
};
Object.defineProperty(window, 'location', {
  value: mockLocation,
  writable: true,
});

// Mock the API slice
const mockUpdateUser = jest.fn();

const mockProducts = [
    { name: 'Eating Out', url: 'eatingout', projectId: { type: "BrandVue", id: "143" } },
    { name: 'Finance', url: 'finance', projectId: { type: "BrandVue", id: "148" } }
];


jest.mock('../../rtk/apiSlice', () => ({
  useGetApiUserGetByUserIdQuery: jest.fn(),
  useGetApiRolesByCompanyIdQuery: jest.fn(),
  useGetApiUsercontextQuery: jest.fn(),
  usePostApiUserMutation: jest.fn(),
  useGetApiProductsGetproductsQuery: jest.fn(),
  useGetApiCompaniesByCompanyIdQuery: jest.fn(),
}), { virtual: true });

jest.mock('@shared/Inputs/InputContainer', () => 
  ({ label, value, onChange, disabled }: any) => (
    <div>
      <label>{label}</label>
      <input 
        value={value || ''} 
        onChange={onChange} 
        disabled={disabled}
        data-testid={`input-${label.toLowerCase().replace(/\s+/g, '-')}`}
      />
    </div>
  )
);

jest.mock('../shared/PageTitle', () => (props: any) => <div>{props.title}</div>);

// Mock urlHelper
jest.mock('../../urlHelper', () => ({
  getBasePathFromCurrentPage: () => '/usermanagement',
}));

// Import the component after all mocks are set up
import EditUser from './EditUser';

// Import the mocked hooks after mocking
import {
  useGetApiUserGetByUserIdQuery,
  useGetApiRolesByCompanyIdQuery,
  useGetApiUsercontextQuery,
  usePostApiUserMutation,
  useGetApiProductsGetproductsQuery,
  useGetApiCompaniesByCompanyIdQuery
} from '../../rtk/apiSlice';

describe('EditUser', () => {
  const mockUsers = [
    {
      id: 'test-user-id-123',
      firstName: 'John',
      lastName: 'Doe',
      email: 'john.doe@example.com',
      role: 'User',
      ownerCompanyDisplayName: 'Test Company',
      ownerCompanyId: 'company-123',
      ownerCompanyShortCode: 'TESTCOMPANY',
    },
    {
      id: 'test-user-id-456',
      firstName: 'Jane',
      lastName: 'Smith',
      email: 'jane.smith@example.com',
      role: 'Admin',
      ownerCompanyDisplayName: 'Another Company',
      ownerCompanyId: 'company-456',
      ownerCompanyShortCode: 'ANOTHERCO',
    },
  ];

  const mockRoles = [
    { id: 1, roleName: 'User' },
    { id: 2, roleName: 'Admin' },
    { id: 3, roleName: 'Manager' },
  ];

  const mockUserContext = {
    userId: 'current-user-id',
    authCompany: 'testcompany',
    userOrganisation: 'TESTCOMPANY',
    };

  const mockCompanyData = {
      id: 'company-123',
      name: 'TESTCOMPANY',
      childCompanies: [],
      products: [
          { type: "BrandVue", id: "143" },
          { type: "BrandVue", id: "148" }
      ],
      surveyVueEditingAvailable: true,
      surveyVueFeedbackAvailable: true,
      shortCode: 'testcompany',
      displayName: 'TESTCOMPANY',
      url: "https://testcompany.test.all-vue.com",
      hasExternalSSOProvider: true

  };
  const defaultMocks = {
      useGetApiUserGetByUserIdQuery: {
          data: {...mockUsers[0]},
          isLoading: false,
          error: null,
      },
      useGetApiRolesByCompanyIdQuery: {
          data: mockRoles,
          isLoading: false,
          error: null,
      },
      useGetApiUsercontextQuery: {
          data: mockUserContext,
      },
      usePostApiUserMutation: [
          mockUpdateUser,
          { isLoading: false, error: null },
      ],
      useGetApiProductsGetproductsQuery: {
          data: mockProducts,
          isLoading: false,
          error: null,
      },
      useGetApiCompaniesByCompanyIdQuery: {
          data: mockCompanyData,
            isLoading: false,
            error: null,
      },
  };

  const renderComponent = (overrides = {}) => {
    const mocks = { ...defaultMocks, ...overrides };
    
    (useGetApiUserGetByUserIdQuery as jest.Mock).mockReturnValue(mocks.useGetApiUserGetByUserIdQuery);
    (useGetApiRolesByCompanyIdQuery as jest.Mock).mockReturnValue(mocks.useGetApiRolesByCompanyIdQuery);
    (useGetApiUsercontextQuery as jest.Mock).mockReturnValue(mocks.useGetApiUsercontextQuery);
    (usePostApiUserMutation as jest.Mock).mockReturnValue(mocks.usePostApiUserMutation);
    (useGetApiProductsGetproductsQuery as jest.Mock).mockReturnValue(mocks.useGetApiProductsGetproductsQuery);
    (useGetApiCompaniesByCompanyIdQuery as jest.Mock).mockReturnValue(mocks.useGetApiCompaniesByCompanyIdQuery);

    return render(
      <MemoryRouter>
        <EditUser />
      </MemoryRouter>
    );
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockUpdateUser.mockResolvedValue({
    });
    mockLocation.href = '';
  });

  describe('Loading States', () => {
    it('shows loading spinner when users are loading', () => {
      renderComponent({
          useGetApiUserGetByUserIdQuery: {
          data: null,
          isLoading: true,
          error: null,
        },
      });

      expect(screen.getByRole('progressbar')).toBeInTheDocument();
    });

    it('shows loading spinner when roles are loading', () => {
      renderComponent({
        useGetApiRolesByCompanyIdQuery: {
          data: null,
          isLoading: true,
          error: null,
        },
      });

      expect(screen.getByRole('progressbar')).toBeInTheDocument();
    });
  });

  describe('Error States', () => {
    it('shows error message when users fail to load', () => {
      renderComponent({
          useGetApiUserGetByUserIdQuery: {
          data: null,
          isLoading: false,
          error: {
              data: { error: 'Failed to load users' },
              status:500,
          },
        },
      });

      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.getByText(/Failed to load users/i)).toBeInTheDocument();
    });

    it('shows error message when roles fail to load', () => {
      renderComponent({
        useGetApiRolesByCompanyIdQuery: {
          data: null,
          isLoading: false,
          error: {
              data: 'Failed to load roles' ,
              status: 500,
          }
        },
      });

      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.getByText(/Failed to load roles/i)).toBeInTheDocument();
    });

    it('shows error message when user is not found', () => {
      renderComponent({
          useGetApiUserGetByUserIdQuery: {
          data: {},
          isLoading: false,
          error: { data: { error: 'user not found' } },
        },
      });

      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.getByText(/user not found/i)).toBeInTheDocument();
    });

    it('shows error message when updating user fails', () => {
      renderComponent({
        usePostApiUserMutation: [
            mockUpdateUser,
          { isLoading: false, error: 'Assignment failed' },
        ],
      });

        expect(screen.getByText(/failed to update user/i)).toBeInTheDocument();
    });
  });

  describe('Form Display', () => {
    it('renders the form with user data', () => {
      renderComponent();

      expect(screen.getByText('Edit User')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Test Company')).toBeInTheDocument();
      expect(screen.getByDisplayValue('John')).toBeInTheDocument();
      expect(screen.getByDisplayValue('Doe')).toBeInTheDocument();
      expect(screen.getByDisplayValue('john.doe@example.com')).toBeInTheDocument();
    });

    it('renders all role options in the dropdown', () => {
      renderComponent();

      const roleSelect = screen.getByRole('combobox');
      fireEvent.mouseDown(roleSelect);

      expect(screen.getAllByText('User')).toHaveLength(2); // One in the dropdown, one in the select
      expect(screen.getByText('Admin')).toBeInTheDocument();
      expect(screen.getByText('Manager')).toBeInTheDocument();
    });

    it('has company, and email fields disabled', () => {
      renderComponent();

      expect(screen.getByDisplayValue('Test Company')).toBeDisabled();
      expect(screen.getByDisplayValue('john.doe@example.com')).toBeDisabled();
    });

    it('has role dropdown enabled', () => {
      renderComponent();

      const roleSelect = screen.getByRole('combobox');
      expect(roleSelect).not.toBeDisabled();
    });
  });

  describe('Role Selection', () => {
    it('allows selecting a different role', () => {
      renderComponent();

      const roleSelect = screen.getByRole('combobox');
      fireEvent.mouseDown(roleSelect);
      fireEvent.click(screen.getByText('Admin'));

      expect(screen.getByDisplayValue('Admin')).toBeInTheDocument();
    });

    it('displays the current user role as selected', () => {
      renderComponent();

      expect(screen.getByDisplayValue('User')).toBeInTheDocument();
    });
  });

  describe('Form Submission', () => {
    it('calls assignUserRole with correct parameters when Save is clicked', async () => {
      renderComponent();

      // Change role
      const roleSelect = screen.getByRole('combobox');
      fireEvent.mouseDown(roleSelect);
      fireEvent.click(screen.getByText('Admin'));

      // Click Save
      fireEvent.click(screen.getByRole('button', { name: /update user/i }));

      await waitFor(() => {
        expect(toast.error).not.toHaveBeenCalled();
          expect(mockUpdateUser).toHaveBeenCalledWith({
          user: {
            "email": "john.doe@example.com",
            "firstName": "John",
            "id": "test-user-id-123",
            "lastName": "Doe",
            "ownerCompanyDisplayName": "Test Company",
            "ownerCompanyId": "company-123",
            "ownerCompanyShortCode": "TESTCOMPANY",
            "role": "Admin",
            "roleId": 2,
          },
        });
      });
    });



    it('does not submit if no user context is available', async () => {
      renderComponent({
        useGetApiUsercontextQuery: {
          data: null,
        },
      });

      const roleSelect = screen.getByRole('combobox');
      fireEvent.mouseDown(roleSelect);
      fireEvent.click(screen.getByText('Admin'));

      fireEvent.click(screen.getByRole('button', { name: /update user/i }));

      await waitFor(() => {
          expect(mockUpdateUser).not.toHaveBeenCalled();
      });
    });

    it('handles save failure gracefully', async () => {
        mockUpdateUser.mockResolvedValue({
            error: {
                status: 501,
                data: { error: 'Network Error' }
            }
      });

      renderComponent();

      const roleSelect = screen.getByRole('combobox');
      fireEvent.mouseDown(roleSelect);
      fireEvent.click(screen.getByText('Admin'));

      fireEvent.click(screen.getByRole('button', { name: /update user/i }));

        await waitFor(() => {
            expect(toast.error).toHaveBeenCalled();
      });

    });
  });

  describe('Loading States During Save', () => {
    it('disables buttons and shows loading text when saving', () => {
      renderComponent({
        usePostApiUserMutation: [
            mockUpdateUser,
          { isLoading: true, error: null },
        ],
      });

      expect(screen.getByRole('button', { name: /updating.../i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled();
      expect(screen.getByRole('button', { name: /updating.../i })).toBeDisabled();
    });
  });

  describe('Cancel Functionality', () => {
    it('navigates to home page when Cancel is clicked', () => {
      renderComponent();

      fireEvent.click(screen.getByRole('button', { name: /cancel/i }));

      expect(mockNavigate).toHaveBeenCalledWith('/');
    });

    it('disables Cancel button when saving', () => {
      renderComponent({
        usePostApiUserMutation: [
            mockUpdateUser,
          { isLoading: true, error: null },
        ],
      });

      expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled();
    });
  });

  describe('Edge Cases', () => {
    it('handles missing role gracefully during save', async () => {
      const consoleError = jest.spyOn(console, 'error').mockImplementation(() => {});
      
      renderComponent({
        useGetApiRolesByCompanyIdQuery: {
          data: [],
          isLoading: false,
          error: null,
        },
      });

      fireEvent.click(screen.getByRole('button', { name: /update user/i }));

      await waitFor(() => {
          expect(mockUpdateUser).not.toHaveBeenCalled();
      });

      consoleError.mockRestore();
    });

    it('handles user with no role set', () => {
      const usersWithoutRole = [
        {
          ...mockUsers[0],
          role: null,
        },
      ];

      renderComponent({
          useGetApiUserGetByUserIdQuery: {
          data: usersWithoutRole[0],
          isLoading: false,
          error: null,
        },
      });

      const hiddenInput = document.querySelector('input[aria-hidden="true"]') as HTMLInputElement;
      expect(hiddenInput?.value).toBe('');
    });

    it('handles missing form data', () => {
      renderComponent({
          useGetApiUserGetByUserIdQuery: {
          data: { id: 'test-user-id-123' }, // User with minimal data
          isLoading: false,
          error: null,
        },
      });

      // Should not crash and should handle empty values
      expect(screen.getByTestId('input-company')).toHaveValue('');
      expect(screen.getByTestId('input-first-name')).toHaveValue('');
      expect(screen.getByTestId('input-last-name')).toHaveValue('');
      expect(screen.getByTestId('input-email-address')).toHaveValue('');
    });
  });

  describe('Company-based Role Filtering', () => {
    it('calls roles API with company ID from current user', () => {
      renderComponent();

      expect(useGetApiRolesByCompanyIdQuery).toHaveBeenCalledWith(
        { companyId: 'company-123' },
        { skip: false }
      );
    });

    it('skips roles query when current user is not available', () => {
      renderComponent({
          useGetApiUserGetByUserIdQuery: {
          data: {},
          isLoading: false,
          error: null,
        },
      });

      expect(useGetApiRolesByCompanyIdQuery).toHaveBeenCalledWith(
        { companyId: '' },
        { skip: true }
      );
    });

    it('skips roles query when ownerCompanyId is not available', () => {
      const usersWithoutCompanyId = [
        {
          ...mockUsers[0],
          ownerCompanyId: null,
        },
      ];

      renderComponent({
          useGetApiUserGetByUserIdQuery: {
          data: usersWithoutCompanyId[0],
          isLoading: false,
          error: null,
        },
      });

      expect(useGetApiRolesByCompanyIdQuery).toHaveBeenCalledWith(
        { companyId: '' },
        { skip: true }
      );
    });

    it('uses empty string as company ID when ownerCompanyId is not available', () => {
      const usersWithEmptyCompanyId = [
        {
          ...mockUsers[0],
          ownerCompanyId: '',
        },
      ];

      renderComponent({
          useGetApiUserGetByUserIdQuery: {
          data: usersWithEmptyCompanyId[0],
          isLoading: false,
          error: null,
        },
      });

      expect(useGetApiRolesByCompanyIdQuery).toHaveBeenCalledWith(
        { companyId: '' },
        { skip: true }
      );
    });
  });

  describe('Product Availability and Disabled State', () => {
      it('shows available products in the form', () => {
          const customCompanyData = {
              ...mockCompanyData,
              hasExternalSSOProvider: false
          };
          renderComponent({
              useGetApiCompaniesByCompanyIdQuery: {
                  data: customCompanyData,
                  isLoading: false,
                  error: null,
              }
          });

          // Check that both products are rendered
          expect(screen.getByText('Eating Out')).toBeInTheDocument();
          expect(screen.getByText('Finance')).toBeInTheDocument();

          const productAInput = screen.getByLabelText('Eating Out');
          expect(productAInput).not.toBeDisabled();

          const productBInput = screen.getByLabelText('Finance');
          expect(productBInput).not.toBeDisabled();
      });

      it('disables product input if product is disabled', () => {
          renderComponent();

          // Check that both products are rendered
          expect(screen.getByText('Eating Out')).toBeInTheDocument();
          expect(screen.getByText('Finance')).toBeInTheDocument();

          const productAInput = screen.getByLabelText('Eating Out');
          expect(productAInput).toBeDisabled();

          const productBInput = screen.getByLabelText('Finance');
          expect(productBInput).toBeDisabled();

      });
  });
});
