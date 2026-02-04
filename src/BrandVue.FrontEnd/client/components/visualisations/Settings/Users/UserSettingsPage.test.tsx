import  React from "react";
import {render, screen, waitForElementToBeRemoved, within} from "@testing-library/react";
import "@testing-library/jest-dom";
import {UserContextState, UserStateContext} from "./UserStateContext";
import {IApplicationUser, UserProjectsModel} from "../../../../BrandVueApi";
import {UserContext} from "../../../../GlobalContext";
import {UsersSettingsPage} from "./UsersSettingsPage";
import {GetRoleDisplayNameFromName, Roles} from "./RoleHelpers";
import { AriaRoles } from "../../../../helpers/ReactTestingLibraryHelpers";
import userEvent from "@testing-library/user-event"
import { ProductConfiguration } from "../../../../ProductConfiguration";

const THROBBER_ARIA = "Content is loading spinner"
const SEARCHBAR_USERS_IN_PROJECT_ARIA = "Users in project"
const SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA = "Users able to be added to project"
const ADD_SPECIFIC_USERS = "Add specific users"
const ADD_ALL_EXISTING_USERS = "Share to all external users"
const ADD_USERS = "Add users"
const REMOVE_USERS_HEADER = 'Remove user?'
const LEAVE_HEADER = 'Leave project?'
const LEAVE = 'Leave'
const REMOVE = 'Remove'
const CANCEL = 'Cancel'
const CROSS = 'Close'
const REMOVE_DISPATCH = 'REMOVE_USER_PROJECTS'
const ADD_DISPATCH = 'ADD_USER_PROJECTS'
const DESELECT = 'Deselect user'

const USER_ONE: UserProjectsModel = new UserProjectsModel({
    applicationUserId: "user-1",
    firstName: "Aaron",
    lastName: "Faulkner",
    email: "test@gmail.com",
    roleName: Roles.Administrator,
    organisationId: "org-1",
    organisationName: "Savanta",
    verified: true,
    lastLogin: undefined,
    projects: [],
    isOrganisationExternalLogin: true,
    products:[],
});
const USER_TWO: UserProjectsModel = new UserProjectsModel({
    applicationUserId: "user-2",
    firstName: "James",
    lastName: "Rodden",
    email: "test@hotmail.com",
    roleName: Roles.User,
    organisationId: "org-2",
    organisationName: "not-Savanta",
    verified: true,
    lastLogin: undefined,
    projects: [],
    isOrganisationExternalLogin: false,
    products: [],
});
const USER_THREE: UserProjectsModel = new UserProjectsModel({
    applicationUserId: "user-3",
    firstName: "James",
    lastName: "Hand",
    email: "test@icloud.com",
    roleName: Roles.User,
    organisationId: "org-2",
    organisationName: "not-Savanta",
    verified: true,
    lastLogin: undefined,
    projects: [],
    isOrganisationExternalLogin: false,
    products: [],
});


// helper function to load UserSettingsPage with mocked user context
const renderUserSettingsPageWithUserData = (userData: Partial<UserContextState> = {}) => {
    const currentUser: Partial<IApplicationUser> = {userId: USER_ONE.applicationUserId}
    const defaultUserContext: UserContextState = {
        projectCompany: undefined,
        activeUsers: [],
        inactiveUsers: [],
        isLoading: false,
        hasMultipleOrganisations: false,
        isSharedToAllUsers: false,
        userDispatch: () => Promise.resolve()
    }
    const productConfiguration = new ProductConfiguration();
    productConfiguration.getManageUsersUrl = (shortCode?: string) => shortCode ? shortCode + "test_url" : "test_url";

    render(
        <UserContext.Provider value={currentUser as IApplicationUser}>
            <UserStateContext.Provider value={{...defaultUserContext, ...userData}}>
                <UsersSettingsPage productConfiguration={productConfiguration}/>
            </UserStateContext.Provider>
        </UserContext.Provider>
    )
}

