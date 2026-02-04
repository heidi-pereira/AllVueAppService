import React from 'react';
import { SwaggerException, UserProjectsModel } from '../../../../BrandVueApi';
import toast from 'react-hot-toast';
import { useUserStateContext } from './UserStateContext';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import _ from "lodash";
import AddSpecificUsersContainer from './AddSpecificUsersButton';
import { getOrganisationNamesFromUserList } from './UsersHelpers';
import { ProductConfiguration } from '../../../../ProductConfiguration';

interface IUserSettingsPageEmptyPageProps {
    openAddUserModal: () => void;
    productConfiguration: ProductConfiguration;
    clientName: string;
}

const SavantaCompanyShortcode: string = 'savanta';

const UserSettingsPageEmptyPage = (props: IUserSettingsPageEmptyPageProps) => {

    const { projectCompany, inactiveUsers, hasMultipleOrganisations, userDispatch } = useUserStateContext();
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);

    const usersGroupedByOrg = _.groupBy(inactiveUsers, user => user.organisationId);
    const organisationIds = Object.keys(usersGroupedByOrg);
    const hasNoUsers = inactiveUsers.length == 0;
    const isSavantaCompany = projectCompany?.shortCode.toLocaleLowerCase() === SavantaCompanyShortcode;

    const bulkAddUsers = (users: UserProjectsModel[]) => {
        toast.promise(userDispatch({type: 'ADD_USER_PROJECTS', data: { userIds: users.map(u => u.applicationUserId) }}), {
            loading: "Adding users...",
            success: () => {
                return `${users.length} ${users.length === 1 ? "user" : "users"} added`
            },
            error: (error) => {
                if (error && SwaggerException.isSwaggerException(error)) {
                    const swaggerException = error as SwaggerException;
                    const responseJson = JSON.parse(swaggerException.response);
                    return responseJson.message;
                }
                return "An error occurred trying to add users";
            }
        });
    }

    const shareToAllUsers = () => {
        toast.promise(userDispatch({type: 'SET_PROJECT_SHARED', data: {isShared: true}}), {
            loading: "Sharing project...",
            success: () => {
                return `Shared project to all ${props.clientName} users`
            },
            error: () => {
                return "An error occurred trying to set project as shared";
            }
        });
    }

    const getAddAllButton = () => {

        if (hasMultipleOrganisations) {
            const orgNames = getOrganisationNamesFromUserList(inactiveUsers);

            //Join string in format 'A, B, C and D'
            const joinedOrganisationNames = `${orgNames.slice(0, -1).join(', ')} and ${orgNames.slice(-1)}`;

            return (
                <>
                    <button className='primary-button' onClick={() => shareToAllUsers()} disabled={hasNoUsers}>
                        Share to all {joinedOrganisationNames} users
                    </button>
                    <ButtonDropdown isOpen={dropdownOpen} toggle={() => setDropdownOpen(!dropdownOpen)}>
                        <DropdownToggle className="primary-button bulk-add-users-dropdown-button">
                            Add existing users
                        </DropdownToggle>
                        <DropdownMenu className="bulk-add-users-dropdown">
                            {organisationIds.map(id => {
                                const users = usersGroupedByOrg[id];
                                const organisationName = users[0].organisationName;
                                return (
                                    <DropdownItem key={id} onClick={() => bulkAddUsers(users)}>Add all {organisationName} users</DropdownItem>
                                )
                            })}
                        </DropdownMenu>
                    </ButtonDropdown>
                </>
            )
        }

        return (
            <button className='primary-button' onClick={() => shareToAllUsers()} disabled={hasNoUsers}>
                Share to all {props.clientName} users
            </button>
        );
    }

    const getButtonContent = () => {
        if (isSavantaCompany) {
            return null;
        }

        return (
            <AddSpecificUsersContainer productConfiguration={props.productConfiguration}>
                {getAddAllButton()}
            </AddSpecificUsersContainer>
        );
    }

    return (
        <section className='user-settings-page empty'>
            <i className='material-symbols-outlined no-symbol-fill empty-page-icon'>lock</i>
            <div className='empty-page-description'>
                <p>This project is only visible to Savanta users.</p>
                {!isSavantaCompany && <p>You can share the project to all {props.clientName} users, or give access to specific {props.clientName} users.</p>}
            </div>

            {getButtonContent()}
        </section>
    );
}

export default UserSettingsPageEmptyPage;