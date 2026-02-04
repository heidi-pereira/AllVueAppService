import React from 'react';
import SearchInput from "../SearchInput";
import { EntitySet, sortEntitySets } from "../../entity/EntitySet";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import CreateNewEntitySetModal from "../CreateNewEntitySetModal";
import { EntityInstance } from "../../entity/EntityInstance";
import CreateOrEditEntitySetModal from "../CreateOrEditEntitySetModal";
import { IEntitySetConfigClient } from "../EntitySetConfigClient";
import { UserContext } from '../../GlobalContext';
import { IApplicationUser } from '../../BrandVueApi';
import { MixPanel } from '../mixpanel/MixPanel';

interface IEntitySetDropdownSelector {
    toggleElement: React.ReactElement<DropdownToggle>;
    entitySets: EntitySet[];
    selectEntitySet(entitySet: EntitySet | undefined): void;
    availableInstances: EntityInstance[];
    existingEntitySetNames: string[];
    selectedEntitySet: EntitySet;
    entitySetConfigClient: IEntitySetConfigClient;
    isBarometer: boolean;
    availableEntitySets: EntitySet[];
}

const EntitySetDropdownSelector = (props: IEntitySetDropdownSelector) => {
    const [isOpen, setIsOpen] = React.useState(false);
    const [searchQuery, setSearchQuery] = React.useState<string>("");
    const [isModalOpen, setIsModalOpen] = React.useState(false);
    const [isEditModalOpen, setIsEditModalOpen] = React.useState(false);
    const [isCreateModal, setIsCreateModal] = React.useState(false);

    const toggleEntitySetDropdown = () => {
        setIsOpen(!isOpen);
        setSearchQuery('');
    }

    const getDisplayText = (entitySet: EntitySet) => {
        return entitySet.name;
    }

    const getEntitySetElement = (entitySet: EntitySet, user: IApplicationUser | null) => {
        const displayText = getDisplayText(entitySet);
        const shouldShowSettingsIcon = !props.selectedEntitySet.isFallbackSet && (!props.selectedEntitySet.isSectorSet || user?.isSystemAdministrator);

        return (
            <DropdownItem key={entitySet.name} title={entitySet.name} className="tabbed" onClick={() => {
                MixPanel.track("mainEntityChanged");
                props.selectEntitySet(entitySet);
            }}>
                <div className="name-container">
                    <span className='title' title={displayText}>{displayText}</span>
                </div>
                {entitySet.name === props.selectedEntitySet.name && shouldShowSettingsIcon &&
                    <div className="settings-cog" onClick={() => {
                        setIsEditModalOpen(true);
                        setIsCreateModal(entitySet.id == null);
                    }}>
                        <i className="material-symbols-outlined">settings</i>
                    </div>
                }
            </DropdownItem>
        );
    }

    const getMatchedEntitySets = (user: IApplicationUser | null) => {
        let matchedEntitySets = props.entitySets;

        if (searchQuery && searchQuery.trim() != '') {
            matchedEntitySets = matchedEntitySets.filter(m =>
                m.name.toLowerCase().includes(searchQuery.toLowerCase()));
        }

        const groupEntitySet = matchedEntitySets.filter(entitySet => !entitySet.isSectorSet).sort(sortEntitySets);
        const sectorEntitySet = matchedEntitySets.filter(entitySet => entitySet.isSectorSet).sort(sortEntitySets);

        return (
            <div className="dropdown-metrics">
                {groupEntitySet.length > 0 && <div className="dropdown-item title">My groups</div>}
                {groupEntitySet.map(entitySet => getEntitySetElement(entitySet, user))}
                {sectorEntitySet.length > 0 && <div className="dropdown-item title">Sector groups</div>}
                {sectorEntitySet.map(entitySet => getEntitySetElement(entitySet, user))}
            </div>
        )
    }

    return (
        <div className="metric-dropdown-menu">
            <ButtonDropdown isOpen={isOpen} toggle={toggleEntitySetDropdown} className="metric-dropdown">
                {props.toggleElement}
                <DropdownMenu className="full-width">
                    <SearchInput id="metric-search-input" onChange={(text) => setSearchQuery(text)} autoFocus={true} text={searchQuery} />
                    <DropdownItem divider />
                    <UserContext.Consumer>
                        {(user) => {
                            return getMatchedEntitySets(user);
                        }}
                    </UserContext.Consumer>
                    <div className="button-wrapper">
                        <button onClick={() => { setIsModalOpen(true) }} className="hollow-button create-new-entity-set-btn" >
                            <i className="material-symbols-outlined">add</i>
                            <div>Create new group</div>
                        </button>
                    </div>
                </DropdownMenu>
            </ButtonDropdown>
            <CreateNewEntitySetModal
                isOpen={isModalOpen}
                setIsOpen={setIsModalOpen}
                entitySetType={props.entitySets[0].type}
                entitySetIcon={props.entitySets[0].GetEntitySetIcon()}
                availableInstances={props.availableInstances}
                entitySetConfigClient={props.entitySetConfigClient}
                existingEntitySetNames={props.existingEntitySetNames}
                isBarometer={props.isBarometer}
                availableEntitySets={props.availableEntitySets}
            />
            <CreateOrEditEntitySetModal isOpen={isEditModalOpen}
                setIsOpen={setIsEditModalOpen}
                entitySet={props.selectedEntitySet}
                entitySetConfigClient={props.entitySetConfigClient}
                isCreateModal={isCreateModal}
                entitySets={props.entitySets}
                isBarometer={props.isBarometer} />
        </div>
    );
}

export default EntitySetDropdownSelector