describe("User permissions page rendering correctly when loading data", () => {
    beforeEach(() => {
        renderUserSettingsPageWithUserData({isLoading: true})
    });

    it("Check is loading throbber is showing when loading data", () => {
        const throbberElement = screen.queryByLabelText(THROBBER_ARIA);
        expect(throbberElement).toBeVisible();
    });

    it("Check add all users is hidden when loading data", () => {
        const buttonElement = screen.queryByRole(AriaRoles.BUTTON, {name: ADD_ALL_EXISTING_USERS});
        expect(buttonElement).toBeNull();
    });

    it("Check add specific users is hidden when loading data", () => {
        const buttonElement = screen.queryByRole(AriaRoles.BUTTON, {name: ADD_SPECIFIC_USERS});
        expect(buttonElement).toBeNull();
    });

    it("Check user table is hidden when loading data", () => {
        const tableElement = screen.queryByRole(AriaRoles.TABLE)
        expect(tableElement).toBeNull();
    });

    it("Check search bar is hidden",  () => {
        const searchBarElement = screen.queryByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_IN_PROJECT_ARIA})
        expect(searchBarElement).toBeNull();
    });

    it("Check add user button is hidden when loading data", () => {
        const allButtons = screen.queryAllByRole(AriaRoles.BUTTON)
        const buttonElement = allButtons.find(button => within(button).queryByText(ADD_USERS) != null) ?? null
        expect(buttonElement).toBeNull();
    });

    it("Check add user modal is hidden when loading data", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});
        expect(titleElement).toBeNull();
    });

    it("Check remove user modal is hidden when loading data", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});
        expect(titleElement).toBeNull();
    });

    it("Check leave user modal is hidden when loading data", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: LEAVE_HEADER});
        expect(titleElement).toBeNull();
    });
});


describe("User permissions page rendering correctly when there is no active users", () => {
    beforeEach(() => {
        renderUserSettingsPageWithUserData({})
    });

    it("Check Add all existing users button is showing",  () => {
        const buttonElement = screen.queryByRole(AriaRoles.BUTTON, {name: ADD_ALL_EXISTING_USERS});
        expect(buttonElement).toBeVisible();
    });

    it("Check Add specific users button is showing",  () => {
        const buttonElement = screen.queryByRole(AriaRoles.BUTTON, {name: ADD_SPECIFIC_USERS});
        expect(buttonElement).toBeVisible();
    });

    it("Check is loading throbber is hidden", () => {
        const throbberElement = screen.queryByLabelText(THROBBER_ARIA);
        expect(throbberElement).toBeNull();
    });

    it("Check user table is hidden", () => {
        const tableElement = screen.queryByRole(AriaRoles.TABLE)
        expect(tableElement).toBeNull();
    });

    it("Check search bar is hidden",  () => {
        const searchBarElement = screen.queryByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_IN_PROJECT_ARIA})
        expect(searchBarElement).toBeNull();
    });

    it("Check add user button is hidden", () => {
        const allButtons = screen.queryAllByRole(AriaRoles.BUTTON)
        const buttonElement = allButtons.find(button => within(button).queryByText(ADD_USERS) != null) ?? null
        expect(buttonElement).toBeNull();
    });

    it("Check add user modal is hidden", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});
        expect(titleElement).toBeNull();
    });

    it("Check remove user modal is hidden", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});
        expect(titleElement).toBeNull();
    });

    it("Check leave user modal is hidden", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: LEAVE_HEADER});
        expect(titleElement).toBeNull();
    });

    it("Check add user modal shows when add specific user button pressed",  async () => {
        const user = userEvent.setup()

        await user.click(screen.getByRole(AriaRoles.BUTTON, {name: ADD_SPECIFIC_USERS}))
        await screen.findByRole(AriaRoles.HEADER, {name: ADD_USERS})

        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});
        expect(titleElement).toBeVisible();
    });
});


