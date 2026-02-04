import React from 'react';
import { OverlayTrigger, Tooltip } from 'react-bootstrap';
import { GetRoleDescriptionFromName, GetRoleDisplayNameFromName } from './RoleHelpers';
import UserIcon from './UserIcon';
import { useUserStateContext } from './UserStateContext';
import { userContainsSearchText } from './UsersHelpers';
import { IApplicationUser, UserProjectsModel } from "../../../../BrandVueApi";
import RemoveUserFromProjectModal from './RemoveUserFromProjectModal';
import { UserContext } from "../../../../GlobalContext";

interface IUserSettingsPageTableInternalProps {
    searchText: string;
    currentUser: IApplicationUser | null;
}

const UserSettingsPageTableInternal = ({ searchText, currentUser }: IUserSettingsPageTableInternalProps) => {

    const { activeUsers, hasMultipleOrganisations } = useUserStateContext();
    const filteredUsers = activeUsers.filter(user => userContainsSearchText(user, searchText));

    const [removeUserIsOpen, setRemoveUserIsOpen] = React.useState(false);
    const [selectedUser, setSelectedUser] = React.useState<UserProjectsModel | undefined>();

    return (
        <div className="table-scroll-container">
            <table className='users-table'>
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Email</th>
                        {hasMultipleOrganisations && <th>Company</th>}
                        <th>Role</th>
                        <th />
                    </tr>
                </thead>
                <tbody>
                    {filteredUsers.map(u => {

                        const isCurrentUserSelected = u.applicationUserId === currentUser?.userId;

                        const toolTip = <Tooltip id={`tooltip-role-${u.applicationUserId}`} className="role-tooltip">
                            {GetRoleDescriptionFromName(u.roleName)}
                        </Tooltip>;

                        return <tr key={u.applicationUserId}>
                            <td>
                                <div className='name-cell'>
                                    <UserIcon firstName={u.firstName} lastName={u.lastName} role={u.roleName} />
                                    <div className='full-name'>
                                        {u.firstName} {u.lastName}
                                        {isCurrentUserSelected ? " (you)" : ""}
                                    </div>
                                </div>
                            </td>
                            <td>
                                <div className='email-address'>{u.email}</div>
                            </td>
                            {hasMultipleOrganisations &&
                                <td>
                                    <div className='company-name'>{u.organisationName}</div>
                                </td>
                            }
                            <OverlayTrigger
                                placement='top'
                                trigger={["hover", "focus"]}
                                overlay={toolTip}
                            >
                                {({ ref, ...triggerHandler }) => (
                                    <td {...triggerHandler}>
                                        <span ref={ref}>{GetRoleDisplayNameFromName(u.roleName)}</span>
                                    </td>
                                )}
                            </OverlayTrigger>
                            <td className='remove-user-cell'>
                                <button className="remove-user-button secondary-button" onClick={() => {
                                    setRemoveUserIsOpen(true)
                                    setSelectedUser(u)
                                }}>{isCurrentUserSelected ? "Leave" : "Remove"}</button>
                            </td>
                        </tr>;
                    })}
                </tbody>
            </table>
            {selectedUser && <RemoveUserFromProjectModal isOpen={removeUserIsOpen} setIsOpen={setRemoveUserIsOpen} currentUser={currentUser} selectedUser={selectedUser} />}
        </div>
    );
};

interface IUserSettingsPageTableProps  {
    searchText: string;
}

const UserSettingsPageTable = (props: IUserSettingsPageTableProps) => {
    return (
        <UserContext.Consumer key="user-page">
            {(user) =>
                <UserSettingsPageTableInternal searchText={props.searchText} currentUser={user} />
            }
        </UserContext.Consumer>
    );
}

export default UserSettingsPageTable;