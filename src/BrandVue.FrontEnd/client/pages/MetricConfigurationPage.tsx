import React from "react";
import {useEffect, useState} from "react";
import * as BrandVueApi from "../BrandVueApi";
import {AutoGenerationType, IVariableConfigurationModel, VariableConfigurationModel} from "../BrandVueApi";
import Footer from "../components/Footer";
import { IGoogleTagManager } from "../googleTagManager";
import JsonConfigurator, {Completions} from "../json_editor/JsonConfigurator";
import ConfigurationList, {ConfigurationElement} from "./ConfigurationList";
import { saveFile } from "../helpers/FileOperations";
import {useTagManager} from "../TagManagerContext";
import { useAppSelector, useAppDispatch } from "client/state/store";
import { fetchVariableConfiguration } from "client/state/variableConfigurationsSlice";
import Throbber from "../components/throbber/Throbber";

interface IMetricProps {
    nav: React.ReactNode;
    productName: string;
}

const getSearchableNames = (metricConfig: BrandVueApi.MetricConfiguration) => {
    return [metricConfig.name, metricConfig.varCode, metricConfig.displayName, metricConfig.helpText]
}

const compareConfigElementNames = (el1: ConfigurationElement, el2: ConfigurationElement) => {
    if (el1.displayName > el2.displayName) return 1;
    if (el1.displayName < el2.displayName) return -1;
    return 0;
}

