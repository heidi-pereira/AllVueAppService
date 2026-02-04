import React from "react";
import { Modal, ModalBody } from "reactstrap";

interface IProps {
    isOpen: boolean;
    description: string;
    title: string;
    update(): void;
    closeModal: (deleted: boolean) => void;
    affectAllUsers?: boolean;
    delayClick?: boolean;
}

const UpdateModal = (props: IProps) => {
    return (
        <Modal isOpen={props.isOpen} centered={true} className="delete-modal" autoFocus={false}>
            <h3>Update {props.title}?</h3>
            <ModalBody>
                <p className="text">Are you sure you want to update the <span className="name">{props.title}</span>?</p>
                <p className="text">{props.description}</p>
                {props.affectAllUsers &&
                    <p className="text delete-warning">
                        This will affect all users of this project and can't be undone
                    </p>
                }
                <div className="button-container">
                    <button onClick={() => props.closeModal(false)} className="secondary-button" autoFocus={true}>Cancel</button>
                    <button onClick={props.update} className={`negative-button ${props.delayClick ? "delay-click" : ""}`}>Update {props.title}</button>
                </div>
            </ModalBody>
        </Modal>
    );
}
export default UpdateModal;