import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import ProjectAccess from './ProjectAccess';

// Mock RTK Query hooks
jest.mock('@/rtk/api/enhancedApi', () => ({
  userManagementApi: {
  useGetApiProjectsByCompanyAndProjectTypeProjectIdQuery: jest.fn(),
  useGetApiCompaniesByCompanyIdAncestornamesQuery: jest.fn(() => [jest.fn(), { isLoading: false }]),
  usePostApiProjectsByCompanyAndProjectTypeProjectIdSetsharedMutation: jest.fn(() => [jest.fn(), { isLoading: false }]),
  useGetApiUsersdatapermissionsGetdatagroupsByCompanyAndProjectTypeProjectIdQuery: jest.fn(() => ({ data: [], isLoading: false, refetch: jest.fn() })),
  useGetApiProjectsByProjectTypeAndProjectIdLegacysharedUserQuery: jest.fn(() => ({ data: [], isLoading: false })),
  useDeleteApiProjectsByCompanyAndProjectTypeProjectIdLegacysharedUserMutation: jest.fn(() => [jest.fn(), { isLoading: false }]),
}}), { virtual: true });

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

jest.mock('../shared/CustomDialog', () => {
  return jest.fn((args) => {
    if (!args.open) return null;
    return (
      <div data-testid="mock-custom-dialog">
        <button onClick={args.onCancel}>Cancel</button>
        <button onClick={args.onConfirm}>Confirm</button>
      </div>
    );
  });
});

describe('ProjectAccess', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

    it('renders a component', () => {
        const { userManagementApi } = require('@/rtk/api/enhancedApi');
        userManagementApi.useGetApiProjectsByCompanyAndProjectTypeProjectIdQuery.mockReturnValue({isLoading: true});
        const {container} = render(<ProjectAccess />);
        expect(container).toBeInTheDocument();
    });
  
  const shareStates = [
      [false, "Share with all users"],
      [true, "Remove Share with all users"],
  ];

  test.each(shareStates)("Opens the dialog when share button is clicked and does not call the API if cancelled", async (isShared: boolean, buttonText: string) => {
    const user = new userEvent.setup();
    const { userManagementApi } = require('@/rtk/api/enhancedApi');
    const mockSetShared = jest.fn();
    mockSetShared.mockResolvedValue( { error: {status: 200} });
    userManagementApi.useGetApiProjectsByCompanyAndProjectTypeProjectIdQuery.mockReturnValue({ data: { projectId: {type: "AllVueSurvey", id: 1}, isShared: isShared, userAccess: "None", name: "Test Project" } });
    userManagementApi.usePostApiProjectsByCompanyAndProjectTypeProjectIdSetsharedMutation.mockReturnValue([mockSetShared, { isLoading: false }]);

    render(<ProjectAccess />);

    const shareButton = screen.getByRole('button', { name: buttonText });
    await user.click(shareButton);
    
    const dialog = screen.getByTestId('mock-custom-dialog');

    const cancelButton = dialog.getElementsByTagName('button')[0];
    await user.click(cancelButton);
    expect(mockSetShared).not.toHaveBeenCalled();
  });

  test.each(shareStates)("Opens the dialog when share button is clicked and calls the API if confirmed", async (isShared: boolean, buttonText: string) => {
    const user = new userEvent.setup();
    const { userManagementApi } = require('@/rtk/api/enhancedApi');
    const mockSetShared = jest.fn();
    mockSetShared.mockResolvedValue( { error: {status: 200} });
    userManagementApi.useGetApiProjectsByCompanyAndProjectTypeProjectIdQuery.mockReturnValue({ data: { projectId: { type: "AllVueSurvey", id: 1 }, isShared: isShared, userAccess: "None", name: "Test Project" }, refetch: jest.fn() });
    userManagementApi.usePostApiProjectsByCompanyAndProjectTypeProjectIdSetsharedMutation.mockReturnValue([mockSetShared, { isLoading: false }]);

    render(<ProjectAccess />);
    
    const shareButton = screen.getByRole('button', { name: buttonText});
    await user.click(shareButton);
    
    const dialog = screen.getByTestId('mock-custom-dialog');

    const confirmButton = dialog.getElementsByTagName('button')[1];
    await user.click(confirmButton);
    expect(mockSetShared).toHaveBeenCalledWith({ projectId: expect.any(Number), projectType: expect.any(String), isShared: !isShared });
  });
});