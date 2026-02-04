import React from 'react';
import { Modal, Dropdown, DropdownMenu, DropdownItem, DropdownToggle } from 'reactstrap';
import { ModalBody } from 'react-bootstrap';
import { ControlledSearchInput } from '../../../../components/SearchInput';
import { IUserFeatureModel, IUserProjectsModel, UserProjectsModel } from '../../../../BrandVueApi';
import UserIcon from '../Users/UserIcon';
import { userContainsSearchText } from '../Users/UsersHelpers';
import { Toaster } from 'react-hot-toast';
import { useAppSelector } from '../../../../state/store';

interface IAddUsersModalProps {
    isOpen: boolean;
    setIsOpen(isOpen: boolean): void;
    currentSet: IUserFeatureModel[];
    addUsers(users: IUserProjectsModel[]): void;
}

const AddUsersModal = (props: IAddUsersModalProps) => {
    const { allUsers } = useAppSelector((state) => state.features);

    const [searchText, setSearchText] = React.useState("");
    const [selectedUsers, setSelectedUsers] = React.useState<IUserProjectsModel[]>([]);
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const [filteredUsers, setFilteredUsers] = React.useState<IUserProjectsModel[]>([])

    React.useEffect(() => {
        const available = allUsers.filter(user =>
            props.currentSet.find(userFeature => userFeature.userId == user.applicationUserId) == undefined);
        setFilteredUsers(available);
    }, [props.currentSet]);
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

    const selectUser = (user: IUserProjectsModel) => {
        let newUsers = [...selectedUsers];
        newUsers.push(user);
        setSelectedUsers(newUsers);
        setSearchText("");
    }

    const deselectUser = (userToRemove: IUserProjectsModel) => {
        let newUsers = selectedUsers.filter(user => user.applicationUserId !== userToRemove.applicationUserId);
        setSelectedUsers(newUsers);
    }

    const addUsers = () => {
        props.addUsers(selectedUsers);
        setSelectedUsers([]);
    }

    const matchedUsers = filteredUsers.filter(user => {
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
                    Give people access to this feature
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
                                    const hasMultipleOrganisations = true;
                                    return <DropdownItem key={user.applicationUserId} onClick={() => selectUser(user)}>
                                        <AddUserRow user={user} showUserOrganisation={hasMultipleOrganisations} />
                                    </DropdownItem>
                                })}
                            </DropdownMenu>
                        </Dropdown>
                        <ul className='add-user-list'>
                            {selectedUsers.map(user => {
                                const hasMultipleOrganisations = true;
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
    user: IUserProjectsModel;
    showUserOrganisation: boolean;
    deselectUser?: (user: IUserProjectsModel) => void;
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