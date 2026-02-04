import React from "react";
import { Modal, ModalBody } from "reactstrap";
import { SavedBreakCombination } from "../../../BrandVueApi";
import DeleteModal from "../../DeleteModal";
import { useSavedBreaksContext } from "../Crosstab/SavedBreaksContext";

interface IProps {
    isOpen: boolean;
    savedBreaksToDelete: SavedBreakCombination | undefined;
    closeModal: (deleted: boolean) => void;
}

const DeleteSavedBreaksModal = (props: IProps) => {
    const { savedBreaksDispatch } = useSavedBreaksContext();

    const deleteSavedBreaks = () => {
        savedBreaksDispatch({type: "DELETE_SAVE_BREAKS", data: {savedBreaksId: props.savedBreaksToDelete!.id}})
            .then(() => props.closeModal(true));
    }

    if (props.savedBreaksToDelete == null) {
        return (
            <Modal isOpen={props.isOpen} centered={true} className="delete-saved-breaks-modal">
                <h3>Delete saved breaks?</h3>
                <ModalBody>
                    <div>Error - could not find matching saved breaks</div>
                </ModalBody>
            </Modal>
        );
    }

    return (
        <DeleteModal
            isOpen={props.isOpen}
            thingToBeDeletedName={props.savedBreaksToDelete.name}
            thingToBeDeletedType="saved breaks"
            delete={deleteSavedBreaks}
            closeModal={props.closeModal}
            affectAllUsers={props.savedBreaksToDelete.isShared}
            delayClick={props.savedBreaksToDelete.isShared}
        />
    );
}
export default DeleteSavedBreaksModal;