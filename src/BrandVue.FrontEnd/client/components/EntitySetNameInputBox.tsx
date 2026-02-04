import React from "react";
import {Label, FormGroup, Input} from 'reactstrap';
import {IEntityType} from "../BrandVueApi";

interface IEntitySetNameInputBox {
    existingEntitySetNames: string[];
    entitySetName: string;
    entitySetType: IEntityType;
    setNameInput: (nameInput: string) => void
    setHasError: (hasError: boolean) => void
    setIsInInitialState: (isInInitial: boolean) => void
    isCreateModal: boolean;
}

const EntitySetNameInputBox = (props: IEntitySetNameInputBox) => {
    const initialEntitySetNameState = props.entitySetName
    const [nameInput, setNameInput] = React.useState(initialEntitySetNameState)
    const [hasBeenTyped, setHasBeenTyped] = React.useState(false)
    const protectedWord = "Custom group"
    
    const compareString = (stringOne: string, stringTwo: string) => {
        return stringOne.trim().localeCompare(stringTwo.trim(), undefined, {sensitivity: 'base'}) === 0
    }

    const doesNewEntitySetAlreadyNameExist = (text: string | undefined) => {
        let entitySetNames = props.existingEntitySetNames;

        if (!props.isCreateModal) {
            entitySetNames = entitySetNames.filter(name => name !== props.entitySetName);
        }

        return text && entitySetNames.some(entityName => compareString(entityName, text))
    }
    
    const getTextWarningPrompt = (text: string) => {
        if (text.trim() === "" && hasBeenTyped) {
            return `You must enter a ${props.entitySetType.displayNameSingular.toLocaleLowerCase()} group name`
        }
        if (text !== "" && !hasBeenTyped){
            setHasBeenTyped(true)
        }
        if (doesNewEntitySetAlreadyNameExist(text)){
            return `A ${props.entitySetType.displayNameSingular.toLocaleLowerCase()} group with this name already exists`
        }
        if (compareString(protectedWord, text)){
            return `This ${props.entitySetType.displayNameSingular.toLocaleLowerCase()} group name can't be used`
        }
        return undefined
    }
    
    const groupNameTextChangeHandler = (event: React.ChangeEvent<HTMLInputElement>) => {
        const newGroupName = event.target.value;
        props.setHasError(getTextWarningPrompt(newGroupName) != undefined)
        props.setNameInput(newGroupName)
        props.setIsInInitialState(newGroupName.trim() === initialEntitySetNameState)
        setNameInput(newGroupName);
    }
    
    return (
        <FormGroup className={getTextWarningPrompt(nameInput) !== undefined ? "red-focus" : ""}>
            <Label className="option-subheading" for="groupName">Group name</Label>
            <Input id="groupName" type="text" autoFocus={true} value={nameInput} onChange={groupNameTextChangeHandler}/>
            <div className="entity-set-validation-text">
                {getTextWarningPrompt(nameInput)}
            </div>
        </FormGroup>
    );
}

export default EntitySetNameInputBox