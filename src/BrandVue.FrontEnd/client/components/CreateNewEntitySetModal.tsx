import React from "react";
import MultiPageModal from "./MultiPageModal";
import {EntitySet} from "../entity/EntitySet";
import NewEntitySetDesignModalContent from "./NewEntitySetDesignModalContent";
import NewEntitySetEntitySelectorModalContent from "./NewEntitySetEntitySelectorModalContent";
import {EntityInstance} from "../entity/EntityInstance";
import NewEntitySetOptionsModalContent from "./NewEntitySetOptionsModalContent";
import {useEffect} from "react";
import {EntityInstanceGroup} from "../entity/EntityInstanceGroup";
import {IEntityType} from "../BrandVueApi";
import {IEntitySetConfigClient} from "./EntitySetConfigClient";
import { EntitySetAverageGroup } from "../entity/EntitySetAverageGroup";
import {EntitySetAverage} from "../entity/EntitySetAverage";


interface ICreateNewEntitySetModal {
    isOpen: boolean;
    setIsOpen: (boolean) => void;
    entitySetType: IEntityType;
    entitySetIcon: string;
    availableInstances: EntityInstance[];
    entitySetConfigClient: IEntitySetConfigClient;
    existingEntitySetNames: string[];
    isBarometer: boolean;
    availableEntitySets: EntitySet[];
}

const CreateNewEntitySetModal = (props: ICreateNewEntitySetModal) => {
    const [newEntitySetName, setNewEntitySetName] = React.useState("");
    const [newEntitySetInstances, setNewEntitySetInstances] = React.useState<EntityInstance[]>([])
    const [newEntitySetMainInstance, setNewEntitySetMainInstance] = React.useState<EntityInstance | undefined>(undefined)
    const [newAverage, setNewAverage] = React.useState<EntitySetAverage | undefined>(undefined)
    const [isDefault, setIsDefault] = React.useState(false);
    const [isSectorSet, setIsSectorSet] = React.useState(false);

    useEffect(() => {
        if (!props.isOpen){
            setNewEntitySetName("");
            setNewEntitySetInstances([]);
            setNewEntitySetMainInstance(undefined);
            setIsDefault(false);
            setIsSectorSet(false);
        }
    }, [props.isOpen])
    
    const createNewEntitySetHandler = () => {
        let instances = new EntityInstanceGroup(newEntitySetInstances);
        const newEntitySet = new EntitySet(undefined, props.entitySetType, newEntitySetName.trim(), instances, isSectorSet, isDefault, newEntitySetMainInstance ?? newEntitySetInstances[0], new EntitySetAverageGroup(newAverage ? [newAverage] : []))
        props.entitySetConfigClient.createEntitySet(newEntitySet)
    }

    return(
        <MultiPageModal isOpen={props.isOpen}
                        setIsOpen={props.setIsOpen}
                        header={`Create ${props.entitySetType.displayNameSingular.toLocaleLowerCase()} group`}>
            <NewEntitySetDesignModalContent entitySetType={props.entitySetType}
                                            entitySetIcon={props.entitySetIcon}
                                            newEntitySetName={newEntitySetName}
                                            setNewEntitySetName={setNewEntitySetName}
                                            existingEntitySetNames={props.existingEntitySetNames}/>
            <NewEntitySetEntitySelectorModalContent entitySetType={props.entitySetType}
                                                    availableInstances={props.availableInstances}
                                                    selectedEntityInstances={newEntitySetInstances}
                                                    setSelectedEntityInstances={setNewEntitySetInstances}/>
            <NewEntitySetOptionsModalContent entitySetType={props.entitySetType}
                                          selectedEntityInstances={newEntitySetInstances}
                                          mainInstance={newEntitySetMainInstance}
                                          setMainInstance={setNewEntitySetMainInstance}
                                          isDefault={isDefault}
                                          setIsDefault={setIsDefault}
                                          isSectorSet={isSectorSet}
                                          setIsSectorSet={setIsSectorSet}
                                          actionButtonHandler={createNewEntitySetHandler}
                                          actionButtonText="Create group"
                                          isBarometer={props.isBarometer} 
                                          availableEntitySets={props.availableEntitySets} 
                                          average={newAverage}
                                          setAverage={setNewAverage}/>
        </MultiPageModal>
    );
}

export default CreateNewEntitySetModal;
