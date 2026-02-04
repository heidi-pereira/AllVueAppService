import React from 'react';
import ModalPage from "../../../ModalPage";
import MultiPageModal from "../../../MultiPageModal";
import { Label } from 'reactstrap';
import { IUserFeatureModel } from '../../../../BrandVueApi';
import { useAppDispatch } from '../../../../state/store';
import { deleteUserFeature } from '../../../../state/featuresSlice';

interface IDeleteUserModalProps {
    isOpen: boolean;
    userFeature: IUserFeatureModel
    userName: string | undefined;
    featureName: string | undefined;
    setIsOpen(isOpen: boolean): void;
}   

const DeleteUserModal = (props: IDeleteUserModalProps) => {
    const dispatch = useAppDispatch();

    const handleDelete = () => {
        dispatch(deleteUserFeature({ userId: props.userFeature.userId, featureId: props.userFeature.featureId }));
    };

    const getDeleteButton = () => {
        if (!props.isOpen) {
            return (
                <button onClick={() => props.setIsOpen(false)}>
                    <i className="material-symbols-outlined">delete</i>
                </button>
            );
        }
    }

    return (<MultiPageModal isOpen={props.isOpen}
        setIsOpen={props.setIsOpen}
        header={`Remove ${props.userName}'s access to ${props.featureName}`}
        headerButtons={getDeleteButton()}>

        <ModalPage 
        className="entity-set-edit-modal" 
        actionButtonText="Remove" 
        actionButtonCss="primary-button delay-click" 
        actionButtonHandler={handleDelete}>
            <Label className="label">{`Are you sure you want to remove ${props.userName}'s access to ${props.featureName}?`}</Label>
            <div>
                You can grant this user access again at any time.
            </div>
        </ModalPage>
    </MultiPageModal>
    );
}
export default DeleteUserModal;