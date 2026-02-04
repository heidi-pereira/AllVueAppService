import React from 'react';
import ModalPage from "../../../ModalPage";
import MultiPageModal from "../../../MultiPageModal";
import { Label } from 'reactstrap';
import { IOrganisationFeatureModel } from '../../../../BrandVueApi';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { deleteOrgFeature } from '../../../../state/featuresSlice';

interface IDeleteOrgModalProps {
    isOpen: boolean;
    orgFeature: IOrganisationFeatureModel
    orgName: string | undefined;
    featureName: string | undefined;
    setIsOpen(isOpen: boolean): void;
}   

const DeleteOrgModal = (props: IDeleteOrgModalProps) => {
    const dispatch = useAppDispatch();
    const { allUsers, userFeaturesByFeatureId } = useAppSelector((state) => state.features);

    const handleDelete = async () => {
        try {
            await dispatch(deleteOrgFeature({ organisationId: props.orgFeature.organisationId, featureId: props.orgFeature.featureId }));
        } catch (error) {
            console.error('Failed to delete organization feature:', error);
        }
    };

    const getDeleteButton = () => {
        if (!props.isOpen) {
            return (
                <button onClick={() => props.setIsOpen(false)}>
                    <i className="material-symbols-outlined">delete</i>
                </button>
            );
        }
    };

    const hasUsersAssigned = () => {
        // Check if the organisation still has users assigned to the feature
        const userFeatures = allUsers
            .filter(user => userFeaturesByFeatureId[props.orgFeature.featureId]
            ?.some(uf => uf.userId === user.applicationUserId && 
                user.organisationId === props.orgFeature.organisationId
            ));
        return userFeatures.length > 0;
    };

    return (<MultiPageModal isOpen={props.isOpen}
        setIsOpen={props.setIsOpen}
        header={`Remove ${props.orgName}'s access to ${props.featureName}`}
        headerButtons={getDeleteButton()}>

        <ModalPage 
        className="entity-set-edit-modal" 
        actionButtonText="Remove" 
        actionButtonCss="primary-button delay-click" 
        actionButtonHandler={handleDelete}>
            <Label className="label">{`Are you sure you want to remove ${props.orgName}'s access to ${props.featureName}?`}</Label>
            {hasUsersAssigned() && (
                <div className="warning">
                    Warning: This organisation still has users assigned to this feature. Removing access may impact those users.
                </div>
            )}
            <div>
                You can grant this organisation access again at any time.
            </div>
        </ModalPage>
    </MultiPageModal>
    );
}
export default DeleteOrgModal;