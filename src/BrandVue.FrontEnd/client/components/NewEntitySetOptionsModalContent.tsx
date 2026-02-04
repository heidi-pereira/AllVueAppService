import React from "react";
import {IModalPage} from "./ModalPage";
import {EntityInstance} from "../entity/EntityInstance";
import { FormGroup, Label, ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem, Input } from 'reactstrap';
import {IEntityType} from "../BrandVueApi";
import SearchInput from "./SearchInput";
import { UserContext } from "../GlobalContext";
import {EntitySetAverage} from "../entity/EntitySetAverage";
import {EntitySet} from "../entity/EntitySet";

interface INewEntitySetOptionsModalContent extends IModalPage{
    entitySetType: IEntityType;
    selectedEntityInstances: EntityInstance[];
    mainInstance: EntityInstance | undefined;
    setMainInstance: (mainInstance: EntityInstance | undefined) => void;
    isDefault: boolean;
    setIsDefault: (isDefault: boolean) => void;
    isSectorSet: boolean
    setIsSectorSet: (isSectorSet: boolean) => void;
    isBarometer: boolean;
    availableEntitySets: EntitySet[];
    average: EntitySetAverage | undefined;
    setAverage: (average: EntitySetAverage | undefined) => void;
}

const NewEntitySetOptionsModalContent = (props: INewEntitySetOptionsModalContent) => {
    const [isMainInstanceDropdownOpen, setIsMainInstanceDropdownOpen] = React.useState(false)
    const [isAverageDropdownOpen, setIsAverageDropdownOpen] = React.useState(false)
    const [searchQuery, setSearchQuery] = React.useState<string>("");
    const selectNoneText = `All ${props.entitySetType.displayNamePlural.toLocaleLowerCase()}`;
    const AVERAGE_ID_NULL_VALUE = 0;
    
    const getAverageChildName = (average: EntitySetAverage | undefined) => {
        if (average)
            return props.availableEntitySets.find(e => e.id === average.entitySetId)?.name
    }
    
    const getSectorSets = () => {
        return props.availableEntitySets.filter(e => e.isSectorSet)
    }
    
    const getNonSectorSets = () => {
        return props.availableEntitySets.filter(e => !e.isSectorSet)
    }

    const toggleMainInstanceDropdown = () => {
        if (isMainInstanceDropdownOpen){
            setSearchQuery("")
        }
        setIsMainInstanceDropdownOpen(!isMainInstanceDropdownOpen)
    }

    const toggleAverageDropdown = () => {
        if (isAverageDropdownOpen){
            setSearchQuery("")
        }
        setIsAverageDropdownOpen(!isAverageDropdownOpen)
    }

    const mainInstanceSelectorChangeHandler = (text: string) => {
        setActionButtonIsDisabledIfBrandAndEmptyOrUndefinedString(text);
        props.setMainInstance(props.selectedEntityInstances.find(e => e.name === text))
    }

    const setActionButtonIsDisabledIfBrandAndEmptyOrUndefinedString = (text: string | undefined) => {
        if (props.entitySetType.isBrand && props.setActionButtonIsDisabled) {
            if (text === undefined || text === "") {
                props.setActionButtonIsDisabled(true)
                return;
            }
            props.setActionButtonIsDisabled(false)
        }
    }

    setActionButtonIsDisabledIfBrandAndEmptyOrUndefinedString(props.mainInstance ? props.mainInstance.name : "")

    const getEntityInstanceDropdownItem = (entityInstance: EntityInstance) => {
        const displayText = entityInstance.name;
        return (
            <DropdownItem key={entityInstance.name} onClick={() => mainInstanceSelectorChangeHandler(entityInstance.name)} title={entityInstance.name} className="tabbed">
                <div className="name-container">
                    <span className='title' title={displayText}>{displayText}</span>
                </div>
            </DropdownItem>
        );
    }

    const getSearchedEntityInstances = () => {
        let searchedInstances = [...props.selectedEntityInstances];

        if (searchQuery && searchQuery.trim() != '') {
            searchedInstances = searchedInstances.filter(m =>
                m.name.toLowerCase().includes(searchQuery.toLowerCase()));
        }

        searchedInstances = searchedInstances.sort((a, b) => a.name.localeCompare(b.name));
        return (
            <div className="dropdown-metrics">
                {searchedInstances.map(entityInstance => getEntityInstanceDropdownItem(entityInstance))}
            </div>
        )
    }

    const getAverageDropdownItem = (entitySet: EntitySet) => {
        if (entitySet.id && entitySet.name) {
            const averageName = entitySet.name
            const average = new EntitySetAverage(entitySet.id, false, AVERAGE_ID_NULL_VALUE)
            return (
                <DropdownItem key={averageName} onClick={() => {
                    props.setAverage(average)
                }} title={averageName} className="tabbed">
                    <div className="name-container">
                        <span className='title' title={averageName}>{averageName}</span>
                    </div>
                </DropdownItem>
            );
        }
    }
    
    const getSearchedAverages = (availableEntitySets: EntitySet[]) => {
        let searchedEntitySet = availableEntitySets;

        if (searchQuery && searchQuery.trim() != '') {
            searchedEntitySet = searchedEntitySet.filter(e =>
                e.name.toLowerCase().includes(searchQuery.toLowerCase()));
        }

        return searchedEntitySet.map(entitySet => getAverageDropdownItem(entitySet))
    }

    return (
        < div className={`new-entity-set new-entity-set-options ${props.className ? props.className : ""}`} >
            <FormGroup>
                {props.entitySetType.isBrand && <div>
                    <Label className="option-subheading">{`Main ${props.entitySetType.displayNameSingular.toLocaleLowerCase()}`}</Label>
                    <ButtonDropdown isOpen={isMainInstanceDropdownOpen} toggle={toggleMainInstanceDropdown} className="metric-dropdown">
                        <DropdownToggle className="metric-selector-toggle toggle-button">
                            <span>{props.mainInstance ? props.mainInstance.name : ""}</span>
                            <i className="material-symbols-outlined">arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu className="full-width">
                            <SearchInput id="metric-search-input" onChange={(text) => setSearchQuery(text)} autoFocus={true} text={searchQuery} />
                            <DropdownItem divider />
                            {getSearchedEntityInstances()}
                        </DropdownMenu>
                    </ButtonDropdown>
                    <div className="info-text">The main {props.entitySetType.displayNameSingular.toLocaleLowerCase()} in a group is prioritised on charts and in reports. Usually it’s best to set it to your {props.entitySetType.displayNameSingular.toLocaleLowerCase()}.</div>
                </div>}
            </FormGroup>
            <FormGroup>
                <div>
                    <Label className="option-subheading">Average</Label>
                    <ButtonDropdown isOpen={isAverageDropdownOpen} toggle={toggleAverageDropdown} className="metric-dropdown">
                        <DropdownToggle className="metric-selector-toggle toggle-button">
                            <span>{getAverageChildName(props.average) ? getAverageChildName(props.average) : selectNoneText}</span>
                            <i className="material-symbols-outlined">arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu className="full-width">
                            {selectNoneText &&
                                <>
                                    <DropdownItem onClick={() => props.setAverage(undefined)}>
                                        <div className="name-container">
                                            <span className='title'>{selectNoneText}</span>
                                        </div>
                                    </DropdownItem>
                                    <DropdownItem divider />
                                </>
                            }
                            <SearchInput id="metric-search-input" onChange={(text) => setSearchQuery(text)} autoFocus={true} text={searchQuery} />
                            <DropdownItem divider />
                            <div className="dropdown-metrics">
                                {getNonSectorSets().length > 0 && <div className="dropdown-item title">{`My ${props.entitySetType.displayNameSingular.toLocaleLowerCase()} groups`}</div>}
                                {getSearchedAverages(getNonSectorSets())}
                                {getSectorSets().length > 0 && <div className="dropdown-item title">Sector groups</div>}
                                {getSearchedAverages(getSectorSets())}
                            </div>
                        </DropdownMenu>
                    </ButtonDropdown>
                    <div className="info-text">{`Averages are shown on charts as a result for a group of ${props.entitySetType.displayNamePlural.toLocaleLowerCase()}. You can add more averages once the group is created`}</div>
                </div>
            </FormGroup>
            <FormGroup>
                <Label className="option-subheading">Sharing</Label>
                <Input id="set-as-default-new" type="checkbox" className="checkbox" checked={props.isDefault} onChange={() => props.setIsDefault(!props.isDefault)}></Input>
                <Label for="set-as-default-new">Set as default group</Label>
                <div className="info-text tabbed">Groups you create are shared with <span className="bold">everyone.</span> The default group is loaded automatically when anyone visits {props.isBarometer ? "Barometer" : "BrandVue"}</div>
            </FormGroup>
            <UserContext.Consumer>
                {(user) => {
                    if (user?.isSystemAdministrator) {
                        return <FormGroup>
                            <Label className="option-subheading">Admin</Label>
                            <Input id="set-as-sector-group-new" type="checkbox" className="checkbox" checked={props.isSectorSet} onChange={() => props.setIsSectorSet(!props.isSectorSet)}></Input>
                            <Label for="set-as-sector-group-new">Set as sector group</Label>
                            <div className="info-text tabbed">Only Savanta system administrators can create or edit sector groups</div>
                        </FormGroup>
                    }
                }}
            </UserContext.Consumer>
        </div>
    );
}

export default NewEntitySetOptionsModalContent