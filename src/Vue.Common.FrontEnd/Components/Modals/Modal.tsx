import React, { ReactNode } from 'react';
import styled from 'styled-components';
import MuiModal from '@mui/material/Modal';
import Box from '@mui/material/Box';
import CloseIcon from '@mui/icons-material/Close';

interface ModalProps {
  open: boolean;
  onClose: () => void;
  header?: ReactNode;
  footer?: ReactNode;
  children: ReactNode;
}

const ModalContent = styled.div`
  border-radius: 4px;
  height: 100%;
  display: flex;
  flex-direction: column;
`;

const Header = styled.div`
  font-size: 18px;
  text-align: center;
  padding: 32px 32px 0 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
`;

const Title = styled.div`
  margin: 0 auto;
  flex: 1;
  text-align: center;
  font-size: 18px;
  color: #212529;
`;

const CloseButton = styled.button`
  position: absolute;
  right: 32px;
  top: 32px;
  color: #64748b;
  border: none;
  background: none;
  outline: 0;
  cursor: pointer;
  padding: 4px;
  font-size: 1.5rem;

  &:hover {
    color: #334155;
    transition: color 175ms ease;
  }
  
  &:active,
  &:focus {
    outline: none;
  }
`;

const Content = styled.div`
  display: flex;
  flex-direction: column;
  gap: 30px;
  padding: 20px;
  flex: 1;
`;

const Footer = styled.div`
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 24px 32px;
  gap: 16px;
  margin-top: auto;
`;

const StyledModal = styled(MuiModal)`
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1300;

  & .MuiBox-root {
    overflow-y: auto;
    &::-webkit-scrollbar {
      width: 8px;
      height: 8px;
      background: #eee;
    }
    scrollbar-width: thin;
    scrollbar-color: #e2e8f0 #eee;
  }
`;

const ModalContainer = styled(Box)`
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0px 3px 24px rgba(0, 0, 0, 0.12);
  width: 100%;
  max-width: 1100px;
  height: 85%;
  display: flex;
  flex-direction: column;
  outline: none;
  padding: 0;
`;

const Modal: React.FC<ModalProps> = ({ open, onClose, header, footer, children }) => {
  if (!open) return null;

  return (
    <StyledModal
      open={open}
      onClose={onClose}
      aria-labelledby="modal-title"
      aria-describedby="modal-description"
      closeAfterTransition
    >
      <ModalContainer>
        <ModalContent>
          <Header>
            {header && <Title id="modal-title">{header}</Title>}
            <CloseButton onClick={onClose} aria-label="Close">
              <CloseIcon />
            </CloseButton>
          </Header>
          <Content>
            {children}
          </Content>
          {footer && <Footer>{footer}</Footer>}
        </ModalContent>
      </ModalContainer>
    </StyledModal>
  );
};

export default Modal;
