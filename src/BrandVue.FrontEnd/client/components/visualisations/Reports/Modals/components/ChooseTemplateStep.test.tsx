import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import ChooseTemplateStep from './ChooseTemplateStep';

jest.mock('client/state/store', () => {
  const ReactRedux = require('react-redux');
  return {
    useAppDispatch: () => ReactRedux.useDispatch(),
    useAppSelector: (selector: any) => ReactRedux.useSelector(selector),
  };
});

jest.mock('../../../../throbber/Throbber', () => () => <div data-testid="throbber" />);
jest.mock('client/components/helpers/FuzzyDate', () => () => <span data-testid="fuzzy-date" />);
jest.mock('client/components/DeleteModal', () => (props: any) => <div data-testid="delete-modal" />);
jest.mock('react-hot-toast', () => ({ success: jest.fn(), error: jest.fn() }));

jest.mock('./ChooseTemplateStep.module.less', () => new Proxy({}, { get: () => 'cls' }));

jest.mock('./TemplateListItem', () => (props: any) => (
  <li>
    <button data-testid={`select-${props.template.id}`} onClick={(e) => { e.stopPropagation?.(); props.onSelect(); }}>Select</button>
    <button data-testid={`delete-${props.template.id}`} onClick={(e) => { e.stopPropagation?.(); props.setDeleteConfirmationModalVisible(); }}>Open Delete</button>
  </li>
));

function renderWithStore(ui: React.ReactElement, preloadedState: any) {
  const defaultTemplatesState = { templates: [], isLoading: false, error: null };
  const templatesReducer = (state = defaultTemplatesState, _action: any) => state;
  const store = configureStore({ reducer: { templates: templatesReducer } as any, preloadedState: preloadedState as any });
  return render(<Provider store={store}>{ui}</Provider>);
}

describe('ChooseTemplateStep', () => {
  const baseProps = {
    isCreatingReport: false,
    applicationConfiguration: {} as any,
    reportName: 'Report',
    onBack: jest.fn(),
    onCreate: jest.fn(),
  };

  test('shows only Throbber when loading', () => {
    const preloadedState = { templates: { templates: [], isLoading: true, error: null } };
    renderWithStore(<ChooseTemplateStep {...baseProps} />, preloadedState);
    expect(screen.getByTestId('throbber')).toBeInTheDocument();
    expect(screen.queryByText('2 of 2: Select template')).not.toBeInTheDocument();
  });

  test('renders list and details when not loading and updates selection', () => {
    const templates = [
      { id: 1, templateDisplayName: 'T1', templateDescription: 'D1', reportTemplateParts: [1,2,3], createdAt: new Date().toISOString(), savedReportTemplate: { reportType: 0 } },
      { id: 2, templateDisplayName: 'T2', templateDescription: 'D2', reportTemplateParts: [1], createdAt: new Date().toISOString(), savedReportTemplate: { reportType: 1 } },
    ] as any;
    const preloadedState = { templates: { templates, isLoading: false, error: null } };

    renderWithStore(<ChooseTemplateStep {...baseProps} />, preloadedState);

    expect(screen.getByText('2 of 2: Select template')).toBeInTheDocument();

    const itemsLabel = screen.getByText('Items');
    const initialValue = itemsLabel.parentElement?.querySelector('span:last-child');
    expect(initialValue?.textContent).toBe('3');

    fireEvent.click(screen.getByTestId('select-2'));
    const newValue = itemsLabel.parentElement?.querySelector('span:last-child');
    expect(newValue?.textContent).toBe('1');
  });

  test('opens DeleteModal when child triggers delete', () => {
    const templates = [
      { id: 1, templateDisplayName: 'T1', templateDescription: 'D1', reportTemplateParts: [], createdAt: new Date().toISOString(), savedReportTemplate: { reportType: 0 } },
    ] as any;
    const preloadedState = { templates: { templates, isLoading: false, error: null } };

    renderWithStore(<ChooseTemplateStep {...baseProps} />, preloadedState);

    fireEvent.click(screen.getByTestId('delete-1'));
    expect(screen.getByTestId('delete-modal')).toBeInTheDocument();
  });
});
