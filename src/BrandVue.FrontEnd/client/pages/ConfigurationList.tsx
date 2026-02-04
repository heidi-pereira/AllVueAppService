import React from "react";
import SearchInput from "../components/SearchInput";
import { useEffect } from "react";
import ExcelDownloadButton from "../components/buttons/ExcelDownloadButton";
import {useReadVueQueryParams} from "../components/helpers/UrlHelper";

export interface IConfigurationListProps {
    configTypeName: string;
    configElements: ConfigurationElement[];
    onCreateNewElementClick?: () => void;
    onSelectElementClick(config: ConfigurationElement): void;
    includeId?: boolean;
    displayFilterCheckBoxes?: boolean;
    preserveTextAcrossPageRefresh?: boolean;
    localStorageKeyForSearch?: string;
    localSelectedKeyForSelectedId?: string;
    showDownloadButton?: boolean;
    downloadFunction?: () => void;
    exportedObjectName?: string;
    exportTooltip?: string;
    selectedItem?: ConfigurationElement;
}

export class ConfigurationElement {
    id: number;
    displayName: string;
    configObject: any;
    indentationLevel?: number;
    enabled: boolean;
    searchableNames: string[]
}

const ConfigurationElementList = ({ configTypeName,
    configElements, onCreateNewElementClick, 
    onSelectElementClick,
    includeId = true,
    displayFilterCheckBoxes = false,
    preserveTextAcrossPageRefresh = false,
    localStorageKeyForSearch = "",
    localSelectedKeyForSelectedId = "",
    showDownloadButton = false,
    downloadFunction = () => { },
    exportedObjectName = "",
    exportTooltip = undefined,
    selectedItem = undefined
}: IConfigurationListProps) => {
    const activeElementId = "activeSetDropdownItem";
    const activeURLParam = "Active";

    const [searchQuery, setSearchQuery] = React.useState("");
    const [displayEnabled, setDisplayEnabled] = React.useState<boolean>(true);
    const [displayDisabled, setDisplayDisabled] = React.useState<boolean>(false);
    const [selectedItemById, setSelectedItemById] = React.useState<number>(0);
    const { getQueryParameter } = useReadVueQueryParams();
    const selectedItemReference = React.useRef<HTMLTableElement>(null);

    const handleDisplayEnabled = (e: React.ChangeEvent<HTMLInputElement>) => {
        setDisplayEnabled(e.target.checked);
    }
    const handleDisplayDisabled = (e: React.ChangeEvent<HTMLInputElement>) => {
        setDisplayDisabled(e.target.checked);
    }

    const IsItemShowed = (searchableNames: string[], isItemEnabled: boolean) => {
        const lowerCaseQuery = searchQuery.trim().toLocaleLowerCase()
        if (searchableNames?.some(s => s?.toLowerCase()?.includes(lowerCaseQuery))) {
            if (displayFilterCheckBoxes) {
                return (isItemEnabled && displayEnabled) || (!isItemEnabled && displayDisabled);
            }
            return true;
        }
        return false;
    }

    React.useEffect(() => {
        window.requestAnimationFrame(() => {
            const selectedElement = document.getElementById(activeElementId);
            if (selectedElement) {
                setTimeout(() => selectedElement.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "start" }), 50);
            }
        });
        
    }, [selectedItem]);

    React.useEffect(() => {
        if (preserveTextAcrossPageRefresh) {
            const selectedMetric = configElements.find(metric => metric.id === selectedItemById);
            if (selectedMetric != undefined) {
                onSelectElementClick(selectedMetric);
            }
        }
    }, [configElements]);

    useEffect(() => {
        if (preserveTextAcrossPageRefresh) {
            console.log(getQueryParameter<number>(activeURLParam));
            if (getQueryParameter<number>(activeURLParam) != undefined) {

                const storedSearchQuery = window.sessionStorage.getItem(localStorageKeyForSearch) || "";
                setSearchQuery(storedSearchQuery);

                const selectedId = window.sessionStorage.getItem(localSelectedKeyForSelectedId);
                if (selectedId != null) {
                    setSelectedItemById(parseInt(selectedId));
                }
            }
            else {
                if (!window.location.toString().match(activeURLParam)) {
                    history.pushState({}, '', `${window.location}?${activeURLParam}=1`);
                }
            }
        }
    }, []);

    const onSelectedElement = (element: ConfigurationElement) => {
        if (preserveTextAcrossPageRefresh) {
            window.sessionStorage.setItem(localSelectedKeyForSelectedId, element.id.toString());
        }
        onSelectElementClick(element);
    }
    const setSearchText = (text: string) => {
        const textToSearch = text;
        if (preserveTextAcrossPageRefresh) {
            if (textToSearch == null || textToSearch.length == 0) {
                window.sessionStorage.removeItem(localStorageKeyForSearch);
            }
            else {
                window.sessionStorage.setItem(localStorageKeyForSearch, textToSearch);
            }
        }
        setSearchQuery(textToSearch);
    }

    const showDownloadButtonFn = (show?: boolean, downloadFunction?: () => void, exportName?: string, exportTooltip? :string) => {
        if (show == undefined) return;

        if (show) {
            if (downloadFunction == undefined) {
                return;
            }
            return <ExcelDownloadButton onClick={() => { return downloadFunction() }} exportedObjectName={exportName} tooltipContent={exportTooltip} />;
        }
    }
    return (
        <div className="create-new">
            <h3>All {configTypeName} configuration objects:</h3>
            {showDownloadButtonFn(showDownloadButton, downloadFunction, exportedObjectName, exportTooltip)}
            <SearchInput id="question-search-input-box" onChange={(text) => setSearchText(text)}
                className="question-search"
                text={searchQuery} />

            {displayFilterCheckBoxes &&
                <>
                    <div className="option">
                        <input
                            type="checkbox"
                            className="checkbox"
                            id="display-enabled"
                            checked={displayEnabled}
                            onChange={handleDisplayEnabled} />
                        <label htmlFor="display-enabled">
                            Show enabled
                        </label>
                    </div>

                    <div className="option">
                        <input
                            type="checkbox"
                            className="checkbox"
                            id="display-disabled"
                            checked={displayDisabled}
                            onChange={handleDisplayDisabled} />
                        <label htmlFor="display-disabled">
                            Show disabled
                        </label>
                    </div>
                </>
            }
            {onCreateNewElementClick &&
                <div className="question-item" onClick={ev => onCreateNewElementClick()}>
                    <div className="question-name"><i className="material-symbols-outlined no-symbol-fill add-icon">add_circle</i>
                        <span className="question-title">{`Create new ${configTypeName}`}</span></div>
                </div>
                }

            <ul className="question-list">
                {configElements.filter(el => IsItemShowed(el.searchableNames, el.enabled)).map((configElement, index) => {
                    const level = configElement.indentationLevel === undefined ? 1 : configElement.indentationLevel;
                    const label = includeId ? `${configElement.displayName} (id: ${configElement.id})` : `${configElement.displayName}`;
                    const isSelected = selectedItem ? selectedItem.id == configElement.id : false;
                    const className = "question-item" + (isSelected ? " selected" : "");
                    const id = isSelected ? activeElementId : `id${index}`
                    return (
                        <li className={className} id={id} key={index} onClick={ev => onSelectedElement(configElement)}>
                            {isSelected &&
                                <div style={{ paddingLeft: `${level * 20}px` }} ref={selectedItemReference}>{label}</div>
                            }
                            {!isSelected &&
                                <div style={{ paddingLeft: `${level * 20}px` }}>{label}</div>
                            }
                            <div>{
                                configElement.id > 0 ? (<span><i className="material-symbols-outlined">storage</i></span>) : (<span><i className="material-symbols-outlined">map</i></span>)
                            }</div>
                        </li>);
                })}
            </ul>
        </div>
    );
};

export default ConfigurationElementList;
