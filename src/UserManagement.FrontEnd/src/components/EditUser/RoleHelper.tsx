import { IUserContext } from "../../orval/api/models/userContext";
import { User } from "../../orval/api/models/user";
import { Roles } from "../../orval/api/models/roles";

export function removeRolesNotAvailable(roles: Roles[], currentUser: User, userContext: IUserContext) {
    let filteredRoles = [...roles];
    if (currentUser && userContext) {
        
        if (currentUser?.ownerCompanyDisplayName !== "Savanta") {
            filteredRoles = filteredRoles.filter(role => role.roleName !== "SystemAdministrator");
        }
        if (!userContext.isAdministrator) {
            filteredRoles = filteredRoles.filter(role => role.roleName !== "Administrator");
        }
    }
    return filteredRoles.sort((a, b) => a.roleName.localeCompare(b.roleName));
}
