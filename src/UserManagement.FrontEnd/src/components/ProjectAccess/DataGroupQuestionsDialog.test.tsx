import React from 'react';
import userEvent from '@testing-library/user-event';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { Variable } from "@/rtk/apiSlice";
import DataGroupQuestionsDialog from './DataGroupQuestionsDialog';

jest.mock('react-router-dom', () => {
  const actual = jest.requireActual('react-router-dom');
  return {
    __esModule: true,
    ...actual,
    useNavigate: () => jest.fn(),
    NavLink: jest.fn(({ children, ...props }) => <a {...props}>{children}</a>),
  };
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

interface ITestProps {
    open: boolean;
    dataGroupName: string;
    questions: Array<Variable>;
    onClose: () => void;
}

const mockOnClose = jest.fn();
const defaultProps: ITestProps = {
    open: true,
    dataGroupName: "Test Data Group",
    questions: [
        { id: 1, name: 'Question 1', description: 'Description 1', options: [] },
    ],
    onClose: mockOnClose,
};

const renderComponent = (props: ITestProps) => {
  return render(
      <DataGroupQuestionsDialog {...props} />
  );
};

describe('DataGroupQuestionsDialog', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('renders a component', () => {
        const {container} = renderComponent(defaultProps);
        expect(container).toBeInTheDocument();
    });

    it('has a close button when opened', () => {
        renderComponent(defaultProps);
        const closeButton = screen.getAllByRole('button', { name: /Close/i });
        expect(closeButton).toHaveLength(2);
    });

    it('calls onClose when close button is clicked', async () => {
        const user = new userEvent.setup();
        renderComponent(defaultProps);
        const closeButtons = screen.getAllByRole('button', { name: /Close/i });
        await user.click(closeButtons[0]);
        expect(mockOnClose).toHaveBeenCalledTimes(1);
    });
});