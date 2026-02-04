import React from "react";
import {ChangeEvent, useEffect, useState} from "react";
import {
    EntityInstanceConfigurationModel,
    EntityInstanceModel,
    IEntityTypeConfiguration,
    EntityTypeCreatedFrom,
    Factory,
    IEntitiesClient,
    Subset
} from "../BrandVueApi";
import _ from "lodash";
import SearchInput from "../components/SearchInput";
import SubsetSelector from "../components/filters/SubsetSelector";
import {DataSubsetManager} from '../DataSubsetManager';
import {toast, Toaster} from 'react-hot-toast';
import Tooltip from "../components/Tooltip";
import moment from 'moment';

interface IEntityTypeConfigurationPage {
    entityTypeConfiguration: IEntityTypeConfiguration;
    handleBlur: (event: ChangeEvent<HTMLInputElement>, entityType: IEntityTypeConfiguration, displayNameSingular: string, displayNamePlural: string) => Promise<void>;
}

const IEntityTypeConfigurationPage: React.FunctionComponent<IEntityTypeConfigurationPage> = (props) => {
    const entitiesClient: IEntitiesClient = Factory.EntitiesClient(() => { });
    const [entityInstanceConfigurations, setEntityInstanceConfigurations] = useState<EntityInstanceModel[]>([]);
    const [displayNameSingular, setDisplayNameSingular] = useState(props.entityTypeConfiguration.displayNameSingular);
    const [displayNamePlural, setDisplayNamePlural] = useState(props.entityTypeConfiguration.displayNamePlural);
    const [searchString, setSearchString] = useState("");
    const [selectedSubset, setSelectedSubset] = useState<Subset>();

    useEffect(() => {
        const api = Factory.MetaDataClient(error => error());
        api.getSubsets().then(subsets => {
            DataSubsetManager.Initialize(subsets, "");
            DataSubsetManager.getAll();
            setSelectedSubset(DataSubsetManager.selectedSubset);
        })
    }, []);

    useEffect(() => {
        setEntityInstanceConfigurations([]);
        setDisplayNameSingular(props.entityTypeConfiguration.displayNameSingular);
        setDisplayNamePlural(props.entityTypeConfiguration.displayNamePlural);
        if (selectedSubset) {
            entitiesClient.getEntityInstanceConfigurations(selectedSubset.id, props.entityTypeConfiguration.identifier)
                .then(values => setEntityInstanceConfigurations(_.orderBy(values, v => v.surveyChoiceId)))
                .catch((e: Error) => {
                    toast.error(e.message);
                });
        }
    }, [props.entityTypeConfiguration, selectedSubset]);

    const getEntityInstanceConfigurationModel = (entityTypeIdentifier: string, surveyChoiceId: number, displayName: string, enabled: boolean, imageUrl: string, startDate: Date | undefined) => {
        return new EntityInstanceConfigurationModel({
            entityTypeIdentifier: entityTypeIdentifier,
            surveyChoiceId: surveyChoiceId,
            displayName: displayName,
            enabled: enabled,
            startDate: startDate,
            imageUrl: imageUrl,
        });
    };

    const updateEntityInstance = async (instanceIdentifier: string, newName: string, enabled: boolean, imageUrl:string, startDate: moment.Moment | undefined, applyToAllSubsets: boolean): Promise<void> => {
        if (!selectedSubset) {
            toast.error("Subset cannot be null");
            return Promise.reject();
        }

        if (!newName || newName.trim().length === 0) {
            toast.error("Value cannot be null or empty");
            return Promise.reject();
        }

        const entityInstancesNotEditing = filteredConfigurations.filter(fc => fc.identifier !== instanceIdentifier);

        if (entityInstancesNotEditing.find(e => e.displayName === newName)) {
            toast.error(`"${newName}" already in use`);
            return Promise.reject();
        }

        const copy = [...entityInstanceConfigurations];
        const toUpdate = copy.find(c => c.identifier === instanceIdentifier)!;
        toUpdate.displayName = newName;
        toUpdate.enabled = enabled;
        toUpdate.startDate = startDate?.toDate();
        setEntityInstanceConfigurations(copy);

        const model = getEntityInstanceConfigurationModel(
            props.entityTypeConfiguration.identifier,
            toUpdate.surveyChoiceId,
            newName,
            enabled,
            imageUrl,
            startDate?.toDate(),
        );
        await entitiesClient.saveEntityInstance(selectedSubset.id, model, applyToAllSubsets)
            .then(() => toast.success("Saved"),
                (error) => {
                    toast.error(error.message);
                });
    };

    const filteredConfigurations = searchString.trim().length > 0 ?
        entityInstanceConfigurations.filter(c => c.displayName.toLowerCase().includes(searchString.trim().toLowerCase()))
        : entityInstanceConfigurations;

    return (
        <div id="entity-type-configuration-page">
            <div className="scroll-area">
                <div className="scroll-area-center">
                    <div className="top-section">
                        <h1>{props.entityTypeConfiguration.displayNameSingular}<span> ({props.entityTypeConfiguration.identifier})</span></h1>
                        <div className="configuration-list">
                            <div className="item-configuration">
                                <label htmlFor="display-name-singular" className="input-label">Display Name Singular</label>
                                <div className="input-group">
                                    <input id="display-name-singular" className="input" type="text" spellCheck={false} autoComplete="off" value={displayNameSingular} onChange={(e) => setDisplayNameSingular(e.target.value)}
                                        onBlur={(e) => props.handleBlur(e, props.entityTypeConfiguration, displayNameSingular, displayNamePlural)
                                            .then(() => toast.success("Saved"),
                                                (error) => {
                                                    toast.error(error.message);
                                                    setDisplayNameSingular(props.entityTypeConfiguration.displayNameSingular);
                                        } )} />
                                </div>
                            </div>
                            <div className="item-configuration">
                                <label htmlFor="display-name-plural" className="input-label">Display Name Plural</label>
                                <div className="input-group">
                                    <input id="display-name-plural" className="input" type="text" spellCheck={false} autoComplete="off" value={displayNamePlural} onChange={(e) => setDisplayNamePlural(e.target.value)}
                                        onBlur={(e) => props.handleBlur(e, props.entityTypeConfiguration, displayNameSingular, displayNamePlural)
                                            .then(() => toast.success("Saved"),
                                                (error) => {
                                                    toast.error(error.message);
                                                    setDisplayNamePlural(props.entityTypeConfiguration.displayNamePlural);
                                            })} />
                                </div>
                            </div>
                        </div>
                        <div className="headers">
                            <h1>Configure Choices</h1>
                            <div className="subset-headers">
                                <h1>For subsets</h1>
                                {selectedSubset && <SubsetSelector selectedSubset={selectedSubset} onChange={setSelectedSubset} darkStyling={true} />}
                            </div>
                        </div>
                        <div className="configure-choices">
                            <SearchInput id="entity-instance-search" text={searchString} onChange={(text) => setSearchString(text)} className="entity-instance-search" />
                            <div className="configure-subset">
                                <div className="configure-subset-headers">
                                    <h4 className="headers">Enabled</h4>
                                    <h4 className="headers">StartDate</h4>
                                </div>
                                <label className="date-format-label">DD/MM/YYYY</label>
                            </div>
                        </div>
                    </div>
                    <div className="configuration-list">
                        {filteredConfigurations.map(c =>
                            <InstanceConfiguration key={c.identifier}
                                instanceId={c.id}
                                surveyChoiceId={c.surveyChoiceId}
                                instanceIdentifier={c.identifier}
                                displayName={c.displayName}
                                updateEntityInstance={updateEntityInstance}
                                enabled={selectedSubset ? c.enabled : true}
                                startDate={c.startDate && moment(c.startDate)}
                                isDisabled={props.entityTypeConfiguration.createdFrom === EntityTypeCreatedFrom.Variable}
                                imageUrl={c.imageUrl}
                            />)}
                    </div>
                </div>
            </div>
            <Toaster
                position='top-center'
                toastOptions={{ duration: 3000 }}
            />
        </div>
    );
};

