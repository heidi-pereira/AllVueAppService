import React from 'react';
import ModalPage from "../../../ModalPage";
import MultiPageModal from "../../../MultiPageModal";
import { Label } from 'reactstrap';
import { IFeatureModel } from '../../../../BrandVueApi';

interface IDeleteFeatureModalProps {
    isOpen: boolean;
    feature: IFeatureModel;
    setIsOpen(isOpen: boolean): void;
    onDelete(feature: IFeatureModel): void;
}

const DeleteFeatureModal = (props: IDeleteFeatureModalProps) => {

    const getDeleteButton = () => {
        if (!props.isOpen) {
            return (
                <button onClick={() => {
                    props.setIsOpen(false);
                }}>
                    <i className="material-symbols-outlined">delete</i>
                </button>
            );
        }
    }

    return (<MultiPageModal isOpen={props.isOpen}
        setIsOpen={props.setIsOpen}
        header={`Delete feature '${props.feature.name}'`}
        headerButtons={getDeleteButton()}>

        <ModalPage className="entity-set-delete-modal"
        actionButtonText="Delete"
        actionButtonCss="negative-button delay-click"
            actionButtonHandler={() => props.onDelete(props.feature)}>
            <Label className="label">{`Are you sure you want to delete the ${props.feature.name} ( ${props.feature.featureCode}) group?`}</Label>
        <div className="entity-set-validation-text">
            This will affect all users associated to this feature and cannot be undone.
        </div>
        </ModalPage>
    </MultiPageModal>
    );
}
export default DeleteFeatureModal;