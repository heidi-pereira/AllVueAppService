import React from 'react';
import styled from 'styled-components';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import { NavLink } from 'react-router-dom';

const NavLinkStyled = styled(NavLink)`
  display: flex;
  align-items: center;
  font-size: 1.25rem;
  padding: 16px 0;
  color: #333;
  text-decoration: none;
  &:hover {
    text-decoration: underline;
    color: #333 !important;
  }
`;

const PageTitleContainer = styled.div`
  display: flex;
  align-items: center;
  font-size: 1.25rem;
  padding: 16px 0;
  color: #333;
  a {
    color: #333;
    text-decoration: none;
    &:hover {
      text-decoration: underline;
      color: #333 !important;
    }
  }
`;

interface PageTitleProps {
  title: string;
  href?: string;
  children?: React.ReactNode;
}

const PageTitle: React.FC<PageTitleProps> = ({ title, href, children }) => {
  return (
    <PageTitleContainer>
      {href && 
        <NavLinkStyled to={href}>
          <ChevronLeftIcon />
        </NavLinkStyled>
      }
      {title}
      {children}
    </PageTitleContainer>
  );
};

export default PageTitle;