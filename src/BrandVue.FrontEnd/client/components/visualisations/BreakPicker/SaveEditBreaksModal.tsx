import React, { useEffect } from "react";
import { Modal, ModalBody } from "reactstrap";
import * as BrandVueApi from "../../../BrandVueApi";
import { IApplicationUser, SavedBreakCombination, CrossMeasure, PermissionFeaturesOptions } from "../../../BrandVueApi";
import { useSavedBreaksContext } from "../Crosstab/SavedBreaksContext";
import DeleteSavedBreaksModal from "./DeleteSavedBreaksModal";
import { BreakPickerParent } from "./BreaksDropdownHelper";
import FeatureGuard from "../../FeatureGuard/FeatureGuard";
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from "../../../components/visualisations/Settings/Weighting/Controls/MaterialSymbol";
interface IProps {
    isOpen: boolean;
    breakForEditing?: SavedBreakCombination;
    breaks: CrossMeasure[];
    user: IApplicationUser | null;
    closeModal: () => void;
    parentComponent: BreakPickerParent;
}

const SaveEditBreaksModal = (props: IProps) => {
    const { savedBreaksDispatch } = useSavedBreaksContext();
    const [name, setName] = React.useState<string>(props.breakForEditing?.name ?? "");
    const [isShared, setIsShared] = React.useState<boolean>(props.breakForEditing?.isShared ?? true);
    
    const [deleteSavedBreaksModalOpen, setDeleteSavedBreaksModalOpen] = React.useState(false);
    const userCanEditOrDelete = (props.user?.isAdministrator ?? false) || (props.user?.userId === props.breakForEditing?.createdByUserId) || !props.breakForEditing;
    const isControlDisabled = !userCanEditOrDelete;
    const [errorMessage, setErrorMessage] = React.useState<string>("");

    useEffect(() => {
        setName(props.breakForEditing?.name ?? "");
        setIsShared(props.breakForEditing?.isShared ?? true);
        setErrorMessage("");
    }, [props.breakForEditing]);

    const handleSaveOrUpdate = () => {
        if (name.trim() === "") {
            setErrorMessage("Name cannot be blank.");
        } else {
            const savedBreaksClient = BrandVueApi.Factory.SavedBreaksClient(error => error());
            let allowSaveBreak = true;
            const savedBreakId = props.breakForEditing?.id ?? -1;

            savedBreaksClient.getBreakForSubproduct(name)
                .then(result => {
                        if (result && result.id && result.id !== 0 && result.id !== savedBreakId) {
                            setErrorMessage("A break with this name already exists.");
                            allowSaveBreak = false;
                        }
                        if (allowSaveBreak) {
                            if (!props.breakForEditing) {
                                savedBreaksDispatch({
                                    type: "SAVE_BREAKS",
                                    data: {
                                        name,
                                        isShared,
                                        breaks: props.breaks,
                                        isSavedFromCrosstab: props.parentComponent == BreakPickerParent.Crosstab
                                    }
                                }).then(props.closeModal);
                            } else {
                                savedBreaksDispatch({
                                    type: "UPDATE_SAVED_BREAKS",
                                    data: {
                                        savedBreaksId: savedBreakId,
                                        name,
                                        isShared,
                                        isUpdatedFromCrosstab: props.parentComponent == BreakPickerParent.Crosstab
                                    }
                                }).then(props.closeModal);
                            }
                        }
                    }
                )
                .catch(error => { setErrorMessage("Unable to verify break, please try again"); });
        }
    };

    const inputNameOnChange = (value: string) => {
        setName(value);
        setErrorMessage("");
    };

    const updatedSharedChecked = (shared: boolean) => {
        setIsShared(shared);
    };

    const closeDeleteModal = (deleted: boolean) => {
        setDeleteSavedBreaksModalOpen(false);
        if (deleted) {
            props.closeModal();
        }
    }

    return (
        <FeatureGuard permissions={[props.breakForEditing ? PermissionFeaturesOptions.BreaksEdit : PermissionFeaturesOptions.BreaksAdd]}>
            <Modal isOpen={props.isOpen} centered={true} autoFocus={false} className="save-breaks-modal">
                {props.breakForEditing && (
                    <>
                        <DeleteSavedBreaksModal isOpen={deleteSavedBreaksModalOpen}
                            savedBreaksToDelete={props.breakForEditing}
                            closeModal={(deleted) => closeDeleteModal(deleted)} />
                        <button onClick={props.closeModal} className="modal-close-button">
                            <i className="material-symbols-outlined">close</i>
                        </button>
                        <FeatureGuard permissions={[PermissionFeaturesOptions.BreaksDelete]}>
                            <button onClick={() => setDeleteSavedBreaksModalOpen(true)} className="delete-saved-breaks" 
                            title={(isControlDisabled ? "This break is managed by someone else. Only Administrators can delete other users' breaks.": "Delete breaks")} disabled={isControlDisabled}>
                                <i className="material-symbols-outlined">delete</i>
                            </button>
                        </FeatureGuard>
                    </>
                )}
                <h3>{props.breakForEditing ? "Edit breaks" : "Save breaks"}</h3>
                <ModalBody>
                    {isControlDisabled &&
                        <div className="warning-panel">
                            <div className="warningMaterialIcon">
                                <MaterialSymbol symbolType={MaterialSymbolType.warning} symbolStyle={MaterialSymbolStyle.outlined} />
                            </div>
                            <div>
                                This break is managed by someone else. Only Administrators can modify other users' breaks.
                            </div>
                        </div>
                    }
                    <div className="input-container">
                        <label htmlFor="saved-break-name-input">Name</label>
                        <input
                            className={`saved-break-name-input ${errorMessage ? "error" : ""}`}
                            id="saved-break-name-input"
                            type="text"
                            autoFocus={true}
                            autoComplete="off"
                            value={name}
                            onChange={(e) => inputNameOnChange(e.target.value)}
                            disabled={isControlDisabled}
                        />
                        {errorMessage && <label className="error-input-message">{errorMessage}</label>}
                    </div>
                    <div className="set-as-shared-input">
                        <input
                            id="saved-break-shared-input"
                            className="checkbox saved-break-shared-input"
                            type="checkbox"
                            checked={isShared}
                            onChange={(e) => updatedSharedChecked(e.target.checked)}
                            disabled={isControlDisabled}
                        />
                        <label htmlFor="saved-break-shared-input">Share with other users</label>
                    </div>
                    <div className="button-container">
                        <button onClick={props.closeModal} className="secondary-button">Cancel</button>
                        <button onClick={handleSaveOrUpdate} className="primary-button" disabled={isControlDisabled}>
                            {props.breakForEditing ? "Save changes" : "Save"}
                        </button>
                    </div>
                </ModalBody>
            </Modal>
        </FeatureGuard >
    );
};

export default SaveEditBreaksModal;