export const MetricConfigurationPage: React.FunctionComponent<IMetricProps> = ((props: IMetricProps) => {
    const metricConfigClient = BrandVueApi.Factory.MetricsClient(error => error());
    const metaDataClient = BrandVueApi.Factory.MetaDataClient(error => error());
    const [configurationElements, setConfigurationElements] = useState<ConfigurationElement[]>([]);
    const [isEditorVisible, setIsEditorVisible] = useState<boolean>(false);
    const [completions, setCompletions] = useState<{ [property: string]: Completions[] }>({});
    const [selectedConfigElement, setSelectedConfigElement] = useState<ConfigurationElement>();
    const [isCreate, setIsCreate] = useState<boolean>(true);
    const { variables, loading } = useAppSelector((state) => state.variableConfiguration);
    const [metrics, setMetrics] = useState<BrandVueApi.MetricConfiguration[]>([]);
    const [metricsLoading, setMetricsLoading] = useState<boolean>(true);
    const dispatch = useAppDispatch();
    const tagManager = useTagManager();
    
    // Helper for restoring the ID before saving:
    const mutateConfigObjectToMetric = (configObject: any, variables: IVariableConfigurationModel[]) => {
        if (configObject.primaryVariableIdentifier) {
            const found = variables.find(v => v.identifier === configObject.primaryVariableIdentifier);
            if (found) {
                configObject.variableConfigurationId = found.id;
            } else {
                throw new Error(`Could not find variable with identifier ${configObject.primaryVariableIdentifier}`);
            }
            delete configObject.primaryVariableIdentifier;
        }
        if (configObject.baseVariableConfigurationIdentifier) {
            const found = variables.find(v => v.identifier === configObject.baseVariableIdentifier);
            if (found) {
                configObject.baseVariableConfigurationId = found.id;
            } else {
                throw new Error(`Could not find base variable with identifier ${configObject.baseVariableIdentifier}`);
            }
            delete configObject.baseVariableIdentifier;
        }
    }

    const metricToConfigElement = (metricConfig: BrandVueApi.MetricConfiguration) : ConfigurationElement => {
        const configObject = metricConfig.toJSON();

        const found = variables?.find(v => v.id === configObject.variableConfigurationId);
        configObject.primaryVariableIdentifier = found ? found.identifier : null;
        if (!found) {
            console.warn(`Could not find variable with id ${configObject.variableConfigurationId} for metric ${configObject.name}`);
        }
        const foundBase = variables?.find(v => v.id === configObject.baseVariableConfigurationId);
        configObject.baseVariableIdentifier = foundBase ? foundBase.identifier : null;
        if (!foundBase) {
            console.warn(`Could not find variable with id ${configObject.baseVariableConfigurationId} for metric ${configObject.name}`);
        }

        const newConfigObject: any = {};
        for (const key in configObject) {
            if (key === "baseExpression") {
                newConfigObject.baseVariableIdentifier = configObject.baseVariableIdentifier;
            }
            newConfigObject[key] = configObject[key];
        }

        delete newConfigObject.variableConfigurationId;
        delete newConfigObject.baseVariableConfigurationId;
        delete newConfigObject.id;
        delete newConfigObject.productShortCode;
        delete newConfigObject.subProductId;

        const searchableNames = [newConfigObject.primaryVariableIdentifier, newConfigObject.baseVariableIdentifier, ...getSearchableNames(metricConfig)].filter(x => x != null);
        return { id: metricConfig.id, displayName: metricConfig.name, configObject: newConfigObject, enabled: true, searchableNames: searchableNames }
    };
    
    const onSelectMetricClick = (configElement: ConfigurationElement) => {
        const cloned = { ...configElement.configObject };
        configElement.configObject = cloned;
        setSelectedConfigElement(configElement);
        setIsCreate(false);
        setIsEditorVisible(true);
    }

    const reloadMetrics = () => {
        metricConfigClient.getMetricConfigurations().then(metrics => {
            const elementsSortedByName = metrics.map(metricToConfigElement).sort(compareConfigElementNames);
            setConfigurationElements(elementsSortedByName);
        });
    }

    const loadCompletions = () => {
        metaDataClient.getFieldDescriptors().then(fields => {
            const variableIdentifiers = variables?.map(v => ({ field: v.identifier, meta: "variable" })) || [];
            const fieldCompletions = fields.map(m => ({ field: m.name, meta: "field" }));

            setCompletions({
                field: fieldCompletions,
                field2: fieldCompletions,
                baseField: fieldCompletions,
                fieldExpression: variableIdentifiers.concat(fieldCompletions),
                baseExpression: variableIdentifiers.concat(fieldCompletions),
                primaryVariableIdentifier: variableIdentifiers,
                baseVariableIdentifier: variableIdentifiers,
            });
        });
    }

    useEffect(() => {
        dispatch(fetchVariableConfiguration());
        metricConfigClient.getMetricConfigurations().then(metrics => {
            setMetrics(metrics);
            setMetricsLoading(false);
        });
    }, []);

    useEffect(() => {
        if (variables && !loading && !metricsLoading) {
            const elementsSortedByName = metrics.map(metricToConfigElement).sort(compareConfigElementNames);
            setConfigurationElements(elementsSortedByName);
            loadCompletions();
        }
    }, [variables, loading, metricsLoading]);

    const resetEditor = () => {
        setSelectedConfigElement(undefined);
        setIsCreate(true);
        setIsEditorVisible(false);
    }

    const onCreateMetricClick = () => {
        const exampleMetricConfig = new BrandVueApi.MetricConfiguration();
        exampleMetricConfig.name = "New Metric";
        exampleMetricConfig.productShortCode = props.productName;
        exampleMetricConfig.calcType = "yn";

        setSelectedConfigElement(metricToConfigElement(exampleMetricConfig));
        setIsCreate(true);
        setIsEditorVisible(true);
    }

    const deleteMetric = (id: number) => {
        tagManager.addConfigurationEvent("metricConfigureDelete");
        return metricConfigClient.deleteMetric(id)
        .then(() => {
            reloadMetrics();
            resetEditor();
        });
    }

    const createMetric = (metricConfig: object) => {
        tagManager.addConfigurationEvent("metricConfigureCreate");
        mutateConfigObjectToMetric(metricConfig, variables!);
        return metricConfigClient.createMetricConfiguration(BrandVueApi.MetricConfiguration.fromJS(metricConfig))
        .then(newMetric => {
            const newlyCreatedConfigEl = metricToConfigElement(newMetric);
            const newConfigElements: ConfigurationElement[] = [ ...configurationElements, newlyCreatedConfigEl ].sort(compareConfigElementNames);
            setConfigurationElements(newConfigElements);
            setSelectedConfigElement(newlyCreatedConfigEl);
            setIsCreate(false);
        });
    }

    const updateMetric = (metricId: number, metricConfig: object) => {
        tagManager.addConfigurationEvent("metricConfigureUpdate");
        mutateConfigObjectToMetric(metricConfig, variables!);
        const metric = BrandVueApi.MetricConfiguration.fromJS(metricConfig);
        return metricConfigClient.updateMetricConfiguration(metricId, metric)
        .then(() => {
            reloadMetrics();
        });
    }

    const downloadMetrics = () => {
        return BrandVueApi.Factory.MetricsClient(() => { }).exportMetricsToCsv()
            .then(r => saveFile(r, `Metrics - ${props.productName} - PRIVATE.csv`));
    }
    const localStorageSelectedMetricIdKey = "selectedMetricId";

    if (loading || !variables) {
        return <Throbber />;
    }

    return (
        <div className="configuration-page">
            {props.nav}

            <div className="view-chart-configurations">
                <ConfigurationList
                    configTypeName="metric"
                    configElements={configurationElements}
                    onCreateNewElementClick={onCreateMetricClick}
                    onSelectElementClick={onSelectMetricClick}
                    preserveTextAcrossPageRefresh={true}
                    localStorageKeyForSearch="metricSearchQuery"
                    showDownloadButton={true}
                    downloadFunction={downloadMetrics}
                    exportedObjectName={"metric"}
                    exportTooltip={"Export metric data to CSV"}
                    selectedItem={selectedConfigElement}
                    localSelectedKeyForSelectedId={localStorageSelectedMetricIdKey}
                />
                {isEditorVisible ?
                    <JsonConfigurator
                        configurationObjectTypeName="metric"
                        configElement={selectedConfigElement!}
                        isCreate={isCreate}
                        closeJsonConfigurator={() => setIsEditorVisible(false)}
                        create={createMetric}
                        update={updateMetric}
                        delete={deleteMetric}
                        completions={completions}
                        deletionWarningMessage={"(If this is a FieldExpression or QuestionVariable based metric the related variable will be deleted)"}
                    />
                    : null}
            </div>
            <Footer />
        </div>
    );
});
