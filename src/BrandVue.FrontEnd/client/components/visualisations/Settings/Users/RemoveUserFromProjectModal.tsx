import React from "react";
import {IApplicationUser, SwaggerException, UserProjectsModel} from "../../../../BrandVueApi";
import {useUserStateContext} from "./UserStateContext";
import toast from "react-hot-toast";
import MultiPageModal from "../../../MultiPageModal";
import ModalPage from "../../../ModalPage";

interface IRemoveUserFromProjectModalProps {
    isOpen: boolean
    setIsOpen: (isOpen: boolean) => void
    selectedUser: UserProjectsModel
    currentUser?: IApplicationUser | null
}

const RemoveUserFromProjectModal = (props: IRemoveUserFromProjectModalProps) => {
    const isCurrentUserSelected = props.selectedUser.applicationUserId === props.currentUser?.userId
    const header = isCurrentUserSelected ? "Leave project?" : "Remove user?"
    const actionButtonText = isCurrentUserSelected ? "Leave" : "Remove"
    const infoText = isCurrentUserSelected ? <>Are you sure you want to leave?</> : <>Are you sure you want to remove <strong>{props.selectedUser.firstName} {props.selectedUser.lastName} ({props.selectedUser.email})</strong>?</>
    const warningText = isCurrentUserSelected ? "You will no longer be able to access the project" : "This user will no longer be able to access the project. Their user account won't be deleted."

    const { userDispatch } = useUserStateContext();

    const removeUserHandler = () => {
        toast.promise(userDispatch({type: 'REMOVE_USER_PROJECTS', data: {userId: props.selectedUser.applicationUserId}}), {
            loading: "Removing user...",
            success: () => {
                props.setIsOpen(false);
                return "User removed";
            },
            error: (error) => {
                if (error && SwaggerException.isSwaggerException(error)) {
                    const swaggerException = error as SwaggerException;
                    const responseJson = JSON.parse(swaggerException.response);
                    return responseJson.message;
                }
                return `An error occurred while trying to remove user: ${props.selectedUser.firstName} ${props.selectedUser.lastName}`;
            }
        });
    }

    return (
        <MultiPageModal isOpen = {props.isOpen}
                        setIsOpen = {props.setIsOpen}
                        header = {header} >
            <ModalPage className = "user-modal-size"
                       actionButtonCss = "negative-button delay-click"
                       actionButtonText = {actionButtonText}
                       actionButtonHandler={removeUserHandler}>
                <p className = "text">{infoText}</p>
                <p className = "text warning">{warningText}</p>
            </ModalPage>
        </MultiPageModal>
    )
}

export default RemoveUserFromProjectModal