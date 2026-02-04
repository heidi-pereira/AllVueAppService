import React from "react";
import MultiPageModal from "./MultiPageModal";
import { EntitySet } from "../entity/EntitySet";
import { Label, FormGroup, Input } from 'reactstrap';
import ModalPage from "./ModalPage";
import { useEffect } from "react";
import { IEntitySetConfigClient } from "./EntitySetConfigClient";
import EntitySetBuilder from "../entity/EntitySetBuilder";
import { EntityInstanceColourRepository } from "../entity/EntityInstanceColourRepository";
import { UserContext } from "../GlobalContext";
import EntitySetNameInputBox from "./EntitySetNameInputBox";

interface ICreateOrEditEntitySetModal {
    isOpen: boolean;
    setIsOpen: (boolean) => void;
    entitySets: EntitySet[];
    entitySet: EntitySet;
    entitySetConfigClient: IEntitySetConfigClient;
    isCreateModal: boolean;
    isBarometer: boolean;
}

const CreateOrEditEntitySetModal = (props: ICreateOrEditEntitySetModal) => {
    const [isDeleteModalOpen, setIsDeleteModalOpen] = React.useState(false)
    const [isSaveChangesDisabled, setIsSaveChangesDisabled] = React.useState(false)
    const initialIsDefaultState = !props.isCreateModal && props.entitySet.isDefaultSet
    const initialIsSectorSetState = !props.isCreateModal && props.entitySet.isSectorSet
    const [entitySetName, setEntitySetName] = React.useState(props.entitySet.name)
    const [isDefault, setIsDefault] = React.useState(initialIsDefaultState);
    const [isSectorSet, setIsSectorSet] = React.useState(initialIsSectorSetState);
    const [hasTextError, setHasTextError] = React.useState(false)
    const [isTextInInitialState, setIsTextInInitialState] = React.useState(true)

    useEffect(() => {
        const isDefault = !props.isCreateModal ? props.entitySet.isDefaultSet : false
        const isSector = !props.isCreateModal ? props.entitySet.isSectorSet : false
        setEntitySetName(props.entitySet.name);
        setIsDefault(isDefault);
        setIsSectorSet(isSector);
        setActionButtonIsDisabledOnEditedState(isDefault, isSectorSet);
    }, [props.entitySet])

    useEffect(() => {
        setActionButtonIsDisabledOnEditedState(isDefault, isSectorSet)
    }, [hasTextError, isTextInInitialState])

    const setActionButtonIsDisabledOnEditedState = (defaultCheckbox: boolean, sectorCheckbox: boolean) => {
        const isInInitialState = props.entitySet.id !== undefined && isTextInInitialState && defaultCheckbox === initialIsDefaultState && sectorCheckbox === initialIsSectorSetState
        if (isInInitialState || hasTextError) {
            setIsSaveChangesDisabled(true)
            return;
        }
        setIsSaveChangesDisabled(false)
    }

    const onDefaultCheckboxChangeHandler = (checkboxValue: boolean) => {
        setIsDefault(checkboxValue)
        setActionButtonIsDisabledOnEditedState(checkboxValue, isSectorSet)
    }

    const onSectorSetCheckboxChangeHandler = (checkboxValue: boolean) => {
        setIsSectorSet(checkboxValue)
        setActionButtonIsDisabledOnEditedState(isDefault, checkboxValue)
    }

    const getDeleteButton = () => {
        if (!props.isCreateModal) {
            return (
                <button onClick={() => {
                    setIsDeleteModalOpen(true)
                }}>
                    <i className="material-symbols-outlined">delete</i>
                </button>
            );
        }
    }

    const onDeleteHandler = () => {
        props.entitySetConfigClient.deleteEntitySet(props.entitySet)?.then(() => {
            props.setIsOpen(false)
        })
    }

    const onUpdateHandler = () => {
        const entitySetBuilder = new EntitySetBuilder(EntityInstanceColourRepository.empty());
        entitySetBuilder.fromEntitySet(props.entitySet)
            .withIsDefaultSet(isDefault)
            .withName(entitySetName)
            .isSectorSet(isSectorSet)
            .withNoIdAverages(props.entitySet.getAverages().getAll());

        if (props.isCreateModal) {
            props.entitySetConfigClient.createEntitySet(entitySetBuilder.withResetSelfRefAverage().withId(undefined).build())
        } else {
            props.entitySetConfigClient.updateEntitySet(entitySetBuilder.build())
        }
    }

    let headerText = props.isCreateModal ? "Save as new" : "Edit";
    headerText += ` ${props.entitySet.type.displayNameSingular.toLocaleLowerCase()} group`;
    const saveButtonText = props.isCreateModal ? "Save" : "Save changes";

    const getUsingEntitySets = (): EntitySet[] => {
        return props.entitySets.filter(s => s.getAverages().getAll().find(a => a.entitySetId === props.entitySet.id));
    }

    const usingEntitySets = getUsingEntitySets();

    return (
        <>
            <MultiPageModal isOpen={props.isOpen}
                            setIsOpen={props.setIsOpen}
                            header={`${headerText}`}
                            headerButtons={getDeleteButton()}>
                <ModalPage className="entity-set-edit-modal"
                           actionButtonText={saveButtonText}
                           actionButtonHandler={onUpdateHandler}
                           actionButtonIsDisabled={isSaveChangesDisabled}>
                    <EntitySetNameInputBox entitySetName={props.entitySet.name}
                        entitySetType={props.entitySet.type}
                        existingEntitySetNames={props.entitySets.filter(es => es.id !== undefined).map(es => es.name)}
                        setHasError={setHasTextError}
                        setNameInput={setEntitySetName}
                        setIsInInitialState={setIsTextInInitialState}
                        isCreateModal={props.isCreateModal} />
                    <FormGroup>
                        <Label className="option-subheading">Sharing</Label>
                        <Input id="set-as-default" type="checkbox" className="checkbox" checked={isDefault} onChange={() => onDefaultCheckboxChangeHandler(!isDefault)}></Input>
                        <Label for="set-as-default">Set as default group</Label>
                        <div className="info-text tabbed">The default group is loaded automatically when anyone visits {props.isBarometer ? "Barometer" : "BrandVue"}</div>
                    </FormGroup>
                    <UserContext.Consumer>
                        {(user) => {
                            if (user?.isSystemAdministrator) {
                                return <FormGroup>
                                    <Label className="option-subheading">Admin</Label>
                                    <Input id="set-as-sector-group-edit" type="checkbox" className="checkbox" checked={isSectorSet} onChange={() => onSectorSetCheckboxChangeHandler(!isSectorSet)}></Input>
                                    <Label for="set-as-sector-group-edit">Set as sector group</Label>
                                    <div className="info-text tabbed">Only Savanta system administrators can create or edit sector groups</div>
                                </FormGroup>
                            }
                        }}
                    </UserContext.Consumer>
                </ModalPage>
            </MultiPageModal>
            {!props.isCreateModal && <MultiPageModal isOpen={isDeleteModalOpen}
                                                     setIsOpen={setIsDeleteModalOpen}
                                                     header={`Delete ${props.entitySet?.type.displayNameSingular.toLocaleLowerCase()} group?`}>
                <ModalPage className="entity-set-delete-modal"
                           actionButtonText="Delete"
                           actionButtonCss="negative-button delay-click"
                           actionButtonHandler={onDeleteHandler}>
                    <Label className="label">{`Are you sure you want to delete the ${props.entitySet?.name} group?`}</Label>
                    {usingEntitySets.length > 0
                        && <div>
                            <div className="entity-set-validation-text">Deleting this set will also remove it as an average for <b>{usingEntitySets.map(s => s.name).join(', ')}</b></div>
                        </div>}
                    <div className="entity-set-validation-text">
                        This will affect all users and cannot be undone
                    </div>
                </ModalPage>
            </MultiPageModal>}
        </>
    );
}

export default CreateOrEditEntitySetModal;
