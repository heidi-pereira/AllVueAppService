import React from 'react';
import toast from 'react-hot-toast';
import { useUserStateContext } from './UserStateContext';
import _ from "lodash";
import AddSpecificUsersContainer from './AddSpecificUsersButton';
import { getOrganisationNamesFromUserList } from './UsersHelpers';
import { ProductConfiguration } from '../../../../ProductConfiguration';

interface IUserSettingsPageIsSharedPageProps {
    openAddUserModal: () => void;
    productConfiguration: ProductConfiguration;
    clientName: string;
}

const UserSettingsPageIsSharedPage = (props: IUserSettingsPageIsSharedPageProps) => {

    const { activeUsers, inactiveUsers, hasMultipleOrganisations, userDispatch } = useUserStateContext();
    const hasNoUsers = inactiveUsers.length == 0;

    const removeShareToAllUsers = () => {
        toast.promise(userDispatch({type: 'SET_PROJECT_SHARED', data: {isShared: false}}), {
            loading: "Removing access to project...",
            success: () => {
                return `Removed access for all ${props.clientName} users`
            },
            error: () => {
                return "An error occurred trying to remove project access";
            }
        });
    }

    const getRemoveAllButton = () => {

        let orgNames = props.clientName;

        if (hasMultipleOrganisations) {
            const allUsers = activeUsers.concat(inactiveUsers);
            const allOrganisationNames = getOrganisationNamesFromUserList(allUsers);
            //Join string in format 'A, B, C and D'
            orgNames = `${allOrganisationNames.slice(0, -1).join(', ')} and ${orgNames.slice(-1)}`;
        }

        return (
            <button className='negative-button' onClick={() => removeShareToAllUsers()} disabled={hasNoUsers}>
                Remove access for all {orgNames} users
            </button>
        );
    }

    return (
        <section className='user-settings-page empty'>
            <i className='material-symbols-outlined no-symbol-fill empty-page-icon'>lock_open</i>
            <div className='empty-page-description'>
                <p className='visibility-message'>This project is visible to all {props.clientName} users.</p>
                <p>New users added to the {props.clientName} company will automatically have access.</p>
            </div>

            <AddSpecificUsersContainer productConfiguration={props.productConfiguration}>
                {getRemoveAllButton()}
            </AddSpecificUsersContainer>
        </section>
    );
}

export default UserSettingsPageIsSharedPage;