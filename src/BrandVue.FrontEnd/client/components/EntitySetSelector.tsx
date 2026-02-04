import React from "react";
import { IApplicationUser, IEntityType } from "../BrandVueApi";
import EntitySetDropdownSelector from "./dropdown/EntitySetDropdownSelector";
import { EntitySet } from "../entity/EntitySet";
import { DropdownToggle } from "reactstrap";
import {EntityInstance} from "../entity/EntityInstance";
import EntityInstancesSelectList from "./EntityInstancesSelectList";
import EntitySetList from "./EntitySetList";
import SidePanelHeader from "./SidePanelHeader";
import { EntityInstanceColourRepository } from "../entity/EntityInstanceColourRepository";
import ButtonWithDropdown from "./dropdown/ButtonWithDropdown";
import MultiPageModal from "./MultiPageModal";
import ModalPage from "./ModalPage";
import EntitySetConfigClient from "./EntitySetConfigClient";
import CreateOrEditEntitySetModal from "./CreateOrEditEntitySetModal";
import EntitySetBuilder from "../entity/EntitySetBuilder";
import { UserContext } from "../GlobalContext";
import { EntitySetAverage } from "../entity/EntitySetAverage";
import EntitySetAverageSelectList from "./EntitySetAverageSelectList";
import { ProductConfiguration } from "../ProductConfiguration";
import { MixPanel } from "./mixpanel/MixPanel";
import {useDispatch} from "react-redux";
import {setActiveEntitySet} from "../state/entitySelectionSlice";
import { useAllActiveEntitySetsWithDefault } from "../state/entitySelectionHooks";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";

interface IEntitySetSelector {
    visible: boolean;
    closeSelector(): void;
    entityType: IEntityType;
    entitySets: EntitySet[];
    availableInstances: EntityInstance[];
    isBarometer: boolean;
    isColourConfigEnabled: boolean;
    productConfiguration: ProductConfiguration;
}

const enum subPanelContentType {
    entityInstanceSelector,
    averageSelector
}

