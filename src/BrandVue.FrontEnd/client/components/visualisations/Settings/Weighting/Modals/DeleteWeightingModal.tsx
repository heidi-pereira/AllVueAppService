import React from "react";
import { Modal, ModalHeader, ModalBody, ModalFooter, Button } from "reactstrap";
import { WaveDescription } from "../WeightingWaveListItem";
import style from './DeleteWeightingModal.module.less'

interface IDeleteWeightingModalProps {
    isOpen: boolean;
    toggle: () => void;
    cancelDelete: () => void;
    deleteWeighting: () => void;
    wave?: WaveDescription;
    segmentName?: string;
}

const DeleteWeightingModal = (props: IDeleteWeightingModalProps) => {
    const getHeader = (headerText: string, onClickExit: () => void) => {
        return (
            <div className="settings-modal-header">
                <div className="close-icon">
                    <button type="button" className="btn btn-close" onClick={onClickExit}>                      
                    </button>
                </div>
                <div className="set-name">{headerText}</div>
            </div>
        );
    }

    const getBodyContent = () => {
        if (props.wave) {
            return (
                <>
                    <p>Are you sure you want to delete this weighting for <strong>{props.wave.InstanceName}</strong>?</p>
                    <div className={style.redText}>This will affect all users of this project and can't be undone</div>
                </>
            )
        }

        return (
            <>
                <p>Are you sure you want to delete weighting from <strong>{props.segmentName}</strong>?</p>
                <div className={style.redText}>This will affect all users of this project and can't be undone</div>
            </>
        );
    }

    return (
        <Modal isOpen={props.isOpen} toggle={props.toggle} centered={true} className="modal-delete content-modal settings-delete">
            <ModalHeader>
                {getHeader("Delete weighting?", props.toggle)}
            </ModalHeader>
            <ModalBody>
                {getBodyContent()}
            </ModalBody>
            <ModalFooter>
                <Button className="secondary-button" onClick={props.cancelDelete}>Cancel</Button>
                <button className="negative-button delay-click" onClick={props.deleteWeighting}>Delete weighting</button>
            </ModalFooter>
        </Modal>
    );
}

export default DeleteWeightingModal;