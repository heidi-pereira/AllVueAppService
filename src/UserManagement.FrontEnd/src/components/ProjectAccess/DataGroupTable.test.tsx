import React from 'react';
import userEvent from '@testing-library/user-event';
import { act, render, screen, within } from '@testing-library/react';
import '@testing-library/jest-dom';
import { DataGroup, ProjectType } from "@/rtk/apiSlice";
import DataGroupTable from './DataGroupTable';

const mockDataGroupDeleteMethod = jest.fn().mockReturnValue({error: null });
jest.mock('@/rtk/api/enhancedApi', () => ({
  userManagementApi: {
  useGetApiUsersGetusersforprojectbycompanyByCompanyIdQuery: jest.fn(() => [jest.fn(), { isLoading: false }]),
  useGetApiProjectsByCompanyIdAndProjectTypeProjectIdVariablesAvailableQuery: jest.fn(() => [jest.fn(), { isLoading: false }]),
  useDeleteApiUsersdatapermissionsDeleteallvueruleByIdMutation: jest.fn(() => [mockDataGroupDeleteMethod, { isLoading: false }]),
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

jest.mock('./DataGroupQuestionsDialog', () => {
  return jest.fn((args) => {
    if (!args.open) return null;
    return (
      <div data-testid="mock-questions-dialog">
        <button onClick={args.onClose}>Close</button>
      </div>
    );
  });
});

jest.mock('./DataGroupUsersDialog', () => {
  return jest.fn((args) => {
    if (!args.open) return null;
    return (
      <div data-testid="mock-users-dialog">
        <button onClick={args.onClose}>Close</button>
      </div>
    );
  });
});

const mockDataGridProps = jest.fn();
jest.mock('@shared/DataGridView/DataGridView', () => {
  return {
    __esModule: true,
    DataGridView: jest.fn((props: any) => {
        mockDataGridProps(props);
        return <div data-testid="mock-data-grid-view"></div>;
    })
  };
});

jest.mock('../shared/CustomDialog', () => {
  return jest.fn((args) => {
    if (!args.open) return null;
    return (
      <div data-testid="mock-delete-dialog">
        <button onClick={args.onClose}>Close</button>
        <button onClick={args.onConfirm}>Delete</button>
      </div>
    );
  });
});

interface TestProps {
    dataGroups: DataGroup[];
    companyId?: string;
    projectId?: number;
    projectType?: ProjectType;
    editUrl: string;
    isLoading: boolean;
}

const defaultProps: TestProps = {
    dataGroups: [
        { id: 1, ruleName: 'Group 1', availableVariableIds: [1, 2], allCompanyUsersCanAccessProject: false, company: 'Company', projectType: 'AllVueSurvey', projectId: 1, filters: [], userIds: [] }, 
        { id: 2, ruleName: 'Group 2', availableVariableIds: [], allCompanyUsersCanAccessProject: false, company: 'Company', projectType: 'AllVueSurvey', projectId: 1, filters: [], userIds: [] }
    ],
    editUrl: '/projects/sample/1/group/',
    companyId: 'Company',
    isLoading: false,
}

const renderComponent = (props: TestProps) => {
  return render(
      <DataGroupTable {...props} />
  );
};

describe('DataGroupTable', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('renders a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it("doesn't render the DataGridView when there are no data groups", () => {
        const {container} = renderComponent({...defaultProps, dataGroups: []});
        expect(container).toBeInTheDocument();
        expect(screen.queryByTestId('mock-data-grid-view')).not.toBeInTheDocument();
    });

    it('renders the DataGridView when there are data groups', () => {
        renderComponent(defaultProps);
        expect(screen.getByTestId('mock-data-grid-view')).toBeInTheDocument();
    });

    it('has a menu with edit and delete options for each data group', () => {
        renderComponent(defaultProps);
        expect(mockDataGridProps).toHaveBeenCalledWith(expect.objectContaining({
            perRowOptions: expect.any(Function)
        }));
        const result = mockDataGridProps.mock.calls[0][0].perRowOptions(defaultProps.dataGroups[0]);
        expect(result).toHaveLength(2);
        expect(result.find((option: any) => option.label === 'Edit')).toBeDefined();
        expect(result.find((option: any) => option.label === 'Delete')).toBeDefined();
    });

    it('shows an alert when the user clicks to delete a data group', () => {
        renderComponent(defaultProps);
        const result = mockDataGridProps.mock.calls[0][0].perRowOptions(defaultProps.dataGroups[0]);
        const deleteOption = result.find((option: any) => option.label === 'Delete');
        act(() => {
          deleteOption.onClick();
        });
        expect(screen.getByTestId('mock-delete-dialog')).toBeInTheDocument();
    });

    it('calls the delete mutation when confirming deletion of a data group', async () => {
        renderComponent(defaultProps);
        const result = mockDataGridProps.mock.calls[0][0].perRowOptions(defaultProps.dataGroups[0]);
        const deleteOption = result.find((option: any) => option.label === 'Delete');
        act(() => {
          deleteOption.onClick();
        });
        const dialog = screen.getByTestId('mock-delete-dialog');
        const deleteButton = within(dialog).getByText('Delete');
        await act(async () => {
          await userEvent.click(deleteButton);
        });
        expect(mockDataGroupDeleteMethod).toHaveBeenCalledWith({
            id: defaultProps.dataGroups[0].id,
            projectId: defaultProps.dataGroups[0].projectId,
            projectType: defaultProps.dataGroups[0].projectType,
            company: defaultProps.dataGroups[0].company
        });
    });
  });