import React from 'react';
import { createContext, useContext, useEffect, useState } from "react";
import { CompanyModel, Factory, UserProject, UserProjectsModel } from "../../../../BrandVueApi";
import { IGoogleTagManager } from '../../../../googleTagManager';
import { PageHandler } from '../../../PageHandler';

export type UserAction =
    | {type: 'ADD_USER_PROJECTS'; data: { userIds: string[] } }
    | {type: 'REMOVE_USER_PROJECTS'; data: { userId: string } }
    | {type: 'SET_PROJECT_SHARED'; data: { isShared: boolean }};

export interface UserContextState {
    projectCompany: CompanyModel | undefined;
    activeUsers: UserProjectsModel[];
    inactiveUsers: UserProjectsModel[];
    isLoading: boolean;
    isSharedToAllUsers: boolean;
    hasMultipleOrganisations: boolean;
    userDispatch: (action: UserAction) => Promise<void>;
}

export const UserStateContext = createContext<UserContextState>({
    projectCompany: undefined,
    activeUsers: [],
    inactiveUsers: [],
    isSharedToAllUsers: false,
    isLoading: false,
    hasMultipleOrganisations: false,
    userDispatch: () => Promise.resolve()});

export const useUserStateContext = () => useContext(UserStateContext);

interface IUserContextProviderProps {
    subProductId: string;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    children: any;
}

export const UserStateProvider = (props: IUserContextProviderProps) => {

    const usersClient = Factory.UsersClient(throwError => handleError(() => throwError()));

    const [, handleError] = useState();
    const [projectCompany, setProjectCompany] = useState<CompanyModel>();
    const [users, setUsers] = useState<UserProjectsModel[]>([]);
    const [isSharedToAllUsers, setIsSharedToAllUsers] = useState<boolean>(false);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const reloadUsers = () => {
        setIsLoading(true);

        usersClient.getUsers()
            .then(r => {
                setProjectCompany(r.projectCompany);
                setUsers(r.users);
                setIsSharedToAllUsers(r.isSharedToAllUsers);
                setIsLoading(false);
            }).catch((e: Error) => {
                setIsLoading(false);
                handleError(() => {throw e});
            });
    }

    useEffect(() => reloadUsers(), []);

    const sortAlphabeticallyByLastName = (a: UserProjectsModel, b: UserProjectsModel) => {
        return a.lastName.localeCompare(b.lastName);
    }

    const activeUsers = users.filter(u => u.projects.some(p => p.projectId === props.subProductId));
    activeUsers.sort(sortAlphabeticallyByLastName);

    const inactiveUsers = users.filter(u => !u.projects.some(p => p.projectId === props.subProductId));
    inactiveUsers.sort(sortAlphabeticallyByLastName);

    const organisationIds = new Set(users.map(u => u.organisationId));
    const hasMultipleOrganisations = organisationIds.size > 1;

    const asyncDispatch = async (action: UserAction): Promise<void> => {
        switch (action.type) {
            case 'ADD_USER_PROJECTS':
                props.googleTagManager.addEvent("userSettingsAddUsers", props.pageHandler);

                const userProjects = action.data.userIds.map(userId => new UserProject({id: 0, applicationUserId: userId, projectId: props.subProductId }));
                return usersClient.addUserProjects(isSharedToAllUsers, userProjects).then(() => reloadUsers());
            case 'REMOVE_USER_PROJECTS':
                props.googleTagManager.addEvent("userSettingsRemoveUser", props.pageHandler);

                const projects = activeUsers.find(user => user.applicationUserId === action.data.userId)?.projects
                if (!projects){
                    throw new Error (`Unable to find user: ${action.data.userId}`)
                }
                const currentUserProject = projects.find(project => project.projectId === props.subProductId)
                if (!currentUserProject) {
                    throw new Error (`Unable to find project: ${props.subProductId} for user: ${action.data.userId}`)
                }
                return usersClient.removeUserFromProject(currentUserProject.id, action.data.userId).then(() => reloadUsers());
            case 'SET_PROJECT_SHARED':
                return usersClient.setProjectShared(action.data.isShared).then(() => reloadUsers());
        }
    }

    return (
        <UserStateContext.Provider value={{
            projectCompany: projectCompany,
            activeUsers: activeUsers,
            inactiveUsers: inactiveUsers,
            isLoading: isLoading,
            isSharedToAllUsers: isSharedToAllUsers,
            hasMultipleOrganisations: hasMultipleOrganisations,
            userDispatch: asyncDispatch}}
        >
            {props.children}
        </UserStateContext.Provider>
    )
}