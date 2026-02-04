import '@fontsource/roboto/300.css';
import '@fontsource/roboto/400.css';
import '@fontsource/roboto/400-italic.css';
import '@fontsource/roboto/500.css';
import '@fontsource/roboto/700.css';
import 'material-symbols';
import React, { useEffect } from 'react';
import {
  createBrowserRouter,
  createRoutesFromElements,
  Route,
  RouterProvider,
    Outlet,
} from 'react-router-dom';
import { Provider, useDispatch } from 'react-redux';
import { store } from './store';
import UsersTable from './components/UsersTable/UsersTable';
import EditUser from './components/EditUser/EditUser';
import AddUser from './components/EditUser/AddUser';
import ProjectsTable from './components/ProjectsTable/ProjectsTable'
import RolePermissionTable from './components/RolesTable/RolePermissionTable';
import ProjectAccess from './components/ProjectAccess/ProjectAccess';
import EditGroup from './components/EditGroup/EditGroup';
import { Container } from '@mui/material';
import Header from './components/Header';
import './styles/cssVariables.scss';
import './App.scss';
import { getBasePathFromCurrentPage } from './urlHelper';
import { setUserContext} from './store';
import {UserContext} from './orval/api/models/userContext'
import { AccessDenied } from './AccessDenied';

const base = getBasePathFromCurrentPage();

// Layout component to wrap Header and page content
const Layout = () => (
  <>
    <Header />
    <Container>
      <Outlet /> {/* Renders the child routes */}
    </Container>
  </>
);

const router = createBrowserRouter(
  createRoutesFromElements(
    <Route  element={<Layout />}>
      <Route path="users" element={<UsersTable />} />
      <Route path="users/:userId/edit" element={<EditUser />} />
      <Route path="users/add/:companyId?" element={<AddUser />} />
      <Route path="projects" element={<ProjectsTable /> } />
      <Route path="projects/:company/:projectType/:projectId" element={<ProjectAccess /> } />
      <Route path="projects/:company/:projectType/:projectId/group/create" element={<EditGroup /> } />
      <Route path="projects/:company/:projectType/:projectId/group/:groupId" element={<EditGroup /> } />
      <Route path="manageroles" element={<RolePermissionTable />} />
      <Route path="*" element={<UsersTable />} />
    </Route>
  ),
  { basename: base }
);
interface IAppProps {
    userContext: UserContext;
}
const App: React.FC<IAppProps> = ({ userContext }) => {

    const SetUserContext = () => {
        const dispatch = useDispatch();
        const checkForUserContext = userContext;
        useEffect(() => {
            if (userContext) {
                dispatch(setUserContext(userContext));
            }
        }, [dispatch, checkForUserContext]);
        return null;
    };
    if (userContext === undefined || !userContext.hasAccessToUserManagement) {
        return (
            <Provider store={store}>
                <SetUserContext />
                <Layout />
                <AccessDenied />
            </Provider>

        )
    }
    return (
        <Provider store={store}>
            <SetUserContext />
            <RouterProvider router={router} />
        </Provider>
    );
};

export default App;