describe("User permissions page rendering correctly when there is active users", () => {
    beforeEach(() => {
        renderUserSettingsPageWithUserData({activeUsers: [USER_ONE, USER_TWO]})
    });

    it("Check table is shown",  () => {
        const tableElement = screen.queryByRole(AriaRoles.TABLE)
        expect(tableElement).toBeVisible();
    });

    it("Check name is shown of each expected user is shown in table",  () => {
        const tableCellElements = screen.queryAllByRole(AriaRoles.TABLE_CELL)
        expect(tableCellElements).toBeTruthy()

        const userOneElement = tableCellElements.find(cellElement => within(cellElement).queryByText(`${USER_ONE.firstName} ${USER_ONE.lastName} (you)`))
        const userTwoElement = tableCellElements.find(cellElement => within(cellElement).queryByText(`${USER_TWO.firstName} ${USER_TWO.lastName}`))
        const userThreeElement = tableCellElements.find(cellElement => within(cellElement).queryByText(`${USER_THREE.firstName} ${USER_THREE.lastName}`))

        expect(userOneElement).toBeVisible()
        expect(userTwoElement).toBeVisible()
        expect(userThreeElement).toBeFalsy()
    });

    it("Check email is shown of each expected user is shown in table",  () => {
        const tableCellElements = screen.queryAllByRole(AriaRoles.TABLE_CELL)
        expect(tableCellElements).toBeTruthy()

        const userOneElement = tableCellElements.find(cellElement => within(cellElement).queryByText(USER_ONE.email))
        const userTwoElement = tableCellElements.find(cellElement => within(cellElement).queryByText(USER_TWO.email))
        const userThreeElement = tableCellElements.find(cellElement => within(cellElement).queryByText(USER_THREE.email))

        expect(userOneElement).toBeVisible()
        expect(userTwoElement).toBeVisible()
        expect(userThreeElement).toBeFalsy()
    });

    it("Check role is shown of each expected user is shown in table",  () => {
        const tableCellElements = screen.queryAllByRole(AriaRoles.TABLE_CELL)
        expect(tableCellElements).toBeTruthy()

        const userOneElement = tableCellElements.find(cellElement => within(cellElement).queryByText(GetRoleDisplayNameFromName(USER_ONE.roleName)))
        const userTwoElement = tableCellElements.find(cellElement => within(cellElement).queryByText(GetRoleDisplayNameFromName(USER_TWO.roleName)))
        const numberOfUserCells = tableCellElements.filter(cellElement => within(cellElement).queryByText(GetRoleDisplayNameFromName(USER_TWO.roleName)) != null)

        expect(userOneElement).toBeVisible()
        expect(userTwoElement).toBeVisible()
        expect(numberOfUserCells.length).toEqual(1)
    });

    it("Check search bar is shown",  () => {
        const searchBarElement = screen.queryByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_IN_PROJECT_ARIA})
        expect(searchBarElement).toBeVisible();
    });

    it("Check add users is shown",  () => {
        const allButtons = screen.queryAllByRole(AriaRoles.BUTTON)
        const buttonElement = allButtons.find(button => within(button).queryByText(ADD_USERS) != null) ?? null
        expect(buttonElement).toBeVisible();
    });

    it("Check is loading throbber is hidden", () => {
        const throbberElement = screen.queryByLabelText(THROBBER_ARIA);
        expect(throbberElement).toBeNull();
    });

    it("Check Add all existing users button is hidden",  () => {
        const buttonElement = screen.queryByRole(AriaRoles.BUTTON, {name: ADD_ALL_EXISTING_USERS});
        expect(buttonElement).toBeNull();
    });

    it("Check Add specific users button is hidden",  () => {
        const buttonElement = screen.queryByRole(AriaRoles.BUTTON, {name: ADD_SPECIFIC_USERS});
        expect(buttonElement).toBeNull();
    });

    it("Check add user modal is hidden", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});
        expect(titleElement).toBeNull();
    });

    it("Check remove user modal is hidden", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});
        expect(titleElement).toBeNull();
    });

    it("Check leave modal is hidden", () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: LEAVE_HEADER});
        expect(titleElement).toBeNull();
    });

    it("Check add user modal shows when add user button pressed",  async () => {
        const user = userEvent.setup()

        const allButtons = screen.queryAllByRole(AriaRoles.BUTTON)
        const buttonElement = allButtons.find(button => within(button).queryByText(ADD_USERS) != null) ?? null
        await user.click(buttonElement!)
        await screen.findByRole(AriaRoles.HEADER, {name: ADD_USERS})

        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});
        expect(titleElement).toBeVisible();
    });

    it("Check Leave modal shows leave button pressed",  async () => {
        const user = userEvent.setup()

        await user.click(screen.getByRole(AriaRoles.BUTTON, {name: LEAVE}))
        await screen.findByRole(AriaRoles.HEADER, {name: LEAVE_HEADER})

        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: LEAVE_HEADER});
        expect(titleElement).toBeVisible();
    });

    it("Check remove user modal shows when remove button pressed",  async () => {
        const user = userEvent.setup()

        await user.click(screen.getAllByRole(AriaRoles.BUTTON, {name: REMOVE})[0])
        await screen.findByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER})

        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});
        expect(titleElement).toBeVisible();
    });
});


