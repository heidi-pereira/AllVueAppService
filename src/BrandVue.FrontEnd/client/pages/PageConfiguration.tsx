import React from "react";
import { useState, useEffect } from "react";
import * as BrandVueApi from "../BrandVueApi";
import _ from "lodash";
import Footer from "../components/Footer";
import JsonConfigurator from "../json_editor/JsonConfigurator"
import ConfigurationList, { ConfigurationElement } from "./ConfigurationList";
import { Completions } from "../json_editor/JsonConfigurator";
import { IGoogleTagManager } from "../googleTagManager";
import { saveFile } from "../helpers/FileOperations";
import PageDescriptor = BrandVueApi.PageDescriptor;
import {useTagManager} from "../TagManagerContext";

interface IPageProps {
    nav: React.ReactNode;
    productName: string;
}

export const PageConfiguration: React.FunctionComponent<IPageProps> = ((props: IPageProps) => {
    const pageConfigClient = BrandVueApi.Factory.PagesClient(error => error());
    const metricsClient = BrandVueApi.Factory.MetricsClient(error => error());
    const [selectedConfigElement, setSelectedConfigElement] = useState<ConfigurationElement>();
    const [configurationElements, setConfigurationElements] = useState<ConfigurationElement[]>([]);
    const [isEditorVisible, setIsEditorVisible] = useState<boolean>(false);
    const [isCreate, setIsCreate] = useState<boolean>(true);
    const [completions, setCompletions] = useState<{ [key: string]: Completions[] }>({});
    const tagManager = useTagManager();
    const reloadPages = (newPageIdToSelect?: number) => {
        return pageConfigClient.getPagesForAllSubsets().then(pages => {
            const configElements: ConfigurationElement[] = Array();
            for (let page of pages) {
                addPage(configElements, page, 1);
            }
            setConfigurationElements(configElements);

            // After creating a page we want to select it for update
            if (newPageIdToSelect) {
                const selectedConfigEl = configElements.find(el => el.id === newPageIdToSelect);
                setSelectedConfigElement(selectedConfigEl);
                setIsCreate(false);
            }
            else {
                setSelectedConfigElement(undefined);
                setIsCreate(true);
            }
        });
    }

    const loadCompletions = () => {
        metricsClient.getMetricConfigurations().then(metrics => {
            const completions = metrics.map(m => ({ field: m.name, meta: "metric-name" }));
            setCompletions({spec1: completions, spec2: completions, spec3: completions});
        });
    }

    useEffect(() => {
        reloadPages();
        loadCompletions();
    }, []);
    useEffect(() => {
        },
        [configurationElements]);

    const addPage = (elements: ConfigurationElement[], page: PageDescriptor, level: number) => {
        elements.push(pageToConfigElement(page, level));
        for (let childPage of page.childPages) {
            addPage(elements, childPage, level + 1);
        }
    }

    const getSearchableNames = (page: PageDescriptor) => {
        return [page.displayName, page.helpText]
    }

    const pageToConfigElement = (page: PageDescriptor, indentationLevel: number): ConfigurationElement => {
        const pageConfig = cleanPageJson(page.toJSON());
        return { id: page.id, displayName: page.displayName, configObject: pageConfig, indentationLevel: indentationLevel, enabled: !page.disabled, searchableNames: getSearchableNames(page) };
    }


    /**
     * Clean the json to be displayed in editor field from unnecessary junk
     */
    const cleanPageJson = pageJson => {
        delete pageJson.id // Displaying id is not needed - it's shown in the page title and it's easier to copy-paste objects without it
        delete pageJson.childPages; // Updating child pages along with the parent is not supported

        // Environment and disabled fields are no longer in pages, panes and parts
        delete pageJson.environment;
        delete pageJson.disabled;

        pageJson.panes && pageJson.panes.forEach((pane, paneIndex) => {
            // We can populate the fields maintaining the relationships between pages, panes and parts on server side:
            // page.name -> pane.pageName, pane.id -> part.paneId
            delete pane.id;
            delete pane.pageName;
            delete pane.environment;
            delete pane.disabled;

            // We don't need all subset details showing, just the id will do
            if (pane.subset.length > 0) {
                pane.subset = pane.subset.map(s => {
                    return { "id": s.id }
                })
            };

            // Roles field appears in almost every object returned by API but is irrelevant for panes and parts
            delete pane.roles;

            pane.parts && pane.parts.forEach(part => {
                delete part.fakeId; // This is technical debt
                delete part.paneId;
                delete part.environment;
                delete part.disabled;
                delete part.subset;
                delete part.roles;
            });
        });

        return pageJson;
    }

    const onSelectClick = (configElement: ConfigurationElement) => {
        setSelectedConfigElement(configElement);
        setIsEditorVisible(true);
        setIsCreate(false);
    }

    const onCreateClick = () => {
        setIsCreate(true);
        setSelectedConfigElement(pageToConfigElement(new PageDescriptor(), 1));
        setIsEditorVisible(true);
    }

    const deletePage = (pageId: number) => {
        tagManager.addConfigurationEvent("pageConfigureDelete");
        return pageConfigClient.deletePage(pageId)
        .then(() => {
            reloadPages();
        });
    }

    const createPage = (pageConfig: object) => {
        tagManager.addConfigurationEvent("pageConfigureCreate");
        return pageConfigClient.createPage(PageDescriptor.fromJS(pageConfig))
        .then(newlyCreatedPage => {
            reloadPages(newlyCreatedPage.id);
        });
    }

    const updatePage = (pageId: number, pageConfig: object) => {
        tagManager.addConfigurationEvent("pageConfigureUpdate");
        const page = PageDescriptor.fromJS(pageConfig);
        return pageConfigClient.updateOrCreatePage(pageId, page)
        .then(updatedPage => {
            reloadPages(updatedPage.id);
        });
    }

    const downloadPages = () => {
        return BrandVueApi.Factory.PagesClient(() => { }).exportPagesToExcel()
            .then(r => saveFile(r, `PagesPanesAndParts - ${props.productName} - PRIVATE.xlsx`));
    }
    return (
        <div className="configuration-page">
            {props.nav}

            <div className="view-chart-configurations">
                <ConfigurationList
                    configTypeName="page"
                    configElements={configurationElements}
                    onCreateNewElementClick={onCreateClick}
                    onSelectElementClick={onSelectClick}
                    displayFilterCheckBoxes={true}
                    showDownloadButton={true}
                    downloadFunction={downloadPages}
                    selectedItem={selectedConfigElement}
                    exportedObjectName={"page"}
                    exportTooltip={"Export pages/parts/panes data to Excel"}
                />
                {isEditorVisible ?
                    <JsonConfigurator
                        configurationObjectTypeName="page"
                        configElement={selectedConfigElement}
                        isCreate={isCreate}
                        closeJsonConfigurator={() => setIsEditorVisible(false)}
                        create={createPage}
                        update={updatePage}
                        delete={deletePage}
                        completions={completions}
                    />
                    : null}
            </div>
            <Footer />
        </div>
    );
});

