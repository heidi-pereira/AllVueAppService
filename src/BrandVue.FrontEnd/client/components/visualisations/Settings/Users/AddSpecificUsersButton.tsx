import React from 'react';
import { useUserStateContext } from './UserStateContext';
import AddUsersModal from './AddUsersModal';
import { ProductConfiguration } from '../../../../ProductConfiguration';

interface IAddSpecificUsersButtonProps {
    productConfiguration: ProductConfiguration;
    children: any;
}

const AddSpecificUsersContainer = (props: IAddSpecificUsersButtonProps) => {

    const { projectCompany, activeUsers, inactiveUsers } = useUserStateContext();
    const hasNoUsers = activeUsers.length + inactiveUsers.length == 0;

    const [isAddUserModalOpen, setIsAddUserModalOpen] = React.useState<boolean>(false);

    const openManageUsersPage = (shortCode: string) => {
        const url = props.productConfiguration.getManageUsersUrl(shortCode);
        window.open(url, "_blank");
    }

    if (hasNoUsers && projectCompany) {
        return (
            <div className='empty-page-no-users-container'>
                No {projectCompany.displayName} users were found.
                <button className='hollow-button' onClick={() => openManageUsersPage(projectCompany.shortCode)}>
                    Manage users
                </button>
            </div>
        );
    }

    return (
        <>
            <div className='empty-page-button-container'>
                {props.children}
                <button className='hollow-button' onClick={() => setIsAddUserModalOpen(true)}>
                    Add specific users
                </button>
            </div>
            <AddUsersModal
                isOpen={isAddUserModalOpen}
                setIsOpen={setIsAddUserModalOpen}
            />
        </>
    );
}

export default AddSpecificUsersContainer;