describe("Leave modal renders and behaves correctly", () => {
    const dispatchMock = jest.fn()
    const user = userEvent.setup()

    beforeEach(async () => {
        dispatchMock.mockClear()
        dispatchMock.mockReturnValue(Promise.resolve())
        renderUserSettingsPageWithUserData({activeUsers: [USER_ONE, USER_TWO, USER_THREE], userDispatch: dispatchMock})
        await user.click(screen.getByRole(AriaRoles.BUTTON, {name: LEAVE}))
        await screen.findByRole(AriaRoles.HEADER, {name: LEAVE_HEADER})
    });

    it("Has correct title",  async () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: LEAVE_HEADER});
        expect(titleElement).toBeVisible();
    });

    it("Has cancel button",  async () => {
        const titleElement = screen.queryByRole(AriaRoles.BUTTON, {name: CANCEL});
        expect(titleElement).toBeVisible();
    });

    it("Has leave button",  async () => {
        const modalElement = screen.getByRole(AriaRoles.DOCUMENT)
        const buttonElement = within(modalElement).queryByRole(AriaRoles.BUTTON, {name: LEAVE});
        expect(buttonElement).toBeVisible();
    });

    it("Cancel button will hide modal",  async () => {
        let titleElement = screen.queryByRole(AriaRoles.HEADER, {name: LEAVE_HEADER});
        await user.click(screen.getByRole(AriaRoles.BUTTON, {name: CANCEL}))

        await waitForElementToBeRemoved(titleElement);
        titleElement = screen.queryByRole(AriaRoles.HEADER, {name: LEAVE_HEADER});

        expect(titleElement).toBeNull();
    });

    it("Cross button will hide modal",  async () => {
        let titleElement = screen.queryByRole(AriaRoles.HEADER, {name: 'Leave project?'});
        await user.click(screen.getByRole(AriaRoles.BUTTON, {description: CROSS}))

        await waitForElementToBeRemoved(titleElement);
        titleElement = screen.queryByRole(AriaRoles.HEADER, {name: LEAVE_HEADER});

        expect(titleElement).toBeNull();
    });

    it("Leave button will send correct user id to remove to the dispatch function", async () => {
        const modalElement = screen.getByRole(AriaRoles.DOCUMENT)
        const buttonElement = within(modalElement).getByRole(AriaRoles.BUTTON, {name: LEAVE});

        await user.click(buttonElement)

        expect(dispatchMock).toBeCalledTimes(1)
        expect(dispatchMock).toBeCalledWith({type: REMOVE_DISPATCH, data: { userId: USER_ONE.applicationUserId }})
    });
});


describe("Remove modal renders and behaves correctly", () => {
    const dispatchMock = jest.fn()
    const user = userEvent.setup()

    beforeEach(async () => {
        dispatchMock.mockClear()
        dispatchMock.mockReturnValue(Promise.resolve())
        renderUserSettingsPageWithUserData({activeUsers: [USER_ONE, USER_TWO, USER_THREE], userDispatch: dispatchMock})
        await user.click(screen.getAllByRole(AriaRoles.BUTTON, {name: REMOVE})[0])
        await screen.findByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER})
    });

    it("Has correct title",  async () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});
        expect(titleElement).toBeVisible();
    });


    it("Has cancel button",  async () => {
        const titleElement = screen.queryByRole(AriaRoles.BUTTON, {name: CANCEL});
        expect(titleElement).toBeVisible();
    });

    it("Has leave button",  async () => {
        const modalElement = screen.getByRole(AriaRoles.DOCUMENT)
        const buttonElement = within(modalElement).queryByRole(AriaRoles.BUTTON, {name: REMOVE});
        expect(buttonElement).toBeVisible();
    });

    it("Shows users name",  async () => {
        const modalElement = screen.getByRole(AriaRoles.DOCUMENT)
        const textElement = within(modalElement).queryByText(`${USER_TWO.firstName} ${USER_TWO.lastName}`, {exact: false});
        expect(textElement).toBeVisible();
    });

    it("Shows users email",  async () => {
        const modalElement = screen.getByRole(AriaRoles.DOCUMENT)
        const textElement = within(modalElement).queryByText(USER_TWO.email, {exact: false});
        expect(textElement).toBeVisible();
    });

    it("Cancel button will hide modal",  async () => {
        let titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});
        await user.click(screen.getByRole(AriaRoles.BUTTON, {name: CANCEL}))

        await waitForElementToBeRemoved(titleElement);
        titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});

        expect(titleElement).toBeNull();
    });

    it("Cross button will hide modal",  async () => {
        let titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});
        await user.click(screen.getByRole(AriaRoles.BUTTON, {description: CROSS}))

        await waitForElementToBeRemoved(titleElement);
        titleElement = screen.queryByRole(AriaRoles.HEADER, {name: REMOVE_USERS_HEADER});

        expect(titleElement).toBeNull();
    });

    it("Remove button will send correct user id to remove to the dispatch function", async () => {
        const modalElement = screen.getByRole(AriaRoles.DOCUMENT)
        const buttonElement = within(modalElement).getByRole(AriaRoles.BUTTON, {name: REMOVE});

        await user.click(buttonElement)

        expect(dispatchMock).toBeCalledTimes(1)
        expect(dispatchMock).toBeCalledWith({type: REMOVE_DISPATCH, data: { userId: USER_TWO.applicationUserId }})
    });
});


