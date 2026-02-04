import _ from "lodash";
import { IUserProjectsModel } from "../../../../BrandVueApi"
import { GetRoleDisplayNameFromName } from "./RoleHelpers"

export const userContainsSearchText = (user: IUserProjectsModel, searchText: string) => {
    const trimmedText = searchText.trim();
    return doesContainSearchText(`${user.firstName} ${user.lastName}`, trimmedText) ||
        doesContainSearchText(user.email, trimmedText) ||
        doesContainSearchText(GetRoleDisplayNameFromName(user.roleName), trimmedText) ||
        doesContainSearchText(user.organisationName, trimmedText);
}

export const doesContainSearchText = (str: string, searchText: string) => {
    return str.toLocaleLowerCase().includes(searchText.toLocaleLowerCase())
}

export const getOrganisationNamesFromUserList = (users: IUserProjectsModel[]): string[] => {
    const usersGroupedByOrg = _.groupBy(users, user => user.organisationId);
    const organisationIds = Object.keys(usersGroupedByOrg);
    return organisationIds.map(id => usersGroupedByOrg[id][0].organisationName);
}