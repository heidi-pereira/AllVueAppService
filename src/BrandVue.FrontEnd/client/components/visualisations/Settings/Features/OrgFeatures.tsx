import React from 'react';
import { IFeature, IOrganisationFeatureModel, ICompanyModel } from "../../../../BrandVueApi";
import toast from 'react-hot-toast';
import { Button } from 'reactstrap';
import AddOrgsModal from './AddOrgsModal';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { createOrgFeature } from '../../../../state/featuresSlice';
import style from './Features.module.less';

const ITEMS_PER_PAGE = 5;

interface IOrgFeatures {
    feature: IFeature;
    isReadOnly: boolean;
    confirmDelete: (orgFeature: IOrganisationFeatureModel) => void;
}

const OrgFeaturesSection = (props: IOrgFeatures) => {
    const dispatch = useAppDispatch();
    const { allOrgs, orgFeatures } = useAppSelector((state) => state.features);

    const [showAddOrg, setShowAddOrg] = React.useState<boolean>(false);
    const [currentPage, setCurrentPage] = React.useState(1);
    const [searchText, setSearchText] = React.useState<string>(''); 

    const filteredOrgFeatures = React.useMemo(() => {
        if (!orgFeatures) {
            return [];
        }

        const orgMap = new Map(allOrgs.map(org => [org.id, org.displayName]));

        return (
            orgFeatures
                .filter(of => of.featureId === props.feature.id)
                .filter(of => orgMap.has(of.organisationId))
                .filter(of => orgMap.get(of.organisationId)?.toLowerCase().includes(searchText.toLowerCase())) // Filter by search text
                .sort((a, b) => {
                    const orgA = orgMap.get(a.organisationId) || '';
                    const orgB = orgMap.get(b.organisationId) || '';
                    return orgA.localeCompare(orgB);
                })
            );
    }, [orgFeatures, allOrgs, searchText]);

    const totalPages = Math.ceil(filteredOrgFeatures.length / ITEMS_PER_PAGE);
    const indexOfLastItem = currentPage * ITEMS_PER_PAGE;
    const indexOfFirstItem = indexOfLastItem - ITEMS_PER_PAGE;
    const currentItems = filteredOrgFeatures.slice(indexOfFirstItem, indexOfLastItem);

    const handlePageChange = (pageNumber: number) => {
        setCurrentPage(pageNumber);
    };

    const addOrgs = React.useCallback(async (orgs: ICompanyModel[]) => {
        try {
            const promises = orgs.map(org => 
                dispatch(createOrgFeature({featureId: props.feature.id, organisationId: org.id}))
            );
            await Promise.all(promises);
            setShowAddOrg(false);
        } catch (error) {
            toast.error('Failed to add organizations');
        }
    }, [props.feature.id, dispatch]);

    const showSearchBar = orgFeatures?.length > 0;

    return (<>
            <div className={style.tabHeader}>
                <h1>
                    <span className={style.tabHeaderLeft}>{props.feature.name}/</span> 
                    <span className={style.tabHeaderRight}>Organisations</span>
                </h1>
                <div className={style.tabHeaderButtons}>
                    {showSearchBar && (
                        <input
                            type="text"
                            placeholder="Search organisations..."
                            value={searchText}
                            onChange={(e) => setSearchText(e.target.value)}
                        />
                    )}
                    {!props.isReadOnly && (
                        <Button type="button" className="hollow-button" onClick={() => setShowAddOrg(true)}>
                            + Add organisation
                        </Button>
                    )}
                </div>
            </div>
            {filteredOrgFeatures.length > 0 && (<>
            <div className={style.featuresTableContainer}>
                <table className={style.featuresTable}>
                    <thead>
                        <tr key="_head">
                            <td>Organisation</td>
                            <td></td>
                        </tr>
                    </thead>
                    <tbody>
                        {currentItems.map(orgFeature => {
                            const organisation = allOrgs.find(org => org.id == orgFeature.organisationId);
                            return (<tr key={orgFeature.organisationId}>
                                <td>{organisation?.displayName}</td>
                                <td className={style.rowDeleteXContainer}>
                                    <span className={style.rowDeleteX} onClick={() => props.confirmDelete(orgFeature)}>
                                        <i className="material-symbols-outlined">close</i>
                                    </span>
                                </td>
                            </tr>)
                        })}
                    </tbody>
                </table>
            </div>
            <div className={style.paginationContainer}>
                <Button onClick={() => handlePageChange(currentPage - 1)} hidden={currentPage === 1} className={`hollow-button ${style.btnPrevious}`}>
                    Previous
                </Button>
                
                <div className={style.pageLabel}>
                    Page {currentPage} of {totalPages}
                </div>
                
                <Button onClick={() => handlePageChange(currentPage + 1)} hidden={currentPage === totalPages} className={`hollow-button ${style.btnNext}`}>
                    Next
                </Button>
            </div>
            </>)}
            <AddOrgsModal
                isOpen={showAddOrg}
                setIsOpen={setShowAddOrg}
                currentSet={filteredOrgFeatures}
                addOrgs={addOrgs}
            />
            <div className={style.noFeaturesMessage}>
                {filteredOrgFeatures.length == 0 && (
                    `This feature doesn't have any organisations associated with it yet.`
                )}
            </div>
    </>)
}
export default OrgFeaturesSection;