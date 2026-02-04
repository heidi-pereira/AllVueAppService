import React from 'react';
import Throbber from '../../../throbber/Throbber';
import AddUsersModal from './AddUsersModal';
import { UserStateProvider, useUserStateContext } from './UserStateContext';
import SearchInput from "../../../SearchInput";
import UserSettingsPageTable from './UserSettingsPageTable';
import UserSettingsPageEmptyPage from './UserSettingsPageEmptyPage';
import UserSettingsPageIsSharedPage from './UserSettingsPageIsSharedPage';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import toast from 'react-hot-toast';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { ProductConfiguration } from '../../../../ProductConfiguration';
import { PageHandler } from '../../../PageHandler';

interface IUsersSettingsPageProps {
    productConfiguration: ProductConfiguration;
}

export const UsersSettingsPage = (props: IUsersSettingsPageProps) => {

    const { activeUsers, projectCompany, inactiveUsers, isLoading, isSharedToAllUsers, userDispatch } = useUserStateContext();
    const [isAddUserModalOpen, setIsAddUserModalOpen] = React.useState<boolean>(false);
    const [hamburgerMenuOpen, setHamburgerMenuOpen] = React.useState<boolean>(false);

    const [searchText, setSearchText] = React.useState("");

    const clientName = projectCompany?.displayName ?? inactiveUsers[0]?.organisationName ?? "external";

    const shareToAllUsers = () => {
        toast.promise(userDispatch({type: 'SET_PROJECT_SHARED', data: {isShared: true}}), {
            loading: "Sharing project...",
            success: () => {
                return `Shared project to all ${clientName} users`
            },
            error: () => {
                return "An error occurred trying to set project as shared";
            }
        });
    }

    if (isLoading) {
        return <section className='user-settings-page'>
            <div className="throbber-container-fixed">
                <Throbber />
            </div>
        </section>
    }

    if (isSharedToAllUsers) {
        return (
            <UserSettingsPageIsSharedPage
                openAddUserModal={() => setIsAddUserModalOpen(true)}
                productConfiguration={props.productConfiguration}
                clientName={clientName}
            />
        )
    }

    if (activeUsers.length === 0) {
        return (
            <UserSettingsPageEmptyPage
                openAddUserModal={() => setIsAddUserModalOpen(true)}
                productConfiguration={props.productConfiguration}
                clientName={clientName}
            />
        )
    }

    return (
        <section className='user-settings-page'>
            <h1 className="users-page-title">Users</h1>
            <div className="user-page-search-and-actions">
                <div className="question-search-container">
                    <SearchInput id="user-search-bar" className="question-search-input-group" onChange={setSearchText} text={searchText} ariaLabel="Users in project" />
                </div>
                <button className='primary-button add-user-btn' onClick={() => setIsAddUserModalOpen(true)}>
                    <i className='material-symbols-outlined'>add</i>
                    <div>Add users</div>
                </button>
                <ButtonDropdown isOpen={hamburgerMenuOpen} toggle={() => setHamburgerMenuOpen(!hamburgerMenuOpen)} className='users-page-more-info-dropdown'>
                    <DropdownToggle className="hollow-button dropdown-toggle users-page-more-info-btn">
                        <i className='material-symbols-outlined'>more_vert</i>
                    </DropdownToggle>
                    <DropdownMenu end={true}>
                        <DropdownItem onClick={() => shareToAllUsers()}>
                            <span>
                                <i className='material-symbols-outlined no-symbol-fill'>lock_open</i>
                                Share access to all {clientName} users
                            </span>
                        </DropdownItem>
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
            <UserSettingsPageTable searchText={searchText} />
            <AddUsersModal
                isOpen={isAddUserModalOpen}
                setIsOpen={setIsAddUserModalOpen}
            />
        </section>
    );
}

interface UsersSettingsPageProps {
    productConfiguration: ProductConfiguration;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
}

const UsersSettingsPageContextWrapper = (props: UsersSettingsPageProps) => {
    return (
        <UserStateProvider subProductId={props.productConfiguration.subProductId} googleTagManager={props.googleTagManager} pageHandler={props.pageHandler}>
            <UsersSettingsPage productConfiguration={props.productConfiguration} />
        </UserStateProvider>
    );
}

export default UsersSettingsPageContextWrapper;