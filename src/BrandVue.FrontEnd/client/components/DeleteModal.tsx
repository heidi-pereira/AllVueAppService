import React from "react";
import { Modal, ModalBody } from "reactstrap";
import Throbber from "./throbber/Throbber";

interface IProps {
    isOpen: boolean;
    thingToBeDeletedName: string;
    thingToBeDeletedType: string;
    delete(): void;
    closeModal: (deleted: boolean) => void;
    affectAllUsers?: boolean;
    delayClick?: boolean;
}

const DeleteModal = (props: IProps) => {
    const [hasBeenClicked, setHasBeenClicked] = React.useState(false);

    const getDeleteContent = () => {
        return (
            <>
                <p className="text">Are you sure you want to delete <span className="name">{props.thingToBeDeletedName}</span>?</p>
                {props.affectAllUsers &&
                    <p className="text delete-warning">
                        This will affect all users of this project and can't be undone
                    </p>
                }
            </>
        )
    }

    const getThrobber = () => {
        return(
            <div className="throbber">
                <Throbber />
            </div>
        )
    }

    return (
        <Modal isOpen={props.isOpen} centered={true} className="delete-modal" autoFocus={false}>
            <h3>Delete {props.thingToBeDeletedType}?</h3>
            <ModalBody>
                {hasBeenClicked ? getThrobber() : getDeleteContent()}
                <div className="button-container">
                    <button onClick={() => { setHasBeenClicked(false); props.closeModal(false) }} className="secondary-button" autoFocus={true}>Cancel</button>
                    <button disabled={hasBeenClicked}  onClick={() => { setHasBeenClicked(true); props.delete(); }} className={`negative-button ${props.delayClick ? "delay-click" : ""}`}>Delete {props.thingToBeDeletedType}</button>
                </div>
            </ModalBody>
        </Modal>
    );
}
export default DeleteModal;