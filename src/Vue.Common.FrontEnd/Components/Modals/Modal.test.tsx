import React from 'react';
import '@testing-library/jest-dom';
import { render, screen, fireEvent } from '@testing-library/react';
import Modal from './Modal';

describe('Modal', () => {
  const headerText = 'Test Header';
  const footerText = 'Test Footer';
  const childrenText = 'Modal Content';

  it('renders nothing when open is false', () => {
    const { container } = render(
      <Modal open={false} onClose={jest.fn()}>
        {childrenText}
      </Modal>
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders header, children, and footer when open', () => {
    render(
      <Modal open={true} onClose={jest.fn()} header={headerText} footer={footerText}>
        {childrenText}
      </Modal>
    );
    expect(screen.getByText(headerText)).toBeInTheDocument();
    expect(screen.getByText(childrenText)).toBeInTheDocument();
    expect(screen.getByText(footerText)).toBeInTheDocument();
  });

  it('calls onClose when close button is clicked', () => {
    const onClose = jest.fn();
    render(
      <Modal open={true} onClose={onClose} header={headerText}>
        {childrenText}
      </Modal>
    );
    const closeButton = screen.getByLabelText(/close/i);
    fireEvent.click(closeButton);
    expect(onClose).toHaveBeenCalled();
  });
});
