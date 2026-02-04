import React from 'react';
import Tooltip from '../../../Tooltip';
import UserFeaturesSection from './UserFeatures'; 
import OrgFeaturesSection from './OrgFeatures';
import DeleteFeatureModal from './DeleteFeatureModal';
import { Toaster, toast } from 'react-hot-toast';
import { IFeatureModel, IOrganisationFeatureModel, IUserFeatureModel } from '../../../../BrandVueApi';
import EditFeatureModal from './EditFeatureModal';
import DeleteOrgModal from './DeleteOrgModal';
import DeleteUserModal from './DeleteUserModal ';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { fetchFeatures, fetchUserFeatures, clearFeaturesCache, deleteFeature as deleteFeatureThunk } from '../../../../state/featuresSlice';
import style from './Features.module.less';

interface IFeaturesTabProps {
    selectedFeature: IFeatureModel;
    isReadOnly: boolean;
    featureToEdit: IFeatureModel | undefined;
    featureToDelete: IFeatureModel | undefined;
    isEditFeatureModalOpen: boolean;
    isFeatureDeleteModalOpen: boolean;
    setIsEditFeatureModalOpen: (isOpen: boolean) => void;
    setIsFeatureDeleteModalOpen: (isOpen: boolean) => void;
}

const FeaturesTab: React.FC<IFeaturesTabProps> = (props) => {
    const dispatch = useAppDispatch();
    const { features, allOrgs, allUsers } = useAppSelector((state) => state.features);

    const [isFeatureDeleteModalOpen, setIsFeatureDeleteModalOpen] = React.useState<boolean>(false);
    const [isOrgDeleteModalOpen, setIsOrgDeleteModalOpen] = React.useState<boolean>(false);
    const [isUserDeleteModalOpen, setIsUserDeleteModalOpen] = React.useState<boolean>(false);
    const [orgFeatureToDelete, setOrgFeatureToDelete] = React.useState<IOrganisationFeatureModel|undefined>(undefined);
    const [userFeatureToDelete, setUserFeatureToDelete] = React.useState<IUserFeatureModel|undefined>(undefined);

    const deleteFeature = (feature: IFeatureModel | undefined) => {
        if (!feature) return;
        dispatch(deleteFeatureThunk(feature.id))
            .unwrap()
            .then(() => {
                setIsFeatureDeleteModalOpen(false);
                props.setIsEditFeatureModalOpen(false);
            })
            .catch(() => toast.error(`Failed to delete feature ${feature.featureCode}`));
    }

    const confirmOrgDelete = (orgFeature: IOrganisationFeatureModel) => {
        setOrgFeatureToDelete(orgFeature);
        setIsOrgDeleteModalOpen(true);
    }

    const confirmUserDelete = (userFeature: IUserFeatureModel) => {
        setUserFeatureToDelete(userFeature);
        setIsUserDeleteModalOpen(true);
    }

    const deleteUserModal = () => {
        if (props.isReadOnly || userFeatureToDelete == undefined) return null;
        
        const user = allUsers.find(u => u.applicationUserId == userFeatureToDelete.userId);
        return (
            <DeleteUserModal 
                isOpen={isUserDeleteModalOpen} 
                setIsOpen={setIsUserDeleteModalOpen} 
                userFeature={userFeatureToDelete} 
                userName={`${user?.firstName} ${user?.lastName}`}
                featureName={features.find(f => f.id == userFeatureToDelete.featureId)?.name}
            />
        );
    };

    return (
        <>
            <div className={style.tabContent}>
                <OrgFeaturesSection
                    feature={props.selectedFeature}
                    isReadOnly={props.isReadOnly}
                    confirmDelete={confirmOrgDelete}
                />
                <UserFeaturesSection
                    feature={props.selectedFeature}
                    isReadOnly={props.isReadOnly}
                    updateUserFeatures={() => dispatch(fetchUserFeatures(props.selectedFeature.id))}
                    confirmDelete={confirmUserDelete}
                />
                <Tooltip placement="top" title={`This action forces the user features cache to be cleared`}>
                    <button className="secondary-button" onClick={() => dispatch(clearFeaturesCache())}>Clear features cache</button>
                </Tooltip>
            </div>
            {!props.isReadOnly && props.featureToDelete != undefined &&
                <DeleteFeatureModal isOpen={props.isFeatureDeleteModalOpen} setIsOpen={props.setIsFeatureDeleteModalOpen} onDelete={deleteFeature} feature={props.featureToDelete} />
            }
            {!props.isReadOnly && orgFeatureToDelete != undefined &&
                <DeleteOrgModal 
                    isOpen={isOrgDeleteModalOpen} 
                    setIsOpen={setIsOrgDeleteModalOpen}
                    orgFeature={orgFeatureToDelete} 
                    orgName={allOrgs.find(o => o.id == orgFeatureToDelete.organisationId)?.displayName}
                    featureName={features.find(f => f.id == orgFeatureToDelete.featureId)?.name}
                />
            }
            {deleteUserModal()}
            {props.featureToEdit != undefined &&
                <EditFeatureModal
                    isOpen={props.isEditFeatureModalOpen}
                    featureToEdit={props.featureToEdit.id}
                    isReadOnly={props.isReadOnly}
                    setIsOpen={props.setIsEditFeatureModalOpen}
                />
            }
            <Toaster position='bottom-center' toastOptions={{ duration: 5000 }} />
        </>
    );
}

export default FeaturesTab;