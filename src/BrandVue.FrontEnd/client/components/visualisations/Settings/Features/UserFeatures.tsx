import React from 'react';
import { Button } from 'reactstrap';
import { IFeature, IUserFeatureModel, IUserProjectsModel, UserProjectsModel } from '../../../../BrandVueApi';
import toast from 'react-hot-toast';
import AddUsersModal from './AddUsersModal';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { createUserFeature } from '../../../../state/featuresSlice';
import style from './Features.module.less';
import { userContainsSearchText } from '../Users/UsersHelpers';

const ITEMS_PER_PAGE = 10;

interface IUserFeatures {
    feature: IFeature;
    isReadOnly: boolean;
    updateUserFeatures: () => void;
    confirmDelete: (orgFeature: IUserFeatureModel) => void;
}

const UserFeaturesSection = (props: IUserFeatures) => {
    const dispatch = useAppDispatch();
    const { allUsers, userFeaturesByFeatureId } = useAppSelector((state) => state.features);

    const [showAddUser, setShowAddUser] = React.useState<boolean>(false);
    const [currentPage, setCurrentPage] = React.useState(1);
    const [searchText, setSearchText] = React.useState<string>('');

    const userFeaturesForUsers = React.useMemo(() => 
        userFeaturesByFeatureId[props.feature.id].filter(uf => 
            allUsers.some(user => user.applicationUserId === uf.userId)
        ) || [],
        [userFeaturesByFeatureId, allUsers]
    );

    const filteredUserFeatures = React.useMemo(() => 
        userFeaturesForUsers.filter(uf => {
            const user = allUsers.find(user => user.applicationUserId === uf.userId);
            return user && userContainsSearchText(user, searchText);
        }) || [],
        [userFeaturesForUsers, allUsers, searchText]
    );

    const totalPages = Math.ceil(filteredUserFeatures.length / ITEMS_PER_PAGE);
    const indexOfLastItem = currentPage * ITEMS_PER_PAGE;
    const indexOfFirstItem = indexOfLastItem - ITEMS_PER_PAGE;
    const currentItems = filteredUserFeatures.slice(indexOfFirstItem, indexOfLastItem);

    const addUsers = React.useCallback(async (users: IUserProjectsModel[]) => {
        try {
            users.map(user => dispatch(createUserFeature({featureId: props.feature.id, userId: user.applicationUserId})));
            setShowAddUser(false);
        } catch (error) {
            toast.error('Failed to add users');
        }
    }, [props.feature.id, dispatch]);

    const showSearchBar = userFeaturesForUsers?.length > 0;

    return (<>
            <div className={style.tabHeader}>
                <h1>
                    <span className={style.tabHeaderLeft}>{props.feature.name}/</span> 
                    <span className={style.tabHeaderRight}>Users</span>
                </h1>
                <div className={style.tabHeaderButtons}>
                    {showSearchBar && (
                        <input
                            type="text"
                            placeholder="Search users..."
                            value={searchText}
                            onChange={(e) => setSearchText(e.target.value)}
                        />
                    )}
                    {!props.isReadOnly && (
                        <Button type="button" className="hollow-button" onClick={() => setShowAddUser(true)}>
                            + Add user
                        </Button>
                    )}
                </div>
            </div>
            {filteredUserFeatures.length > 0 && (<>
                <div className={style.featuresTableContainer}>
                    <table className={style.featuresTable}>
                        <thead>
                            <tr key="_head">
                                <td>Full Name</td>
                                <td>Organisation</td>
                                <td>Email</td>
                                <td></td>
                            </tr>
                        </thead>
                        <tbody>
                            {currentItems.map(userFeature => {
                                const user = allUsers.find(user => user.applicationUserId == userFeature.userId);
                                return (<tr key={userFeature.userId}>
                                    <td>{`${user?.firstName} ${user?.lastName}`}</td>
                                    <td>{user?.organisationName}</td>
                                    <td>{user?.email}</td>
                                    <td className={style.rowDeleteXContainer}>
                                        <span className={style.rowDeleteX} onClick={() => props.confirmDelete(userFeature)}>
                                            <i className="material-symbols-outlined">close</i>
                                        </span>
                                    </td>
                                </tr>)
                            })}
                        </tbody>
                    </table>
                </div>
                <div className={style.paginationContainer}>
                    <Button onClick={() => setCurrentPage(currentPage - 1)} hidden={currentPage === 1} className={`hollow-button ${style.btnPrevious}`}>
                        Previous
                    </Button>
                    
                    <div className={style.pageLabel}>
                        Page {currentPage} of {totalPages}
                    </div>
                    
                    <Button onClick={() => setCurrentPage(currentPage + 1)} hidden={currentPage === totalPages} className={`hollow-button ${style.btnNext}`}>
                        Next
                    </Button>
                </div>
            </>)}
            <AddUsersModal
                isOpen={showAddUser}
                setIsOpen={setShowAddUser}
                currentSet={filteredUserFeatures}
                addUsers={addUsers}
            />
            <div className={style.noFeaturesMessage}>
                {filteredUserFeatures.length == 0 && (
                    `This feature doesn't have any users associated with it yet.`
                )}
            </div>
    </>
    );
}
export default UserFeaturesSection;