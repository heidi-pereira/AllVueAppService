import React from "react";
import style from "./WeightingUploadErrorModal.module.less"
import { Modal, ModalHeader } from 'reactstrap';
import { ModalBody } from 'react-bootstrap';

interface IWeightingUploadErrorModalProps {
    isOpen: boolean;
    setIsOpen(isOpen: boolean): void;
    toggle: () => void;
    errorMessage: string
}

const WeightingUploadErrorModal = (props: IWeightingUploadErrorModalProps) => {

    const closeModal = () => {
        props.setIsOpen(false);
    }

    return (
        <Modal isOpen={props.isOpen} modalTransition={{ timeout: 50 }} toggle={props.toggle} className="variable-content-modal modal-dialog-centered content-modal settings-create">
            <ModalHeader style={{ width: "100%" }}>
                <div className="settings-modal-header">
                    <div className="close-icon">
                        <button type="button" className="btn btn-close" onClick={closeModal}>
                        </button>
                    </div>
                    <div className="set-name">Upload error</div>
                </div>
            </ModalHeader>
            <ModalBody>
                <span className={style.errorContainer}>
                    <i className="material-symbols-outlined">warning</i>
                    <div className={style.errorMessage}>{props.errorMessage}</div>
                </span>
                <div className="modal-buttons">
                    <button className="modal-button primary-button" onClick={closeModal}>Close</button>
                </div>
            </ModalBody>
        </Modal>
    )
}

export default WeightingUploadErrorModal;