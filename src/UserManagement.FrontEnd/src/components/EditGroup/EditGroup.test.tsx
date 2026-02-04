import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import EditGroup from './EditGroup';
import { User } from './GroupUsersDialog';
import { userManagementApi } from '@/rtk/apiSlice';

const mockAddDataGroup = jest.fn();
mockAddDataGroup.mockResolvedValue( { error: {status: 200} });

const mockUpdateDataGroup = jest.fn();
mockUpdateDataGroup.mockResolvedValue( { error: {status: 200} });

const mockDataGroup = { id: 1,
    ruleName: 'Group 1',
    allCompanyUsersCanAccessProject: false,
    company: 'savanta',
    projectType: 'allvuesurvey',
    projectId: 1,
    availableVariableIds: [],
    filters: [],
    userIds: []
};
const mockDataArray = [mockDataGroup];

const mockUsers: Array<User> = [
    { id: "1", name: 'User 1', email: 'user1@example.com', isSelected: false },
    { id: "2", name: 'User 2', email: 'user2@example.com', isSelected: true },
];

jest.mock('@reduxjs/toolkit/query/react', () => ({
  ...jest.requireActual('@reduxjs/toolkit/query/react'),
  skipToken: Symbol('skipToken'),
}));

// Mock RTK Query hooks
jest.mock('@/rtk/api/enhancedApi', () => ({
  userManagementApi: {
  useGetApiProjectsByCompanyAndProjectTypeProjectIdQuery: jest.fn(() => ({data: {projectId: { id: 1, type: "allvuesurvey"}, name: 'Test Project'}, isLoading: false})),
  useGetApiCompaniesByCompanyIdAncestornamesQuery: jest.fn(() => [jest.fn(), { isLoading: false }]),
  useGetApiUsersGetusersforprojectbycompanyByCompanyIdQuery: jest.fn(() => ({ data: mockUsers, isLoading: false, isFetching: false })),
  useGetApiProjectsByCompanyIdAndProjectTypeProjectIdVariablesAvailableQuery: jest.fn(() => [jest.fn(), { isLoading: false }]),
  usePostApiUsersdatapermissionsAdddatagroupMutation: jest.fn(() => [mockAddDataGroup, { isLoading: false }]),
  usePostApiUsersdatapermissionsUpdatedatagroupMutation: jest.fn(() => [mockUpdateDataGroup, { isLoading: false }]),
  useGetApiUsersdatapermissionsGetdatagroupByIdQuery: jest.fn(() => ({data: mockDataGroup, isLoading: false})),
  usePostApiProjectsByCompanyIdAndProjectTypeProjectIdFilterMutation: jest.fn(() => [
      jest.fn().mockImplementation(() => Promise.resolve({ unwrap: () => Promise.resolve(100) })),
      { isLoading: false }
  ]),
  useGetApiUsersdatapermissionsGetdatagroupsByCompanyAndProjectTypeProjectIdQuery: jest.fn(() => ({data: mockDataArray, isLoading: false})),
  }}), { virtual: true });
  
import { userManagementApi as api } from "@/rtk/api/enhancedApi";

jest.mock('react-router-dom', () => {
  const actual = jest.requireActual('react-router-dom');
  return {
    __esModule: true,
    ...actual,
    useNavigate: () => jest.fn(),
    useParams: jest.fn().mockReturnValue({groupId: undefined}),
    NavLink: jest.fn(({ children, ...props }) => <a {...props}>{children}</a>),
  };
});

import { useParams } from 'react-router-dom';
import { skipToken } from '@reduxjs/toolkit/query/react';

const userDialogMockTestId = 'mock-group-users-dialog';

jest.mock("./GroupUsersDialog", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
      return <button data-testid={userDialogMockTestId} onClick={props.onChange(mockUsers)}>users</button>;
    })
  };
});

const questionDialogMockTestId = 'mock-group-questions-dialog';
const mockQuestions = [
    { title: 'Question 1', description: 'Description 1', percent: 50, isSelected: false },
    { title: 'Question 2', description: 'Description 2', percent: 75, isSelected: true },
];

jest.mock("./GroupQuestionsDialog", () => {
  return {
    __esModule: true,
    default: jest.fn((props: any) => {
      return <button data-testid={questionDialogMockTestId} onClick={props.onChange(mockQuestions)}>questions</button>;
    })
  };
});

const filterDialogMockTestId = 'mock-group-filters-dialog';
const mockFilters = [
  {
    name: 'Filter 1',
    description: 'Description of Filter 1',
    options: [
      { name: 'Option 1', isSelected: false, percent: 20 },
      { name: 'Option 2', isSelected: true, percent: 80 }
    ]
  },
  {
    name: 'Filter 2',
    description: 'Description of Filter 2',
    options: [
      { name: 'Option 1', isSelected: false, percent: 20 },
      { name: 'Option 2', isSelected: true, percent: 80 },
      { name: 'Option 3', isSelected: true, percent: 70 }
    ]
  }
];

jest.mock("./GroupFiltersDialog", () => {
    return {
        __esModule: true,
        default: jest.fn((props: any) => {
            return (
                <button
                    data-testid={filterDialogMockTestId}
                    onClick={() => props.onChange(mockFilters)}
                >
                    filters
                </button>
            );
        })
    };
});
const enterDataGroupName = async (name: string) => {
    const user = new userEvent.setup();
    const nameInput = screen.getByLabelText(/Data group name/i);
    await user.clear(nameInput);
    if (name && name.length > 0) {
        await user.type(nameInput, name);
    }
};

