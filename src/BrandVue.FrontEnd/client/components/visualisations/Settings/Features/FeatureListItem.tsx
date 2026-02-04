import React from 'react';
import { IFeatureModel } from 'client/BrandVueApi';
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from 'reactstrap';
import Tooltip from '../../../Tooltip';
import style from './Features.module.less';
import { useAppDispatch } from '../../../../state/store';
import { fetchUserFeatures, activateFeature, deactivateFeature } from '../../../../state/featuresSlice';

interface IFeatureConfigListItemProps {
    feature: IFeatureModel;
    isSelected: boolean;
    isReadOnly: boolean;
    setSelectedfeature(feature: IFeatureModel): void;
    setIsFeatureEditModalOpen(isOpen: boolean): void;
    setFeatureToEdit(feature: IFeatureModel): void;
    confirmDelete(feature: IFeatureModel): void;
}

const FeatureListItem = (props: IFeatureConfigListItemProps) => {
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const dispatch = useAppDispatch();

    const toggleDropdown = (e: React.MouseEvent) => {
        e.stopPropagation();
        setDropdownOpen(!dropdownOpen);
    }

    const handleEditFeature = () => {
        props.setIsFeatureEditModalOpen(true);
        props.setFeatureToEdit(props.feature);
        dispatch(fetchUserFeatures(props.feature.id));
    }

    const handleActivateFeature = () => {
        dispatch(activateFeature(props.feature));
    }

    const handleDeactivateFeature = () => {
        dispatch(deactivateFeature(props.feature.id));
    }

    const canDeleteFeature = (feature: IFeatureModel): boolean => {
        return feature.isInDatabase && !feature.isInEnum && !feature.isActive;
    }

    const handleDeleteFeature = () => {
        props.confirmDelete(props.feature);
    }

    const containerClass = `${style.listitem}${props.isSelected ? ` ${style.selected}` : ''}`;

    return (
        <div className={containerClass} onClick={() => props.setSelectedfeature(props.feature)}>
            <div className={style.titleContainer}>
                {props.feature.name}
                {!props.isReadOnly &&
                <span className={style.buttons}>
                    <ButtonDropdown isOpen={dropdownOpen} toggle={toggleDropdown} className={style.styledDropdown}>
                        <DropdownToggle className={style.styledToggle}>
                            <i className="material-symbols-outlined">more_vert</i>
                        </DropdownToggle>
                        <DropdownMenu className={style.styledDropdownMenu}>
                            {!props.feature.isActive &&
                            <>
                                <DropdownItem onClick={handleActivateFeature} className={style.styledDropdownItem}>
                                    <i className={`material-symbols-outlined ${style.editButton}`}>enable</i>Activate
                                </DropdownItem>
                                <DropdownItem onClick={handleDeleteFeature} className={style.styledDropdownItem} disabled={!canDeleteFeature(props.feature)}>
                                    <i className={`material-symbols-outlined ${style.editButton}`}>delete</i>Delete
                                </DropdownItem>
                            </>
                            }
                            {props.feature.isActive && 
                            <>
                                <DropdownItem onClick={handleDeactivateFeature} className={style.styledDropdownItem}>
                                    <i className={`material-symbols-outlined ${style.editButton}`}>block</i>Deactivate
                                </DropdownItem>
                                <DropdownItem onClick={handleEditFeature} className={style.styledDropdownItem}>
                                    <i className={`material-symbols-outlined ${style.editButton}`}>edit</i>Edit
                                </DropdownItem>
                            </>
                            }
                        </DropdownMenu>
                    </ButtonDropdown>
                </span>}
            </div>
        </div>
    );
}

export default FeatureListItem;