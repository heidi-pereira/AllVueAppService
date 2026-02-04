import React from 'react';
import { Modal, Dropdown, DropdownMenu, DropdownItem, DropdownToggle } from 'reactstrap';
import { ModalBody } from 'react-bootstrap';
import { ControlledSearchInput } from '../../../../components/SearchInput';
import { SwaggerException, UserProjectsModel } from '../../../../BrandVueApi';
import UserIcon from './UserIcon';
import { useUserStateContext } from './UserStateContext';
import toast, { Toaster } from 'react-hot-toast';
import { userContainsSearchText } from './UsersHelpers';

interface IAddUsersModalProps {
    isOpen: boolean;
    setIsOpen(isOpen: boolean): void;
}

const AddUsersModal = (props: IAddUsersModalProps) => {

    const { inactiveUsers, hasMultipleOrganisations, userDispatch } = useUserStateContext();

    const [searchText, setSearchText] = React.useState("");
    const [selectedUsers, setSelectedUsers] = React.useState<UserProjectsModel[]>([]);
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);

    const closeModal = () => {
        props.setIsOpen(false);
        setSearchText("");
        setSelectedUsers([]);
        setDropdownOpen(false);
    }

    const toggle = () => {
        setDropdownOpen(!dropdownOpen);
    }

    const searchTextChanged = (searchText: string) => {
        setSearchText(searchText);

        if (searchText.length > 0) {
            setDropdownOpen(true);
        }
    }

    const selectUser = (user: UserProjectsModel) => {
        let newUsers = [...selectedUsers];
        newUsers.push(user);
        setSelectedUsers(newUsers);
        setSearchText("");
    }

    const deselectUser = (userToRemove: UserProjectsModel) => {
        let newUsers = selectedUsers.filter(user => user.applicationUserId !== userToRemove.applicationUserId);
        setSelectedUsers(newUsers);
    }

    const addUsers = () => {
        const selectedUserIds = selectedUsers.map(user => user.applicationUserId);

        toast.promise(userDispatch({type: 'ADD_USER_PROJECTS', data: {userIds: selectedUserIds}}), {
            loading: "Adding users...",
            success: () => {
                closeModal();
                return `${selectedUserIds.length} ${selectedUserIds.length === 1 ? "user" : "users"} added`
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

    const matchedUsers = inactiveUsers.filter(user => {
        return !selectedUsers.some(u => u.applicationUserId === user.applicationUserId) && userContainsSearchText(user, searchText);
    });

    const getAddButtonText = () => {
        if (selectedUsers.length === 1) {
            return "Add user";
        }

        return "Add users";
    }

    return (
        <Modal isOpen={props.isOpen} className="add-users-modal" centered keyboard={false} autoFocus={false}>
            <ModalBody>
                <button className="modal-close-button" onClick={closeModal} title="Close">
                    <i className="material-symbols-outlined">close</i>
                </button>
                <h1 className="header">
                    Add users
                </h1>
                <div className="details">
                    Give people access to this project
                </div>
                <div className="content-and-buttons">
                    <div className="content">
                        <Dropdown isOpen={dropdownOpen} className='add-user-dropdown' toggle={toggle}>
                            <DropdownToggle tag="div" className="question-search-container">
                                <ControlledSearchInput id="user-search"
                                                       text={searchText}
                                                       onChange={(text) => searchTextChanged(text)}
                                                       className="question-search-input-group"
                                                       autoFocus={true}
                                                       placeholder={"Search for a user"}
                                                       ariaLabel="Users able to be added to project"/>
                            </DropdownToggle>
                            <DropdownMenu>
                                {matchedUsers.map(user => {
                                    return <DropdownItem key={user.applicationUserId} onClick={() => selectUser(user)}>
                                        <AddUserRow user={user} showUserOrganisation={hasMultipleOrganisations} />
                                    </DropdownItem>
                                })}
                            </DropdownMenu>
                        </Dropdown>
                        <ul className='add-user-list'>
                            {selectedUsers.map(user => {
                                return <AddUserRow key={user.applicationUserId} user={user} deselectUser={deselectUser} showUserOrganisation={hasMultipleOrganisations} />
                            })}
                        </ul>
                    </div>
                    <div className="modal-buttons">
                        <button className="modal-button secondary-button" onClick={closeModal}>Cancel</button>
                        <button className="modal-button primary-button" onClick={addUsers} disabled={selectedUsers.length === 0}>{getAddButtonText()}</button>
                    </div>
                </div>
            </ModalBody>
            <Toaster position='bottom-center' toastOptions={{duration: 5000}} />
        </Modal>
    );
}

interface IAddUserRowProps {
    user: UserProjectsModel;
    showUserOrganisation: boolean;
    deselectUser?: (user: UserProjectsModel) => void;
}

const AddUserRow = ({user, showUserOrganisation, deselectUser}: IAddUserRowProps) => {
    return (
        <li className='add-user-row'>
            <UserIcon firstName={user.firstName} lastName={user.lastName} role={user.roleName} />
            <div className='user-dropdown-title'>
                <div className='full-name'>
                    {user.firstName} {user.lastName} {showUserOrganisation && <span className='company-name'>({user.organisationName})</span>}
                </div>
                <div className='email-address'>{user.email}</div>
            </div>
            {deselectUser &&
                <button className="deselect-user-button" onClick={() => deselectUser(user)} title="Deselect user">
                    <i className="material-symbols-outlined">close</i>
                </button>
            }
        </li>
    )
}

export default AddUsersModal;