describe("Add user modal renders and behaves correctly", () => {
    const dispatchMock = jest.fn()
    const user = userEvent.setup()

    beforeEach(async () => {
        dispatchMock.mockClear()
        dispatchMock.mockReturnValue(Promise.resolve())
        renderUserSettingsPageWithUserData({inactiveUsers: [USER_ONE, USER_TWO, USER_THREE], userDispatch: dispatchMock})
        await user.click(screen.getByRole(AriaRoles.BUTTON, {name: ADD_SPECIFIC_USERS}))
        await screen.findByRole(AriaRoles.HEADER, {name: ADD_USERS})
    });

    it("Has correct title",  async () => {
        const titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});
        expect(titleElement).toBeVisible();
    });

    it("Has cancel button",  async () => {
        const buttonElement = screen.queryByRole(AriaRoles.BUTTON, {name: CANCEL});
        expect(buttonElement).toBeVisible();
    });

    it("Has Add user button",  async () => {
        const buttonElement = screen.queryByRole(AriaRoles.BUTTON, {name: ADD_USERS});
        expect(buttonElement).toBeVisible();
    });

    it("Has Search bar",  async () => {
        const searchBarElement = screen.queryByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA})
        expect(searchBarElement).toBeVisible();
    });

    it("User dropdown menu hidden, when nothing searched",  async () => {
        const menu = screen.queryByRole(AriaRoles.MENU)
        expect(menu).toBeNull();
    });

    it("List of users to add is empty when modal first opened",  async () => {
        const addUsersList = screen.getByRole(AriaRoles.LIST)

        const userOneElement = within(addUsersList).queryByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})
        const userTwoElement = within(addUsersList).queryByText(`${USER_TWO.firstName} ${USER_TWO.lastName}`, {exact: false})
        const userThreeElement = within(addUsersList).queryByText(`${USER_THREE.firstName} ${USER_THREE.lastName}`, {exact: false})

        expect(userOneElement).toBeNull()
        expect(userTwoElement).toBeNull()
        expect(userThreeElement).toBeNull()
    });

    it("Cancel button will hide modal",  async () => {
        let titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});
        await user.click(screen.getByRole(AriaRoles.BUTTON, {name: CANCEL}))

        await waitForElementToBeRemoved(titleElement);
        titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});

        expect(titleElement).toBeNull();
    });

    it("Cross button will hide modal",  async () => {
        let titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});
        await user.click(screen.getByRole(AriaRoles.BUTTON, {description: CROSS}))

        await waitForElementToBeRemoved(titleElement);
        titleElement = screen.queryByRole(AriaRoles.HEADER, {name: ADD_USERS});

        expect(titleElement).toBeNull();
    });

    it("Searching by name shows correct users in drop down",  async () => {
        const searchBarElement = screen.getByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA})

        await user.type(searchBarElement, USER_ONE.firstName)
        const menu = screen.getByRole(AriaRoles.MENU)
        const userOneElement = within(menu).queryByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})
        const userTwoElement = within(menu).queryByText(`${USER_TWO.firstName} ${USER_TWO.lastName}`, {exact: false})
        const userThreeElement = within(menu).queryByText(`${USER_THREE.firstName} ${USER_THREE.lastName}`, {exact: false})

        expect(userOneElement).toBeVisible();
        expect(userTwoElement).toBeNull();
        expect(userThreeElement).toBeNull();
    });

    it("Searching by organisation shows correct users in drop down",  async () => {
        const searchBarElement = screen.getByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA})

        await user.type(searchBarElement, USER_TWO.organisationName)
        const menu = screen.getByRole(AriaRoles.MENU)
        const userOneElement = within(menu).queryByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})
        const userTwoElement = within(menu).queryByText(`${USER_TWO.firstName} ${USER_TWO.lastName}`, {exact: false})
        const userThreeElement = within(menu).queryByText(`${USER_THREE.firstName} ${USER_THREE.lastName}`, {exact: false})

        expect(userOneElement).toBeNull();
        expect(userTwoElement).toBeVisible();
        expect(userThreeElement).toBeVisible();
    });

    it("Searching by email shows correct users in drop down",  async () => {
        const searchBarElement = screen.getByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA})

        await user.type(searchBarElement, "hotmail")
        const menu = screen.getByRole(AriaRoles.MENU)
        const userOneElement = within(menu).queryByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})
        const userTwoElement = within(menu).queryByText(`${USER_TWO.firstName} ${USER_TWO.lastName}`, {exact: false})
        const userThreeElement = within(menu).queryByText(`${USER_THREE.firstName} ${USER_THREE.lastName}`, {exact: false})

        expect(userOneElement).toBeNull();
        expect(userTwoElement).toBeVisible();
        expect(userThreeElement).toBeNull();
    })

    it("Searching by role shows correct users in drop down",  async () => {
        const searchBarElement = screen.getByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA})

        await user.type(searchBarElement, GetRoleDisplayNameFromName(Roles.User))
        const menu = screen.getByRole(AriaRoles.MENU)
        const userOneElement = within(menu).queryByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})
        const userTwoElement = within(menu).queryByText(`${USER_TWO.firstName} ${USER_TWO.lastName}`, {exact: false})
        const userThreeElement = within(menu).queryByText(`${USER_THREE.firstName} ${USER_THREE.lastName}`, {exact: false})

        expect(userOneElement).toBeNull();
        expect(userTwoElement).toBeVisible();
        expect(userThreeElement).toBeVisible();
    })

    it("Adding a user from the dropdown, adds user to list of users to be added",  async () => {
        // search for user
        const searchBarElement = screen.getByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA})
        await user.type(searchBarElement, USER_ONE.firstName)

        // click on user in dropdown menu
        const menu = screen.getByRole(AriaRoles.MENU)
        const userElement = within(menu).getByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})
        await user.click(userElement)

        // check user is in list of selected users
        const addUsersList = screen.getByRole(AriaRoles.LIST)
        const userOneElement = within(addUsersList).queryByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})

        expect(userOneElement).toBeVisible();
    })

    it("Little cross on added users, removes them from the list to be added",  async () => {
        // search for user
        const searchBarElement = screen.getByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA})
        await user.type(searchBarElement, USER_ONE.firstName)

        // click on user in dropdown menu
        const menu = screen.getByRole(AriaRoles.MENU)
        const userElement = within(menu).getByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})
        await user.click(userElement)

        //Get user from selected user list
        const addUsersList = screen.getByRole(AriaRoles.LIST)
        const userElements = within(addUsersList).getAllByRole(AriaRoles.LIST_ITEM)

        // click cross on user
        await user.click(within(userElements[0]).getByRole(AriaRoles.BUTTON, {description: DESELECT}))
        const userOneElement = within(addUsersList).queryByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})

        expect(userOneElement).toBeNull();
    })

    it("Add users button sends the correct user ids to the dispatch function",  async () => {
        const searchBarElement = screen.getByRole(AriaRoles.SEARCHBAR, {name: SEARCHBAR_USERS_CAN_BE_ADDED_TO_PROJECT_ARIA})
        const buttonElement = screen.getByRole(AriaRoles.BUTTON, {name: ADD_USERS})

        // search for user
        await user.type(searchBarElement, USER_ONE.firstName)

        // click on user in dropdown
        let menu = screen.getByRole(AriaRoles.MENU)
        const userOneElement = within(menu).getByText(`${USER_ONE.firstName} ${USER_ONE.lastName}`, {exact: false})
        await user.click(userOneElement)

        // search for another user
        await user.type(searchBarElement, USER_TWO.firstName)

        // click on this user in dropdown
        menu = screen.getByRole(AriaRoles.MENU)
        const userTwoElement = within(menu).getByText(`${USER_TWO.firstName} ${USER_TWO.lastName}`, {exact: false})
        await user.click(userTwoElement)

        // click add user button
        await user.click(buttonElement)

        expect(dispatchMock).toBeCalledTimes(1)
        expect(dispatchMock).toBeCalledWith({type: ADD_DISPATCH, data: { userIds: [USER_ONE.applicationUserId, USER_TWO.applicationUserId] }})
    })
});