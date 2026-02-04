import React from 'react';
import {EntityInstance} from "../../../../../entity/EntityInstance";
import {PartWithExtraData} from "../../ReportsPageDisplay";
import {PartDescriptor, SelectedEntityInstances} from '../../../../../BrandVueApi';
import {IConfigureNets} from "../ConfigureNets";
import Tooltip from "../../../../Tooltip";

interface IConfigureReportPartEntityInstancesProps {
    reportPart: PartWithExtraData;
    savePartChanges(newPart: PartDescriptor): void;
    setIsSidePanelOpen: (boolean) => void;
    configureNets: IConfigureNets;
}

const ConfigureReportPartEntityInstances = (props: IConfigureReportPartEntityInstancesProps) => {
    const selectedInstanceIds = props.reportPart.part.selectedEntityInstances?.selectedInstances;
    const selectedEntityInstances = props.configureNets.availableEntityInstances.filter(i => selectedInstanceIds == null || selectedInstanceIds.includes(i.id));

    const removeNet = (option: EntityInstance) => {
        if (!props.configureNets.isDeletingNet) {
            props.configureNets.removeNet(option).then(() => {
                    props.setIsSidePanelOpen(false)
                }
            )
        }
    }

    const updateSelectedOptions = (instances: EntityInstance[]) => {
        const modifiedPart = new PartDescriptor(props.reportPart.part);
        modifiedPart.selectedEntityInstances = new SelectedEntityInstances({
            selectedInstances: instances.map(i => i.id)
        });;
        props.savePartChanges(modifiedPart);
    }

    const toggleEntity = (reportOption: EntityInstance) => {
        const newInstanceIds = [...(selectedInstanceIds ?? props.configureNets.availableEntityInstances.map(i => i.id))];
        if(newInstanceIds.includes(reportOption.id)) {
            const index = newInstanceIds.findIndex(id => id === reportOption.id);
            newInstanceIds.splice(index, 1);
        } else {
            newInstanceIds.push(reportOption.id);
        }
        const modifiedPart = new PartDescriptor(props.reportPart.part);
        if (modifiedPart.multiBreakSelectedEntityInstance !== undefined
            && !newInstanceIds.some(id => modifiedPart.multiBreakSelectedEntityInstance === id))
        {
            modifiedPart.multiBreakSelectedEntityInstance = newInstanceIds[0]
        }
        modifiedPart.selectedEntityInstances = new SelectedEntityInstances({
            selectedInstances: newInstanceIds
        });
        props.savePartChanges(modifiedPart);
    }

    const isOptionSelected = (reportOption: EntityInstance) => {
        return selectedEntityInstances.some(option => option.id === reportOption.id);
    }

    const getListItemCross = (option: EntityInstance) => {
        return (
            <div className="remove-button" onClick={() => {removeNet(option)}}>
                <i className="material-symbols-outlined">close</i>
            </div>
        );
    }

    const getNetInstances = (option: EntityInstance) => {
        const nettedInstances = props.configureNets.getNettedInstanceNames(option).join(", ")
        return (
            <Tooltip placement="top" title={nettedInstances}>
                <i>
                    {nettedInstances}
                </i>
            </Tooltip>
        )
    }

    const getDecoratedOption = (option: EntityInstance) => {
        const key = 'options-selector' + "-entity-" + option.name;
        if (props.configureNets.isNetInstance(option)){
            return (
                <div className="option" key={key}>
                    <input disabled={props.configureNets.isDeletingNet} type="checkbox" className="checkbox" id={key} checked={isOptionSelected(option)} onChange={() => toggleEntity(option)} />
                        <label className="option-label" htmlFor={key}>
                            <div className="netted-option-text">
                                {isOptionSelected(option) ? <strong>{option.name}</strong> : option.name}
                                {getNetInstances(option)}
                            </div>
                        </label>
                    {getListItemCross(option)}
                </div>
            );
        }
        return (
         <div className="option" key={key}>
            <input disabled={props.configureNets.isDeletingNet} type="checkbox" className="checkbox" id={key} checked={isOptionSelected(option)} onChange={() => toggleEntity(option)} />
            <label className="option-label" htmlFor={key}>
                    {isOptionSelected(option) ? <strong>{option.name}</strong> : option.name}
            </label>
         </div>
        );
    }

    const sortAvailableOptions = (): EntityInstance[] => {
        const originalOption: EntityInstance[] = []
        const nettedOption: EntityInstance[] = []
        for (const option of props.configureNets.availableEntityInstances){
            props.configureNets.isNetInstance(option) ? nettedOption.push(option) : originalOption.push(option)
        }
        originalOption.push(...nettedOption)
        return originalOption
    }

    if (props.configureNets.canPickEntityInstances) {
        return (
            <>
                <div className="report-entity-options-list">
                    <div className="report-entity-options-buttons">
                        <button className="modal-button secondary-button" onClick={() => {updateSelectedOptions(props.configureNets.availableEntityInstances)}}>Select All</button>
                        <button className="modal-button secondary-button" onClick={() => {updateSelectedOptions([])}}>Clear All</button>
                        <div className="report-entity-options-buttons-text"><strong>{selectedEntityInstances.length}</strong> selected</div>
                    </div>
                    <div className="valid-options">
                        <div className="options">
                            {props.configureNets.availableEntityInstances.length > 0 ?
                                <div>
                                    {sortAvailableOptions().map((option, _) => {return getDecoratedOption(option)})}
                                </div>
                                :
                                <div className="no-results">No results</div>
                            }
                        </div>
                    </div>
                </div>
                {props.configureNets.canAddNet &&
                    <div className="add-net-button">
                            <button className={'hollow-button'} onClick={() => { props.setIsSidePanelOpen(true) }}>
                                <i className="material-symbols-outlined">add</i>
                                <div>Add net</div>
                        </button>
                    </div>
                }
        </>
        );
    }

    return null;
};

export default ConfigureReportPartEntityInstances;