const EntitySetSelector = (props: IEntitySetSelector) => {
    const selectedEntitySet = useAllActiveEntitySetsWithDefault().find(x=>x.type.identifier == props.entityType.identifier)!;
    const [subPanelOpen, setSubPanelOpen] = React.useState(false);
    const [selectedInstances, setSelectedInstances] = React.useState<EntityInstance[]>(selectedEntitySet.getInstances().getAll());
    const [saveChangesModalVisible, setSaveChangesModalVisible] = React.useState(false);
    const [saveAsNewEditModalVisible, setSaveAsNewEditModalVisible] = React.useState(false);
    const [unmodifiedEntitySet, setUnmodifiedEntitySet] = React.useState<EntitySet>(props.entitySets.find(e => e.id === selectedEntitySet.id)!);
    const [subPanelContent, setSubPanelContent] = React.useState<subPanelContentType>(subPanelContentType.entityInstanceSelector);
    const dispatch = useDispatch();
    const shouldShowColourEditor = props.entityType.isBrand && props.isColourConfigEnabled;
    const selectEntitySet = (entitySet: EntitySet) => {
        setUnmodifiedEntitySet(entitySet);
        dispatch(setActiveEntitySet({ entitySet: entitySet }));
    }
    const subsetId = useAppSelector(selectSubsetId);

    const entitySetConfigClient = EntitySetConfigClient(subsetId, props.entityType, selectEntitySet)

    const entitySetEdited = () => {
        const initialInstanceIds = unmodifiedEntitySet.getInstances().getAll().map(i => i.id);
        const currentInstanceIds = selectedEntitySet.getInstances().getAll().map(i => i.id);
        const initialAverageEntitySetIds = unmodifiedEntitySet.getAverages().getAll().map(a => a.entitySetId);
        const currentAverageEntitySetIds = selectedEntitySet.getAverages().getAll().map(a => a.entitySetId);

        return (!(initialInstanceIds.every(i => currentInstanceIds.includes(i)) &&
            currentInstanceIds.every(i => initialInstanceIds.includes(i)) &&
            initialAverageEntitySetIds.every(i => currentAverageEntitySetIds.includes(i)) &&
            currentAverageEntitySetIds.every(i => initialAverageEntitySetIds.includes(i)) &&
            unmodifiedEntitySet.mainInstance?.id === selectedEntitySet.mainInstance?.id));
    }

    React.useEffect(() => {
            if (!props.visible) {
                setSubPanelOpen(false);
            }
        },
        [props.visible]);

    React.useEffect(() => {
        setUnmodifiedEntitySet(props.entitySets.find(e => e.id === selectedEntitySet.id) ?? selectedEntitySet)
    }, [selectedEntitySet.id, selectedEntitySet.type])

    const getEntitySetDisplayText = () => {
        return unmodifiedEntitySet.name;
    }

    const getEntitySetDropdownToggleButton = () => {
        return (
            <DropdownToggle className="metric-selector-toggle toggle-button">
                <i className="material-symbols-outlined">{selectedEntitySet.GetEntitySetIcon()}</i>
                <div className="title">{getEntitySetDisplayText()}</div>
                <div className="textbox-tag">{entitySetEdited() && "Edited"}</div>
                <i className="material-symbols-outlined">arrow_drop_down</i>
            </DropdownToggle>
        );
    }

    React.useEffect(() => {
        setSelectedInstances(selectedEntitySet.getInstances().getAll());
    },
        [selectedEntitySet]);

    const removeEntityInstance = (entity: EntityInstance) => {
        const entityInstanceListClone = selectedEntitySet.getInstances().clone();
        let instances = entityInstanceListClone.getAll();
        if (instances.length < 1){
            return;
        }
        let newMainInstance = selectedEntitySet.mainInstance ?? instances[0];
        if (newMainInstance && entity.name === newMainInstance.name){
            let index = instances.map(e => e.name).indexOf(newMainInstance.name)
            index = index > 0? index - 1 : 1;
            newMainInstance = instances[index];
        }
        entityInstanceListClone.removeInstance(entity);
        updateSelectedEntityInstances(entityInstanceListClone.getAll(), newMainInstance);
    }

    const setEntityInstanceAsActive = (entity: EntityInstance) => {
        const entityInstanceListClone = selectedEntitySet.getInstances().clone();
        updateSelectedEntityInstances(entityInstanceListClone.getAll(), entity.clone());
    }

    const getAddButtonName = (entityType: IEntityType) => {
        return `Add ${entityType.displayNamePlural.toLowerCase()}`;
    }

    const updateSelectedEntityInstances = (selected: EntityInstance[], mainInstance: EntityInstance) => {
        const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
        const newSet = entitySetBuilder.fromEntitySet(selectedEntitySet)
            .withInstances(selected)
            .withMainInstance(mainInstance)
            .build();
        dispatch(setActiveEntitySet({ entitySet: newSet }));
        setSubPanelOpen(false);
    }

    const updateSelectedEntitySetAverages = (selected: EntitySetAverage[]) => {
        const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
        const newSet = entitySetBuilder.fromEntitySet(selectedEntitySet)
            .withAverages(selected)
            .build()
        dispatch(setActiveEntitySet({ entitySet: newSet }));
        setSubPanelOpen(false);
    }

    const getAddDropdownToggleButton = () => {
        return (
            <DropdownToggle className="hollow-button">
                <i className="material-symbols-outlined">add</i>
                <div className="title">Add</div>
                <i className="material-symbols-outlined">arrow_drop_down</i>
            </DropdownToggle>
        );
    }

    const getSaveDropdownToggleButton = () => {
        return (
            <DropdownToggle className="hollow-button">
                <div className="title">Save</div>
                <i className="material-symbols-outlined">arrow_drop_down</i>
            </DropdownToggle>
        );
    }

    const closeSubPanel = () => {
        setSelectedInstances(selectedEntitySet.getInstances().getAll());
        setSubPanelOpen(false);
    }

    const getAvailableInstances = () => {
        return props.availableInstances
            .filter(entity => !selectedEntitySet.getInstances()
                .getAll().map(e => e.name).includes(entity.name));
    }

    const newInstancesSelected = selectedInstances.some(i => !selectedEntitySet
        .getInstances().getAll().map(e => e.id).includes(i.id));

    const openSaveChangesModal = () => {
        MixPanel.track("setSavedTo");
        setSaveChangesModalVisible(true);
    }

    const openSaveAsNewGroupModal = () => {
        MixPanel.track("setSavedAsNew");
        setSaveAsNewEditModalVisible(true);
    }

    const openEntityInstanceSelectorPanel = () => {
        setSubPanelContent(subPanelContentType.entityInstanceSelector);
        setSubPanelOpen(true);
    }

    const openAverageSelectorPanel = () => {
        setSubPanelContent(subPanelContentType.averageSelector);
        setSubPanelOpen(true);
    }

    const getAddButtonDropdownItems = (entityType: IEntityType) => {
        return [{ itemName: getAddButtonName(entityType), onClick: openEntityInstanceSelectorPanel },
            { itemName: "Add average", onClick: openAverageSelectorPanel }];
    }

    const getSaveButtonDropdownItems = (entitySet: EntitySet, entityType: IEntityType, user: IApplicationUser | null) => {
        const saveToExistingGroupText = `Save changes to ${entitySet.name}`;
        const saveAsNewGroupText = `Save as new ${entityType.displayNameSingular} group`;

        const saveToExistingGroupItem = { itemName: saveToExistingGroupText, onClick: openSaveChangesModal };
        const saveAsNewGroupItem = { itemName: saveAsNewGroupText, onClick: openSaveAsNewGroupModal };

        if (selectedEntitySet.isCustomSet || (selectedEntitySet.isSectorSet && !user?.isSystemAdministrator) || selectedEntitySet.isFallbackSet) {
            return [saveAsNewGroupItem];
        }

        return [saveToExistingGroupItem, saveAsNewGroupItem];
    }

    const onSaveHandler = async () => {
        const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
        const setToSave = entitySetBuilder.fromEntitySet(selectedEntitySet)
            .withName(unmodifiedEntitySet.name)
            .build();
        await setToSave.id ? entitySetConfigClient.updateEntitySet(setToSave) : entitySetConfigClient.createEntitySet(setToSave);
        setSaveChangesModalVisible(false);
    }

    const saveChangesModal = () => {
        return (
            <MultiPageModal isOpen={saveChangesModalVisible}
                            setIsOpen={setSaveChangesModalVisible}
                            header={`Save changes to ${selectedEntitySet.type.displayNameSingular.toLocaleLowerCase()} group`}>
                <ModalPage className={"entity-set-save"} actionButtonText={"Save changes"} actionButtonCss="primary-button delay-click"
                           actionButtonHandler={onSaveHandler}>
                    <p>Are you sure you want to save your changes to the <strong>{unmodifiedEntitySet.name}</strong> group?</p>
                    <div className="warning"><i className="material-symbols-outlined">info</i>This will change the group for <strong>all users.</strong></div>
                </ModalPage>
            </MultiPageModal>
        );
    };

    const saveAsNewEditEntitySetModal = () => {
        return (
            <CreateOrEditEntitySetModal isOpen={saveAsNewEditModalVisible}
                                        setIsOpen={setSaveAsNewEditModalVisible}
                                        entitySet={selectedEntitySet}
                                        entitySetConfigClient={entitySetConfigClient}
                                        entitySets={props.entitySets}
                                        isCreateModal={true}
                                        isBarometer={props.isBarometer} />
        );
    }

    const getEntitySetNames = () => {
        return props.entitySets.map(entitySet => entitySet.name)
    }

    const getSubPanelHeaderText = (contentType: subPanelContentType) => {
        if (contentType === subPanelContentType.averageSelector)
            return "Add average";

        return `Add ${props.entityType.displayNamePlural}`;
    }

    const getSelfRefAverageEntitySet = (entitySet: EntitySet) => {
        let SelfRefAverageName = `All ${entitySet.type.displayNamePlural.toLocaleLowerCase()} in the group`;
        if (entitySet.type.isBrand) {
            SelfRefAverageName += ` (includes main ${entitySet.type.displayNameSingular.toLocaleLowerCase()})`
        }
        const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
        return entitySetBuilder.fromEntitySet(entitySet)
            .withName(SelfRefAverageName)
            .build();
    }

    const entitySetsWithSelfRefName = () => {
        return props.entitySets.map(es => es.id === selectedEntitySet.id ? getSelfRefAverageEntitySet(es) : es);
    }

    const averagesForSelectedEntitySet = selectedEntitySet.getAverages().getAll();

    const availableEntitySetsForAverage = () => {
        const selectedEntitySetAverageIds = averagesForSelectedEntitySet.map(av => av.entitySetId);
        return entitySetsWithSelfRefName().filter(es => es.id && !selectedEntitySetAverageIds.includes(es.id));
    }

    const addToSelectedEntitySet = (entitySet: EntitySet) => {
        MixPanel.track("averageAdded");
        const newAverage = new EntitySetAverage(entitySet.id!);
        const entitySetAverageGroupClone = selectedEntitySet.getAverages().clone();
        entitySetAverageGroupClone.addAverage(newAverage);
        updateSelectedEntitySetAverages(entitySetAverageGroupClone.getAll());
    }

    const getSubPanelContent = (contentType: subPanelContentType) => {
        if (contentType === subPanelContentType.averageSelector)
            return <div className="content">
                <EntitySetAverageSelectList
                    selectedEntitySet={selectedEntitySet}
                    availableEntitySetsForAverage={availableEntitySetsForAverage()}
                    addEntitySetToAverages={addToSelectedEntitySet}
                    entityType={selectedEntitySet.type} />
                    </div>;

        return (
            <div className="content">
                <EntityInstancesSelectList
                    availableEntityInstances={getAvailableInstances()}
                    selectedEntityInstances={selectedInstances}
                    updateSelectedEntityInstances={setSelectedInstances}
                    entityInstanceIdentifierKey={"Choose-entities"}
                    entityType={selectedEntitySet.type} />
                <div className="control-buttons add-brand-buttons">
                    <button className="modal-button primary-button" disabled={!newInstancesSelected} onClick={() => updateSelectedEntityInstances(selectedInstances, selectedEntitySet.mainInstance ?? selectedInstances[0])}>{getAddButtonName(props.entityType)}</button>
                    <button className="modal-button secondary-button" onClick={closeSubPanel}>Cancel</button>
                </div>
            </div>
        );
    }

    const getEntitySetName = (entitySetId: number) => {
        const entitySet = entitySetsWithSelfRefName().find(es => es.id === entitySetId);
        return entitySet ? entitySet.name : "";
    }

    const removeAverage = (entitySetAverage: EntitySetAverage) => {
        MixPanel.track("averageRemoved");
        updateSelectedEntitySetAverages(selectedEntitySet.getAverages().getAll().filter(a => a.entitySetId != entitySetAverage.entitySetId));
    }

    return (
        <div className="entity-set-selector">
            {saveChangesModal()}
            {saveAsNewEditEntitySetModal()}
            <div className={`selector-main ${subPanelOpen ? "hide" : ""}`}>
                <div className={`selector-main-content ${subPanelOpen ? "hide" : ""}`}>
                    <div className="content">
                        <label className="entity-set-selector-heading">{`${props.entityType.displayNameSingular} group`}</label>
                        <EntitySetDropdownSelector
                            toggleElement={getEntitySetDropdownToggleButton()}
                            entitySets={props.entitySets}
                            selectEntitySet={selectEntitySet}
                            availableInstances={props.availableInstances}
                            existingEntitySetNames={getEntitySetNames()}
                            selectedEntitySet={selectedEntitySet}
                            entitySetConfigClient={entitySetConfigClient}
                            isBarometer={props.isBarometer}
                            availableEntitySets={props.entitySets}
                        />
                        <div className="control-buttons">
                            <ButtonWithDropdown
                                dropdownItems={getAddButtonDropdownItems(props.entityType)}
                                toggleElement={getAddDropdownToggleButton()}
                            />
                            {entitySetEdited() &&
                                <UserContext.Consumer>
                                    {(user) => {
                                        return <ButtonWithDropdown
                                            dropdownItems={getSaveButtonDropdownItems(unmodifiedEntitySet, props.entityType, user)}
                                            toggleElement={getSaveDropdownToggleButton()}
                                        />
                                    }}
                                </UserContext.Consumer>
                            }
                        </div>
                        <EntitySetList
                            entitySet={selectedEntitySet}
                            getEntitySetName={getEntitySetName}
                            removeEntityInstance={removeEntityInstance}
                            setEntityInstanceAsActive={setEntityInstanceAsActive}
                            removeAverage={removeAverage}
                        />
                        {shouldShowColourEditor && <div>
                            <label className="entity-set-selector-heading colour-heading">Colours</label>
                            <a className="hollow-button edit-entity-colours-btn" href={`${props.productConfiguration.appBasePath}/ui/colour-configuration`}>
                                <i className="material-symbols-outlined">edit</i>
                                {`Edit ${props.entityType.displayNameSingular.toLocaleLowerCase()} colours`}
                            </a>
                        </div>}
                    </div>
                </div>
            </div>
            <div className={`entity-instance-selector ${subPanelOpen ? "visible" : ""}`}>
                <SidePanelHeader returnButtonHandler={() => setSubPanelOpen(false)}>
                    {getSubPanelHeaderText(subPanelContent)}
                </SidePanelHeader>
                {getSubPanelContent(subPanelContent)}
            </div>
        </div>
    );
}

export default EntitySetSelector;