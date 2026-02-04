import React from "react";
import { useEffect, useState, useContext } from "react";
import { Factory, SubsetConfiguration } from "../BrandVueApi";
import Footer from "../components/Footer";
import { IGoogleTagManager } from "../googleTagManager";
import JsonConfigurator, { Completions } from "../json_editor/JsonConfigurator";
import ConfigurationList, { ConfigurationElement } from "./ConfigurationList";
import Throbber from "../components/throbber/Throbber";
import { saveFile } from "../helpers/FileOperations";
import { ProductConfigurationContext } from "../ProductConfigurationContext";
import {useTagManager} from "../TagManagerContext";

interface ISubsetProps {
    nav: React.ReactNode;
}

const getSearchableNames = (subset: SubsetConfiguration) => {
    return [subset.displayName, subset.identifier]
}

const subsetToConfigElement = (subsetConfig: SubsetConfiguration) : ConfigurationElement => {
    const configObject = subsetConfig.toJSON();
    delete configObject.id; // Displaying id is not needed - it's shown in the page title and it's easier to copy-paste objects without it
    delete configObject.productShortCode; // No need to show the short code as it must be equal to the current product's short code
    delete configObject.subProductId;
    return { id: subsetConfig.id, displayName: subsetConfig.displayName, configObject: configObject, enabled: !subsetConfig.disabled, searchableNames: getSearchableNames(subsetConfig) }
};

const compareConfigElementNames = (el1: ConfigurationElement, el2: ConfigurationElement) => {
    if (el1.displayName > el2.displayName) return 1;
    if (el1.displayName < el2.displayName) return -1;
    return 0;
}

