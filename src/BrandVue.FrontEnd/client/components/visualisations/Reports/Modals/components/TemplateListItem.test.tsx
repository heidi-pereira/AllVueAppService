import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import TemplateListItem from './TemplateListItem';
import { ReportType } from 'client/BrandVueApi';

const baseTemplate = {
  id: 1,
  templateDisplayName: 'Sales Report',
  templateDescription: 'Monthly sales figures',
  createdAt: '2025-08-25T12:00:00Z',
  reportTemplateParts: [{}, {}],
  savedReportTemplate: { reportType: ReportType.Chart },
};

describe('TemplateListItem', () => {
  it('renders template name, description and item count', () => {
    render(
      <TemplateListItem
        template={baseTemplate as any}
        selected={false}
        onSelect={() => {}}
        setDeleteConfirmationModalVisible={() => {}}
      />
    );
    expect(screen.getByText('Sales Report')).toBeInTheDocument();
    expect(screen.getByText('Monthly sales figures')).toBeInTheDocument();
    expect(screen.getByText('2 items')).toBeInTheDocument();
  });

  it('shows chart icon for Chart type', () => {
    render(
      <TemplateListItem
        template={baseTemplate as any}
        selected={false}
        onSelect={() => {}}
        setDeleteConfirmationModalVisible={() => {}}
      />
    );
    expect(screen.getByText('bar_chart')).toBeInTheDocument();
  });

  it('shows table icon for Table type', () => {
    const tableTemplate = {
      ...baseTemplate,
      savedReportTemplate: { reportType: ReportType.Table },
    };
    render(
      <TemplateListItem
        template={tableTemplate as any}
        selected={false}
        onSelect={() => {}}
        setDeleteConfirmationModalVisible={() => {}}
      />
    );
    expect(screen.getByText('table_chart')).toBeInTheDocument();
  });

  it('calls onSelect when clicked', () => {
    const onSelect = jest.fn();
    render(
      <TemplateListItem
        template={baseTemplate as any}
        selected={false}
        onSelect={onSelect}
        setDeleteConfirmationModalVisible={() => {}}
      />
    );
    fireEvent.click(screen.getByText('Sales Report'));
    expect(onSelect).toHaveBeenCalled();
  });

  it('calls setDeleteConfirmationModalVisible when delete clicked', () => {
    const setDeleteConfirmationModalVisible = jest.fn();
    render(
      <TemplateListItem
        template={baseTemplate as any}
        selected={false}
        onSelect={() => {}}
        setDeleteConfirmationModalVisible={setDeleteConfirmationModalVisible}
      />
    );
    fireEvent.click(screen.getByRole('button'));
    fireEvent.click(screen.getByText('Delete Template'));
    expect(setDeleteConfirmationModalVisible).toHaveBeenCalled();
  });
});
