import React from "react";
import { Modal, ModalHeader, ModalBody } from "reactstrap";
import { IWaveErrorMessage } from "../Controls/WeightingWaveValidationControl";
import style from "./WeightingValidationMessageListModal.module.less";
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from "../Controls/MaterialSymbol";

interface IWeightingValidationMessageListModal {
    isVisible: boolean;
    toggle: () => void;
    header: string;
    messages: IWaveErrorMessage[];
}

const WeightingValidationMessageListModal = (props: IWeightingValidationMessageListModal) => {
    const messageItem = (message: IWaveErrorMessage, index: number) => {
        const icon = message.isWarning ? "warning" : "error";
        const iconColorStyle = message.isWarning ? style.warning : style.error;
        const messageText = message.isWarning ? <>{message.errorMessage}</> : <strong>{message.errorMessage}</strong>;

        return (
            <div className={style.messageItem} key={index}>
                <MaterialSymbol symbolType={MaterialSymbolType[icon]} symbolStyle={MaterialSymbolStyle["outlined"]} noFill={!message.isWarning} className={`${style.symbol} ${iconColorStyle}`} />
                {messageText}
            </div>
        )
    }

    const messageList = (messages: IWaveErrorMessage[]) => {
        return (
            <div className={style.messageListContainer}>
                {messages.map((m, i) => messageItem(m, i))}
            </div>
        )
    }

    return (
        <Modal isOpen={props.isVisible} toggle={props.toggle} modalTransition={{ timeout: 50 }} className={`variable-content-modal modal-dialog-centered content-modal settings-create ${style.modal}`}>
            <ModalHeader style={{ width: "100%" }}>
                <div className={`settings-modal-header ${style.header}`}>
                    <div className="close-icon">
                        <button type="button" className="close" onClick={props.toggle}>
                            <i className={`${style.symbol} material-symbols-outlined`}>close</i>
                        </button>
                    </div>
                    <div className={`set-name ${style.headerText}`}>{props.header}</div>
                </div>
            </ModalHeader>
            <ModalBody>
                {messageList(props.messages)}
            </ModalBody>
            <ModalBody>
                <div className="modal-buttons">
                    <button className={`modal-button secondary-button ${style.button}`} onClick={props.toggle}>Close</button>
                </div>
            </ModalBody>
        </Modal>
    );
}

export default WeightingValidationMessageListModal;