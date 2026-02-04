import React from "react";
import {IModalPage} from "./ModalPage";
import {IEntityType} from "../BrandVueApi";
import EntitySetNameInputBox from "./EntitySetNameInputBox";

interface INewEntitySetDesignModalContent extends IModalPage{
    entitySetType: IEntityType
    entitySetIcon: string
    newEntitySetName: string
    setNewEntitySetName: (entitySetName: string) => void
    existingEntitySetNames: string[]
}

const NewEntitySetDesignModalContent = (props: INewEntitySetDesignModalContent) => {
    const [hasTextError, setHasTextError] = React.useState(false)
    const [isTextInInitialState, setIsTextInInitialState] = React.useState(true)
    
    const setActionButtonIsDisabledOnValidName = () => {
        if (props.setActionButtonIsDisabled) {
            if (hasTextError || isTextInInitialState) {
                props.setActionButtonIsDisabled(true)
                return;
            }
            props.setActionButtonIsDisabled(false)
        }
    }
    
    React.useEffect(()=> {
        setActionButtonIsDisabledOnValidName()
    }, [hasTextError, isTextInInitialState])

    return (
        < div className={`new-entity-set new-entity-set-details ${props.className ? props.className : ""}`} >
            <EntitySetNameInputBox entitySetName={props.newEntitySetName}
                                   entitySetType={props.entitySetType}
                                   existingEntitySetNames={props.existingEntitySetNames}
                                   setHasError={setHasTextError}
                                   setNameInput={props.setNewEntitySetName}
                                   setIsInInitialState={setIsTextInInitialState}
                                   isCreateModal={true}/>
            <div className="logo">
                <i className="material-symbols-outlined">{props.entitySetIcon}</i>
            </div>
        </div>
    );
}

export default NewEntitySetDesignModalContent