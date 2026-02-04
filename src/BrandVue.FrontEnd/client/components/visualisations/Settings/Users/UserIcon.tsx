import React from 'react';
import { Roles } from './RoleHelpers';

interface IUserIconProps {
    firstName: string;
    lastName: string;
    role: string;
}

const UserIcon = (props: IUserIconProps) => {

    const initials = (props.firstName?.charAt(0)?.toLocaleUpperCase() ?? "") + (props.lastName?.charAt(0)?.toLocaleUpperCase() ?? "");

    const getClassNameFromRole = (): string => {
        switch (props.role) {
            case Roles.SystemAdministrator:
                return "system-administrator";
            case Roles.Administrator:
                return "administrator";
            case Roles.User:
                return "user";
            case Roles.ReportViewer:
                return "report-viewer";
            case Roles.TrialUser:
                return "trial-user";
            default:
                return "";
        }
    }

    return (
        <div className={`user-icon ${getClassNameFromRole()}`}>
            {initials}
        </div>
    );
}

export default UserIcon;