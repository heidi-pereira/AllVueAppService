import React from "react";
import { Modal, ModalFooter, ModalHeader, ModalBody, Button } from "reactstrap";
import { EntityInstance } from "../../../../../entity/EntityInstance";
import { WeightingButtonSimple } from "../../../../../pages/WeightingPlansControls/WeightingButton";
import SearchableCheckboxList, { ListItem } from "../../../../SearchableCheckboxList";
import style from './CopyToSiblingsModal.module.less'

interface ICopyToSiblingsModalProps {
    isOpen: boolean;
    activeInstance: EntityInstance;
    closeModal: () => void;
    confirm: (selectedEntityIds: number[]) => void;
    entityInstances: EntityInstance[];
    entityInstancesWithNoWeightings: EntityInstance[];
    flattenToRim: boolean;
}

const CopyToSiblingsModal: React.FunctionComponent<ICopyToSiblingsModalProps> = (props) => {

    const generateListItemFromEntity = (entityInstance: EntityInstance): ListItem => {

        return {
            Id: entityInstance.id,
            Name: entityInstance.name ?? "",
            Keywords: []
        };
    }

    const availableSiblingList = (noChildren: boolean) => {
        if (noChildren) {
            return props.entityInstancesWithNoWeightings.map(generateListItemFromEntity).filter(li => li !== null);
        }
        return props.entityInstances.map(generateListItemFromEntity).filter(li => li !== null);
    }

    const [selectedTargets, setSelectedTargets] = React.useState<ListItem[]>(availableSiblingList(true));
    const [displayOnlyEmptySiblings, setDisplayOnlyEmptySiblings] = React.useState(true);

    return (
        <Modal isOpen={props.isOpen} toggle={props.closeModal} centered={true} className={`modal-dialog-centered content-modal modal-copy ${style.weightingPlansConfigurationPage}`}>
            <ModalHeader>
                <div className="settings-modal-header">
                    <div className="close-icon">
                        <button type="button" className="btn btn-close" onClick={props.closeModal}>                         
                        </button>
                    </div>
                    <div className="set-name">Copy {props.flattenToRim?"& flatten ":"" }weighting</div>
                </div>
            </ModalHeader>
            <ModalBody className={style.modalBody}>
                <p>Copy weightings from <strong>{props.activeInstance.name}</strong> to:</p>
                <SearchableCheckboxList
                    availableItems={availableSiblingList(displayOnlyEmptySiblings)}
                    selectedItems={selectedTargets}
                    updateSelectedItems={setSelectedTargets}
                    />
                <p className={style.textDetails} >This will overwrite any existing weightings for all selected siblings</p>
                <p>
                    <input id="set-as-default" type="checkbox" className="checkbox" checked={displayOnlyEmptySiblings} onChange={() => setDisplayOnlyEmptySiblings(!displayOnlyEmptySiblings)}></input>
                    <label htmlFor="set-as-default">Only show siblings with no weightings</label>
                </p>
            </ModalBody>
            <ModalFooter>
                <Button className="secondary-button" onClick={props.closeModal}>Cancel</Button>
                <WeightingButtonSimple disabled={selectedTargets.length == 0}
                    className='primary-button'
                    buttonIcon='save'
                    buttonText='Save changes'
                    onClick={() => props.confirm(selectedTargets.map(t => t.Id))}
                    toolTipText={`Copy ${props.flattenToRim?"& flatten ":"" }weighting`} />
            </ModalFooter>
        </Modal>
    );
};

export default CopyToSiblingsModal;