import React from 'react';
import { IOrganisationFeatureModel, ICompanyModel, IFeature } from "../../../../BrandVueApi";
import ToggleSwitch from '../../../checkboxes/ToggleSwitch';
import Tooltip from '../../../Tooltip';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { createOrgFeature, clearFeaturesCache } from '../../../../state/featuresSlice';
import DeleteOrgModal from './DeleteOrgModal';
import style from './Features.module.less';

interface IOrgsTabProps {
    selectedOrg: ICompanyModel;
    isReadOnly: boolean;
}

const OrgsTab: React.FC<IOrgsTabProps> = (props) => {
    const dispatch = useAppDispatch();
    const { features, orgFeatures, allUsers } = useAppSelector((state) => state.features);

    const [isOrgDeleteModalOpen, setIsOrgDeleteModalOpen] = React.useState<boolean>(false);
    const [orgFeatureToDelete, setOrgFeatureToDelete] = React.useState<IOrganisationFeatureModel | undefined>(undefined);

    const [filteredOrgFeatures, setFilteredOrgFeatures] = React.useState<IOrganisationFeatureModel[]>([]);

    React.useEffect(() => {
        setFilteredOrgFeatures(orgFeatures.filter(of => of.organisationId === props.selectedOrg.id));
    }, [orgFeatures, props.selectedOrg]);

    const confirmOrgDelete = (orgFeature: IOrganisationFeatureModel) => {
        setOrgFeatureToDelete(orgFeature);
        setIsOrgDeleteModalOpen(true);
    };

    const handleToggle = async (orgFeature: IOrganisationFeatureModel | undefined, feature: IFeature) => {
        if (orgFeature) {
            confirmOrgDelete(orgFeature);
        } else {
            try {
                await dispatch(createOrgFeature({ organisationId: props.selectedOrg.id, featureId: feature.id }));
            } catch {
                console.error('Failed to create organization feature');
            }
        }
    };

    const getUserName = (userId: string) => {
        const user = allUsers.find(u => u.applicationUserId === userId);
        return user ? `${user.firstName} ${user.lastName}` : "Unknown";
    };

    return (
        <>
            <div className={style.tabContent}>
                <div className={style.tabHeader}>
                    <h1>
                        <span className={style.tabHeaderLeft}>{props.selectedOrg.displayName}/</span>
                        <span className={style.tabHeaderRight}>Features</span>
                    </h1>
                </div>
                {features.length > 0 && (
                <div style={{ overflow:"auto", marginTop:"5px", marginBottom:"2rem" }}>
                    <table className={style.featuresTable}>
                        <thead>
                            <tr key="_head">
                                <td>Feature</td>
                                <td></td>
                                <td>Enabled</td>
                            </tr>
                        </thead>
                        <tbody>
                            {features.map(feature => {
                                const orgFeature = filteredOrgFeatures?.find(f => f.featureId === feature.id);
                                const checked = !!orgFeature;
                                return (<tr key={feature.id}>
                                    <td>{feature.name}</td>
                                    <td style={{textAlign:"right"}}>
                                        {orgFeature && orgFeature.updatedByUserId && orgFeature.updatedDate &&  
                                            `Added on ${new Date(orgFeature.updatedDate).toLocaleDateString('en-GB')} by ${getUserName(orgFeature.updatedByUserId)}`
                                        }
                                    </td>
                                    <td>
                                        <ToggleSwitch checked={checked} disabled={props.isReadOnly} onChange={() => handleToggle(orgFeature, feature)}/>
                                    </td>
                                </tr>);
                            })}
                        </tbody>
                    </table>
                </div>
                )}
                <Tooltip placement="top" title={`This action forces the user features cache to be cleared`}>
                    <button className="secondary-button" onClick={() => dispatch(clearFeaturesCache())}>Clear features cache</button>
                </Tooltip>
            </div>
            {!props.isReadOnly && orgFeatureToDelete != undefined && (
                <DeleteOrgModal
                    isOpen={isOrgDeleteModalOpen}
                    setIsOpen={setIsOrgDeleteModalOpen}
                    orgFeature={orgFeatureToDelete}
                    orgName={props.selectedOrg.displayName}
                    featureName={features.find(f => f.id === orgFeatureToDelete.featureId)?.name}
                />
            )}
        </>
    );
};

export default OrgsTab;