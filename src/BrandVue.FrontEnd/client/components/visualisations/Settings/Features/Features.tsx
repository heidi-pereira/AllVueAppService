import React from 'react';
import Throbber from '../../../throbber/Throbber';
import { ProductConfiguration } from '../../../../ProductConfiguration';
import { ICompanyModel, IFeatureModel } from '../../../../BrandVueApi';
import style from './Features.module.less';
import SearchInput from '../../../SearchInput';
import { Nav, NavItem, NavLink } from 'reactstrap';
import FeaturesTab from './FeaturesTab';
import OrgsTab from './OrgsTab';
import FeatureListItem from './FeatureListItem';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { fetchFeatures, fetchUserFeatures, fetchOrgFeatures, fetchAllOrgs, fetchAllUsers } from '../../../../state/featuresSlice';

interface IFeaturesProps {
    productConfiguration: ProductConfiguration;
    nav: React.ReactNode;
}

const FeaturesPage = (props: IFeaturesProps) => {
    const dispatch = useAppDispatch();
    const {features, allOrgs, loading} = useAppSelector((state) => state.features);

    const [selectedFeature, setSelectedFeature] = React.useState<IFeatureModel>();
    const [selectedOrg, setSelectedOrg] = React.useState<ICompanyModel>();
    const [searchText, setSearchText] = React.useState<string>('');
    const [activeTab, setActiveTab] = React.useState<string>('orgs');
    const [isFeatureEditModalOpen, setIsFeatureEditModalOpen] = React.useState<boolean>(false);
    const [isFeatureDeleteModalOpen, setIsFeatureDeleteModalOpen] = React.useState<boolean>(false);
    const [featureToEdit, setFeatureToEdit] = React.useState<IFeatureModel | undefined>(undefined);
    const [featureToDelete, setFeatureToDelete] = React.useState<IFeatureModel|undefined>(undefined);

    const isReadOnly = !props.productConfiguration.user.isSystemAdministrator;

    React.useEffect(() => {
        dispatch(fetchFeatures());
        dispatch(fetchAllOrgs());
        dispatch(fetchAllUsers());
    }, [dispatch]);

    React.useEffect(() => {
        if (selectedFeature) {
            dispatch(fetchUserFeatures(selectedFeature.id));
            dispatch(fetchOrgFeatures());
        }
    }, [dispatch, selectedFeature]);

    React.useEffect(() => {
        setSelectedOrg(allOrgs[0]);
    }, [allOrgs]);

    React.useEffect(() => {
        if (features.length > 0 && !selectedFeature) {
            setSelectedFeature(features[0]); // Set the first feature as the selected feature on render
        }
    }, [features, selectedFeature]);

    React.useEffect(() => {
        setSearchText(''); // Clear search text when switching tabs
    }, [activeTab]);

    if (loading) {
        return (
            <section className="user-settings-page">
                <div className="throbber-container-fixed">
                    <Throbber />
                </div>
            </section>
        );
    }

    const confirmFeatureDelete = (feature: IFeatureModel) => {
        setFeatureToDelete(feature);
        setIsFeatureDeleteModalOpen(true);
    }

    const getFeaturesList = (): JSX.Element[] => {
        const filteredFeatures = features.filter((feature) => feature.name.toLowerCase().includes(searchText.toLowerCase()));
        const activeFeatures = filteredFeatures.filter((feature) => feature.isActive);
        const inactiveFeatures = filteredFeatures.filter((feature) => !feature.isActive);

        const activeList = activeFeatures.map((feature, i) => (
            <FeatureListItem
                key={`feature-list-item-active-${i}`}
                feature={feature}
                isSelected={selectedFeature?.id === feature.id}
                isReadOnly={isReadOnly}
                setSelectedfeature={setSelectedFeature}
                setIsFeatureEditModalOpen={setIsFeatureEditModalOpen}
                setFeatureToEdit={setFeatureToEdit}
                confirmDelete={confirmFeatureDelete}
            />
        ));

        const inactiveList = inactiveFeatures.map((feature, i) => (
            <FeatureListItem
                key={`feature-list-item-inactive-${i}`}
                feature={feature}
                isSelected={selectedFeature?.id === feature.id}
                isReadOnly={isReadOnly}
                setSelectedfeature={setSelectedFeature}
                setIsFeatureEditModalOpen={setIsFeatureEditModalOpen}
                setFeatureToEdit={setFeatureToEdit}
                confirmDelete={confirmFeatureDelete}
            />
        ));

        return [
            <div key="active-title" className={style.listTitle}>Active</div>,
            ...activeList,
            <div key="inactive-title" className={style.listTitle}>Inactive</div>,
            ...inactiveList
        ];
    };

    const getOrgsList = (): JSX.Element[] => {
        return allOrgs.filter((org) => org.displayName.toLowerCase().includes(searchText.toLowerCase())) // Filter organizations based on search text
            .map((org, i) => {
                return (
                    <div
                        key={`org-list-item-${i}`}
                        className={org.id === selectedOrg?.id ? `${style.listitem} ${style.selected}` : style.listitem}
                        onClick={() => setSelectedOrg(org)}
                    >
                        <div className={style.titleContainer}>{org.displayName}</div>
                    </div>
                );
            });
    };

    return (
        <div className={style.featurespage}>
            {props.nav}
            <div className={style.leftpane}>
                <div className={style.sidebarcontainer}>
                    <Nav tabs>
                        <NavItem>
                            <NavLink
                                className={activeTab === 'orgs' ? 'tab-active' : 'tab-item'}
                                onClick={() => setActiveTab('orgs')}
                            >
                                Organisations
                            </NavLink>
                        </NavItem>
                        <NavItem>
                            <NavLink
                                className={activeTab === 'features' ? 'tab-active' : 'tab-item'}
                                onClick={() => setActiveTab('features')}
                            >
                                Features
                            </NavLink>
                        </NavItem>
                    </Nav>
                    <div className={style.searchcontainer}>
                        <SearchInput
                            id="search"
                            onChange={(text) => setSearchText(text)}
                            text={searchText}
                            className={style.searchinputgroup}
                            autoFocus={true}
                        />
                    </div>
                    <div className={style.sidebarlist}>
                        {activeTab === 'features' ? getFeaturesList() : getOrgsList()}
                    </div>
                </div>
            </div>
            <div className={style.rightpane}>
                {activeTab === 'features' && selectedFeature && (
                    <FeaturesTab
                        selectedFeature={selectedFeature}
                        featureToEdit={featureToEdit}
                        featureToDelete={featureToDelete}
                        isReadOnly={isReadOnly}
                        isEditFeatureModalOpen={isFeatureEditModalOpen}
                        isFeatureDeleteModalOpen={isFeatureDeleteModalOpen}
                        setIsEditFeatureModalOpen={setIsFeatureEditModalOpen}
                        setIsFeatureDeleteModalOpen={setIsFeatureDeleteModalOpen}
                    />
                )}
                {activeTab === 'orgs' && selectedOrg && (
                    <OrgsTab
                        selectedOrg={selectedOrg}
                        isReadOnly={isReadOnly}
                    />
                )}
            </div>
        </div>
    );
};

export default FeaturesPage;