const SubsetConfigurationPage: React.FunctionComponent<ISubsetProps> = ((props: ISubsetProps) => {
    const subsetConfigClient = Factory.SubsetsClient(error => error());
    const [configurationElements, setConfigurationElements] = useState<ConfigurationElement[]>([]);
    const [isEditorVisible, setIsEditorVisible] = useState<boolean>(false);
    const [completions, setCompletions] = useState<{ [key: string]: Completions[] }>({});
    const [selectedConfigElement, setSelectedConfigElement] = useState<ConfigurationElement>();
    const [isCreate, setIsCreate] = useState<boolean>(true);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const tagManager = useTagManager();
    const onSelectSubsetClick = (configElement: ConfigurationElement) => {
        setSelectedConfigElement(configElement);
        setIsCreate(false);
        setIsEditorVisible(true);
    }

    const reloadSubsets = () => {
        setIsLoading(true);
        subsetConfigClient.getSubsetConfigurations().then(subsets => {
            const elementsSortedByName = subsets.map(subsetToConfigElement).sort(compareConfigElementNames);
            setIsLoading(false);
            setConfigurationElements(elementsSortedByName);
            if (configurationElements.length > 0) {
                onSelectSubsetClick(configurationElements[0]);
            }
        });
    }

    const loadCompletions = () => {
        subsetConfigClient.getValidSegmentNames().then(validSegmentNames => {
            const surveyIds = Object.keys(validSegmentNames).map(s => ({ field: s, meta: "survey-id" }));
            surveyIds.sort();
            const segmentNames = Object.values(validSegmentNames)
                .reduce((segments, segment) => segments.concat(segment), [] as string[])
                .map(s => ({ field: s, meta: "segment-name" }));
            segmentNames.sort();
            setCompletions({surveyIdToAllowedSegmentNames: segmentNames.concat(surveyIds)});
        });
    }

    useEffect(() => {
        reloadSubsets();
        loadCompletions();
    }, []);

    const onCreateSubsetClick = () => {
        const exampleSubsetConfig = new SubsetConfiguration();
        exampleSubsetConfig.displayName = "New Survey Segment";
        exampleSubsetConfig.disabled = false;
        exampleSubsetConfig.enableRawDataApiAccess = true;
        exampleSubsetConfig.identifier = "NewSegment";
        exampleSubsetConfig.order = 0;
        exampleSubsetConfig.displayNameShort = "";
        exampleSubsetConfig.iso2LetterCountryCode = "";
        exampleSubsetConfig.description = "";

        const currentSubset = (selectedConfigElement && selectedConfigElement.configObject) ?
            SubsetConfiguration.fromJS(selectedConfigElement.configObject) :
            SubsetConfiguration.fromJS(configurationElements[0].configObject);

        if (currentSubset)
            exampleSubsetConfig.subProductId = currentSubset.subProductId;
        exampleSubsetConfig.productShortCode = currentSubset.productShortCode;

        exampleSubsetConfig.surveyIdToAllowedSegmentNames = {} as any;
        for (let key in currentSubset.surveyIdToAllowedSegmentNames) {
            if (currentSubset.surveyIdToAllowedSegmentNames.hasOwnProperty(key))
                (exampleSubsetConfig.surveyIdToAllowedSegmentNames)[key] = currentSubset.surveyIdToAllowedSegmentNames[key] !== undefined ? currentSubset.surveyIdToAllowedSegmentNames[key] : [];
        }

        setSelectedConfigElement(subsetToConfigElement(exampleSubsetConfig));
        setIsCreate(true);
        setIsEditorVisible(true);
    }

    const reloadPage = () => {
        setIsLoading(true);
        location.reload();
    }

    const deleteSubset = (id: number) => {
        tagManager.addConfigurationEvent("subsetConfigureDelete");
        return subsetConfigClient.deleteSubsetConfiguration(id)
        .then(() => {
            reloadPage();
        });
    }

    const createSubset = (subsetConfig: object) => {

        tagManager.addConfigurationEvent("subsetConfigureCreate");
        return subsetConfigClient.createSubsetConfiguration(SubsetConfiguration.fromJS(subsetConfig))
        .then(() => {
            reloadPage();
        });
    }

    const updateSubset = (subsetId: number, subsetConfig: object) => {
        if (subsetId === 0) {
            return createSubset(subsetConfig);
        }
        const subset = SubsetConfiguration.fromJS(subsetConfig);
        tagManager.addConfigurationEvent("subsetConfigureUpdate");
        return subsetConfigClient.updateSubsetConfiguration(subsetId, subset)
        .then(() => {
            reloadPage();
        });
    }

    const productConfig = React.useContext(ProductConfigurationContext).productConfiguration;
    const downloadFields = () => {
        if (selectedConfigElement !== undefined) {
            const subsetId = selectedConfigElement.configObject.identifier;
            return Factory.MetaDataClient(() => {}).exportFieldsToJson(subsetId)
                .then(r => saveFile(r, `Fields - ${productConfig.productName} - ${subsetId} - PRIVATE.json`));
        }
    }

    if (isLoading) {
        return (
            <div className="configuration-page">
                {props.nav}
                <div className="throbber-container">
                    <Throbber />
                </div>
            </div>
        );
    }

    return (
        <div className="configuration-page">
            {props.nav}
            <div className="view-chart-configurations">
                <ConfigurationList
                    configTypeName="segment"
                    configElements={configurationElements}
                    onCreateNewElementClick={onCreateSubsetClick}
                    onSelectElementClick={onSelectSubsetClick}
                    displayFilterCheckBoxes={true}
                    showDownloadButton={true}
                    downloadFunction={downloadFields}
                    exportedObjectName={"fields"}
                    exportTooltip={"Export fields data to JSON"}
                    selectedItem={selectedConfigElement}
                    preserveTextAcrossPageRefresh={true}
                    localSelectedKeyForSelectedId="subsetSelectedId"
                    localStorageKeyForSearch="subsetSearch"
                />
                {isEditorVisible ?
                    <>
                    <JsonConfigurator
                        configurationObjectTypeName="survey segment"
                        configElement={selectedConfigElement!}
                        isCreate={isCreate}
                        closeJsonConfigurator={() => setIsEditorVisible(false)}
                        create={createSubset}
                        update={updateSubset}
                        delete={deleteSubset}
                        completions={completions}
                        showDowntimeWarning={true}
                    />
                    
                    </>
                    : null}
            </div>
            <Footer/>
        </div>
);
});

export default SubsetConfigurationPage;