interface IInstanceConfigurationProps {
    instanceId: number;
    surveyChoiceId: number;
    instanceIdentifier: string;
    displayName: string | undefined;
    enabled: boolean | undefined;
    startDate: moment.Moment | null | undefined;
    imageUrl: string;
    updateEntityInstance: (instanceIdentifier: string, newName: string, enabled: boolean, imageUrl: string, startDate: moment.Moment | undefined, applyToAllSubsets: boolean) => Promise<void>;
    isDisabled: boolean;
}

const InstanceConfiguration: React.FunctionComponent<IInstanceConfigurationProps> = ((props: IInstanceConfigurationProps) => {
    const dateFormat = "DD/MM/YYYY";

    const [initialName, setInitialName] = useState(props.displayName ?? "");
    const [initialImageUrl, setInitialImageUrl] = useState(props.imageUrl ?? "");
    const [initialEnabled, setInitialEnabled] = useState(props.enabled ?? true);
    const [initialStartDate, setInitialStartDate] = useState(props.startDate ?? undefined);
    const [inputNameValue, setInputNameValue] = useState(initialName);
    const [inputStartDateValue, setInputStartDateValue] = useState(initialStartDate?.format(dateFormat) ?? "");
    const [hideApplyToAllOption, setHideApplyToAllOption] = useState(true);
    const [applyToAllSubsets, setApplyToAllSubsets] = useState(false);

    const saveInstance = async (newName: string | undefined, newEnabled: boolean | undefined, newStartDate: moment.Moment | undefined | null): Promise<boolean> => {
        const name = newName || initialName;
        const enabled = newEnabled === undefined ? initialEnabled : newEnabled;
        const startDate = newStartDate === undefined ? initialStartDate : newStartDate;
        if (name !== initialName || enabled !== initialEnabled || startDate != initialStartDate) {
            await props.updateEntityInstance(props.instanceIdentifier, name, enabled, initialImageUrl, startDate ?? undefined, applyToAllSubsets)
                .then(() => {
                    setInitialName(name);
                    setInitialEnabled(enabled);
                    setInitialStartDate(startDate ?? undefined);
                },() => {
                        setInitialName(initialName);
                        setInitialEnabled(initialEnabled);
                        setInitialStartDate(initialStartDate);
                        setInputNameValue(initialName);
                        return false;
            });
            return true;
        }
        return false;
    }

    const handleNameChange = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
        setInputNameValue(event.target.value);
        setHideApplyToAllOption(false);
    }

    const handleStartDateChange = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
        setInputStartDateValue(event.target.value);
    }

    const saveNameChange = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
        const entityInstanceSaved = await saveInstance(event.target.value, undefined, undefined);
        if (!entityInstanceSaved) {
            setInputNameValue(initialName);
        }
        setHideApplyToAllOption(true);
    }

    const changeEnabled = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
        await saveInstance(undefined, event.target.checked, undefined);
    }

    const saveStartDateChange = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
        let startDate: moment.Moment | null | undefined = null;
        if (event.target.value.length !== 0) {
            startDate = moment.utc(event.target.value, dateFormat, true);
            if (!startDate.isValid()) {
                setInputStartDateValue(initialStartDate?.format(dateFormat) ?? "");
                return;
            }
        }
        const entityInstanceSaved = await saveInstance(undefined, undefined, startDate);
        if (!entityInstanceSaved) {
            setInputStartDateValue(initialStartDate?.format(dateFormat) ?? "");
        }
    }
    
    const getTooltipWrapper = (reactNode: React.ReactNode) => {
        if (props.isDisabled) {
            return (
                <Tooltip placement="top-start" title={`Unable to edit entity types used by variables here, try editing the variable`}>
                    {reactNode}
                </Tooltip>
            );
        }
        return reactNode
    }

    const inputName = `input-${props.instanceIdentifier}`;
    const checkBoxName = `checkbox-${props.instanceIdentifier}`;
    const applyToAllCheckBoxName = `apply-to-all-checkbox-${props.instanceIdentifier}`;
    const dateName = `date-${props.instanceIdentifier}`;
    const defaultNameLabel = props.displayName !== props.instanceIdentifier ? ` (Default: ${props.instanceIdentifier})` : ``;
    const hasIcon = props.imageUrl && props.imageUrl.length;
    const titleName = props.surveyChoiceId + ": " + props.displayName + defaultNameLabel;
    return (
        <div className="item-configuration">
            <div className="item-headers">
                <label htmlFor={inputName} className="input-label">
                    {hasIcon &&
                        <a href={props.imageUrl} target="_blank">
                            {titleName}
                        </a>
                    }
                    {!hasIcon && titleName}
                </label>
                <div className="checkbox-group" hidden={hideApplyToAllOption} onMouseDown={e => e.preventDefault()}>
                    <input
                        type="checkbox"
                        className="checkbox"
                        id={applyToAllCheckBoxName}
                        checked={applyToAllSubsets}
                        onChange={e => setApplyToAllSubsets(e.target.checked)}
                    />
                    <label htmlFor={applyToAllCheckBoxName}>Apply to all subsets</label>
                </div>
            </div>
            <div className="input-group">
                {getTooltipWrapper(
                    <input id={inputName} className="input" type="text" spellCheck={false} autoComplete="off" value={inputNameValue} onChange={handleNameChange} onBlur={saveNameChange} disabled={props.isDisabled}/>
                )}
                <div className="subset-group">
                    <div className="checkbox-group">
                        <input
                            type="checkbox"
                            className="checkbox"
                            id={checkBoxName}
                            checked={initialEnabled}
                            onChange={changeEnabled}
                            disabled={props.isDisabled}/>
                        <label htmlFor={checkBoxName}/>
                    </div>
                    {getTooltipWrapper(
                        <input id={dateName} className="input startDate" type="text" spellCheck={false} autoComplete="off" value={inputStartDateValue} onChange={handleStartDateChange} onBlur={saveStartDateChange} disabled={props.isDisabled}/>
                    )} 
                </div>
            </div>
        </div>
    );
});

export default IEntityTypeConfigurationPage;