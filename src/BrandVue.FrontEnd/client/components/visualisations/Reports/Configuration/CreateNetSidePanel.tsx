import React from "react";
import {useState} from "react";
import {Metric} from "../../../../metrics/metric";
import {EntityInstance} from "../../../../entity/EntityInstance";
import {IConfigureNets} from "./ConfigureNets";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { PageHandler } from "../../../PageHandler";

interface ICreateNettSidePanelProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    selectedMetric: Metric;
    isSidePanelOpen: boolean;
    setIsSidePanelOpen: (boolean) => void;
    configureNets: IConfigureNets;
}

const CreateNetSidePanel = (props: ICreateNettSidePanelProps) => {
    const [netName, setNetName] = useState("");
    const [selectedInstanceIds, setSelectedInstanceIds] = useState<number[]>([]);

    const focusRef = React.useRef<HTMLInputElement | null>(null);

    const selectedEntityInstances = props.configureNets.availableEntityInstances.filter(i => selectedInstanceIds == null || selectedInstanceIds.includes(i.id));

    const doesNetNameAlreadyExist = () => {
        if (netName !== "") {
            return props.configureNets.availableEntityInstances
                .some(e => e.name.localeCompare(netName, undefined, {sensitivity: 'base'}) === 0)
        }
    }

    const getValidationText = () => {
        if (doesNetNameAlreadyExist()){
            return "Invalid name, an answer or net with this name already exists"
        }
        return undefined
    }

    const onCloseHandler = () => {
        props.setIsSidePanelOpen(false)
        setNetName("")
        setSelectedInstanceIds([])
    }

    const createNet = () => {
        props.configureNets.createNet(netName, props.selectedMetric, selectedEntityInstances).then(() =>{
            props.setIsSidePanelOpen(false)}
        )
    }

    React.useEffect(() => {
        if (props.isSidePanelOpen) {
            setTimeout(() => focusRef?.current?.focus(), 250);
        }
    }, [props.isSidePanelOpen])

    const getNetNameInput = () => {
        return (
            <div className={getValidationText() ? "net-name-input red-focus" : "net-name-input"}>
                <label className="category-label">Name</label>
                <input type="text"
                    className="input text-input display-name-input"
                    value={netName}
                    onChange={(e) => setNetName(e.target.value)}
                    placeholder="eg NET: Agree"
                    ref={focusRef}
                />
                <div className="create-net-validation-text">
                    {getValidationText()}
                </div>
            </div>
        )
    }

    const toggleEntity = (entityInstance: EntityInstance) => {
        const newInstanceIds = [...(selectedInstanceIds ?? props.configureNets.availableEntityInstances.map(i => i.id))];
        if(newInstanceIds.includes(entityInstance.id)) {
            const index = newInstanceIds.findIndex(id => id === entityInstance.id);
            newInstanceIds.splice(index, 1);
        } else {
            newInstanceIds.push(entityInstance.id);
        }
        setSelectedInstanceIds(newInstanceIds)
        props.googleTagManager.addEvent('reportsPageToggleEntityInstance', props.pageHandler, { value: entityInstance.name });
    }

    const isOptionSelected = (instance: EntityInstance) => {
        return selectedEntityInstances.some(entity => entity.id === instance.id);
    }

    const getEntityInstancesToCombine = () => {
        return (
            <div className="entity-instances-to-combine">
                <label className="category-label">Answers to combine</label>
                <div className="valid-options">
                    <div className="options">
                        {props.configureNets.availableEntityInstances.length > 0 ?
                            <div>
                                {props.configureNets.availableEntityInstances.filter(o => !props.configureNets.isNetInstance(o)).map((option, i) => {
                                    const key = 'options-selector' + "-net-" + option.name;
                                    return <div className="option" key={key}>
                                        <input type="checkbox" className="checkbox" id={key} checked={isOptionSelected(option)} onChange={() => toggleEntity(option)} />
                                        <label className="option-label" htmlFor={key}>
                                            {isOptionSelected(option) ? <strong>{option.name}</strong> : option.name}
                                        </label>
                                    </div>
                                })}
                            </div>
                            :
                            <div className="no-results">No results</div>
                        }
                    </div>
                </div>
            </div>
        )
    }

    const isAddNetButtonDisabled = () => {
        return selectedInstanceIds.length < 1 || netName === "" || doesNetNameAlreadyExist()
    }

    const getNetActionButtons = () => {
        return (
            <div className="net-buttons">
                <button className="primary-button" disabled={isAddNetButtonDisabled()} onClick={() => createNet()}>Add net</button>
                <button className="hollow-button" onClick={onCloseHandler}>Cancel</button>
            </div>
        )
    }

    return (
        <div className="netting-selector">
            {getNetNameInput()}
            {getEntityInstancesToCombine()}
            {getNetActionButtons()}
        </div>
    );
}

export default CreateNetSidePanel;