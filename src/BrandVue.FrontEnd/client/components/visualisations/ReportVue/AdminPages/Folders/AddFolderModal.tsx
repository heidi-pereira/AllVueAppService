import React from "react";
import toast from "react-hot-toast";
import { Modal, ModalFooter, ModalHeader, ModalBody, Button } from "reactstrap";


interface IProps {
    path: string;
    isOpen: boolean;
    closeModal: () => void;
    createFolder: (folder: string) => void;
}

const AddFolderModal = (props: IProps) => {
    const [folderToCreate, setFolderToCreate] = React.useState<string>("");

    React.useEffect(() => {
        if (props.isOpen) {
            setFolderToCreate("");
        }
    }, [props.isOpen]);

    const saveBreaks = () => {
        if (folderToCreate.trim() == "") {
            toast.error("Folder to create cannot be an empty name.");
        } else {
            props.createFolder(folderToCreate);
        }
    }


    const fullName = props.path.endsWith("\\") ? props.path + folderToCreate : props.path + "\\" + folderToCreate;

    return (<Modal isOpen={props.isOpen} toggle={() => props.closeModal()} centered={true} className={`modal-dialog-centered content-modal modal-copy add-folder-modal`}>
        <ModalHeader>
            <div className="settings-modal-header">
                <div className="close-icon">
                    <button type="button" className="btn btn-close" onClick={() => props.closeModal()}>
                    </button>
                </div>
                <div className="set-name">Create folder</div>
            </div>
        </ModalHeader>
        <ModalBody >

            <div className="input-container">
                <label htmlFor="folder-name-input">Name:</label>
                <input className="folder-name-input"
                    id="folder-name-input"
                    type="text"
                    autoFocus={true}
                    autoComplete="off"
                    value={folderToCreate}
                    onChange={(e) => setFolderToCreate(e.target.value)} />
            </div>
            <div>Are you sure you want to create <strong className="filename">{fullName} ?</strong></div>

        </ModalBody>
        <ModalFooter>
            <Button className="secondary-button" onClick={() => props.closeModal()}>Cancel</Button>
            <Button className="primary-button destructive" onClick={() => saveBreaks()} >Create</Button>
        </ModalFooter>
    </Modal>
    );

}
export default AddFolderModal;