const selectUsers = async () => {
    const user = new userEvent.setup();
  
    const userSelectButton = screen.getByRole('combobox', { name: /Users/i });
    await user.click(userSelectButton);
    const userSelect = screen.getByTestId(userDialogMockTestId);
    await user.click(userSelect);
};

const selectQuestions = async () => {
    const user = new userEvent.setup();

    const questionSelectButton = screen.getByRole('combobox', { name: /Questions/i });
    await user.click(questionSelectButton);
    const questionSelect = screen.getByTestId(questionDialogMockTestId);
    await user.click(questionSelect);
};

const selectFilters = async () => {
    const user = new userEvent.setup();

    const filterSelectButton = screen.getByRole('combobox', { name: /Filters/i });
    await user.click(filterSelectButton);
    const filterSelect = screen.getByTestId(filterDialogMockTestId);
    await user.click(filterSelect);
};

describe('EditGroup', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders a component', () => {
    const {container} = render(<EditGroup />);
    expect(container).toBeInTheDocument();
  });

  it('renders the project name', () => {
    render(<EditGroup />);
    expect(screen.getByDisplayValue('Test Project')).toBeInTheDocument();
  });

  it('disables the user selection when shareAll is true', async () => {
    render(<EditGroup />);
    const user = new userEvent.setup();
    const shareButton = screen.getByRole('checkbox');
    await user.click(shareButton);
    const userSelect = screen.getByRole('combobox', { name: /Users/i });
    expect(userSelect).toHaveAttribute('aria-disabled', 'true');
  });

  it('disables the save button when nothing is selected', () => {
    render(<EditGroup />);
    const saveButton = screen.getByRole('button', { name: /Create data group/i });
    expect(saveButton).toBeDisabled();
  });

  it('disables the button if the group name is empty', async () => {
    render(<EditGroup />);

    await enterDataGroupName('');
    await selectUsers();
    await selectQuestions();
    await selectFilters();

    const saveButton = screen.getByRole('button', { name: /Create data group/i });
    expect(saveButton).toBeDisabled();
  });

  it('enables the save button when a user is selected', async () => {
    render(<EditGroup />);
    const user = new userEvent.setup();

    await enterDataGroupName('Test Data Group');
    await selectUsers();

    const saveButton = screen.getByRole('button', { name: /Create data group/i });
    expect(saveButton).toBeEnabled();
  });

  it('enables the save button when a question is selected', async () => {
    render(<EditGroup />);
    const user = new userEvent.setup();

    await enterDataGroupName('Test Data Group');
    await selectQuestions();

    const saveButton = screen.getByRole('button', { name: /Create data group/i });
    expect(saveButton).toBeEnabled();
  });

  it('enables the save button when a filter is selected', async () => {
    render(<EditGroup />);
    const user = new userEvent.setup();

    await enterDataGroupName('Test Data Group');
    await selectFilters();

    const saveButton = screen.getByRole('button', { name: /Create data group/i });
    expect(saveButton).toBeEnabled();
  });

  it('calls endpoint when save button is clicked', async () => {
    render(<EditGroup />);
    const user = new userEvent.setup();
    
    await enterDataGroupName('Test Data Group');
    await selectUsers();
    await selectQuestions();
    await selectFilters();

    const saveButton = screen.getByRole('button', { name: /Create data group/i });
    await user.click(saveButton);
    expect(mockAddDataGroup).toHaveBeenCalled();
  });

  it('does not load existing group data when no id route param is present', () => {
    (useParams as jest.Mock).mockReturnValue({groupId: null});
    render(<EditGroup />);
    expect(api.useGetApiUsersdatapermissionsGetdatagroupByIdQuery).toHaveBeenCalledWith(skipToken);
  });

  it('loads existing group data when an id route param is present', () => {
    const dataGroupId = 11;
    (useParams as jest.Mock).mockReturnValue({groupId: dataGroupId});
    render(<EditGroup />);
    expect(api.useGetApiUsersdatapermissionsGetdatagroupByIdQuery).toHaveBeenCalledWith(expect.objectContaining({id: dataGroupId}));
  });

  it('saves existing group data via correct endpoint', async () => {
    const user = new userEvent.setup();
    (useParams as jest.Mock).mockReturnValue({groupId: 11});
    render(<EditGroup />);
    
    await enterDataGroupName('Test Data Group');
    await selectUsers();
    await selectQuestions();
    await selectFilters();
    
    const saveButton = screen.getByRole('button', { name: /Save changes/i });
    await user.click(saveButton);
    expect(mockUpdateDataGroup).toHaveBeenCalled();
  });

  it('disables user selection and shows info alert when no users are available', () => {
    (api.useGetApiUsersGetusersforprojectbycompanyByCompanyIdQuery as jest.Mock).mockReturnValue({ data: [], isLoading: false, isFetching: false });
    render(<EditGroup />);
    const userSelect = screen.getByRole('combobox', { name: /Users/i });
    expect(userSelect).toHaveAttribute('aria-disabled', 'true');
    expect(screen.getByLabelText(/No users added yet/i)).toBeInTheDocument();
  });
});