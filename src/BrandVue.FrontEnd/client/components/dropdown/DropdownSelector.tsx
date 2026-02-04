import React from "react";
import { Dispatch, PropsWithChildren, ReactNode, useEffect, useState } from "react";
import { Dropdown, DropdownItem, DropdownMenu, DropdownToggle } from 'reactstrap';

export const renderItemWithSearch = (name: string, filterQuery: string): ReactNode => {
    if (filterQuery.length === 0 || name == null || name.toLowerCase().indexOf(filterQuery.toLowerCase()) < 0) {
        return <span key={name}>{name}</span>;
    }

    const startIndex = name.toLowerCase().indexOf(filterQuery.toLowerCase());
    return (
        <span key={name}>
            <span>{name.substring(0, startIndex)}</span>
            <span style={{ fontWeight: 'bold' }}>{name.substr(startIndex, filterQuery.length)}</span>
            <span>{name.substring(startIndex + filterQuery.length)}</span>
        </span>
    );
};

interface IProps<T extends object> {
    label: string;
    items: T[] | DropdownItemGroup<T>[];
    selectedItem: T;
    onSelected: (item: T) => void;
    itemDisplayText: (item: T) => string;
    asButton: boolean,
    showLabel: boolean,
    itemKey?: (item: T) => string;
    renderItem?: (item: T, searchQuery: string) => React.ReactNode;
    filterPredicate?: (item: T, searchQuery: string) => boolean;
    sortFunction?: (a: T, b: T, searchQuery: string) => number;
    renderItemContextPanel?: (item: T, searchQuery: string) => React.ReactNode;
    materialIcon?: string;
    textOverride?: string;
}

export class DropdownItemGroup<T extends object> {
    public heading: string;
    public items: T[];

    constructor(heading: string, items: T[]) {
        this.heading = heading;
        this.items = items;
    }
}

type DropDownItemOrGroup<T extends object> = T | DropdownItemGroup<T>;

const isGrouped = <T extends object>(items: Array<DropDownItemOrGroup<T>>): items is Array<DropdownItemGroup<T>> => {
    return items.every(i => (i as DropdownItemGroup<T>).heading);
};

const DropdownSelector = <T extends object,>(props: PropsWithChildren<IProps<T>>) => {

    const [filter, setFilter] = useState("");
    const [dropdownOpen, setDropdownOpen] = useState<boolean>(false);
    const initialActiveItem = props.renderItemContextPanel ? props.selectedItem : undefined;
    const [activeItem, setActiveItem] = useState(initialActiveItem);

    const escFunction = (event: KeyboardEvent) => {
        if(event.key === "Escape" || event.key === "Esc") {
            setDropdownOpen(false);
        }
    };

    useEffect(() => {
        document.addEventListener("keydown", escFunction, false);
        return () => document.removeEventListener("keydown", escFunction, false);
    }, []);

    const filterQuery = (item: T) => {
        if (props.filterPredicate === undefined) return true;
        const trimmed = filter.trim();
        if (trimmed.length === 0) return true;
        return props.filterPredicate(item, trimmed);
    };

    const toggle = () => {
        if (!dropdownOpen) {
            setFilter("");
            setActiveItem(initialActiveItem);
        }
        setDropdownOpen(!dropdownOpen);
    };

    const defaultRenderItem = (item: T, filter: string) => renderItemWithSearch(props.itemDisplayText(item), filter);
    const renderItem: (item: T, filter: string) => React.ReactNode = props.renderItem ?? defaultRenderItem;
    const selectedDisplayText = props.textOverride ?? props.itemDisplayText(props.selectedItem);
    const itemKey = props.itemKey ?? props.itemDisplayText;

    const getItemsInner = (items: Array<T>, ...addedClasses: string[]): Array<ReactNode> => {
        const filteredItems = items.filter(i => filterQuery(i));
        const sortedItems = props.sortFunction ? filteredItems.sort((a, b) => props.sortFunction!(a, b, filter.trim())) : filteredItems;
        return sortedItems.map((item, index) => {
            const classNames = ["search-item"].concat(addedClasses);
            if(item === activeItem) classNames.push("selected-dropdown-item");
            return (
                <DropdownItem key={`${itemKey(item)}-${index}`}
                             className={classNames.join(" ")}
                             title={props.itemDisplayText(item).length > 28 ? props.itemDisplayText(item) : undefined}
                             onClick={() => props.onSelected(item)}
                             onMouseEnter={() => setActiveItem(item)}
                >
                    {renderItem(item, filter.trim())}
                </DropdownItem>
            );
        });
    };

    const getItems = (items: Array<T> | Array<DropdownItemGroup<T>>): ReactNode => {

        if (items.length === 0) return;

        if (isGrouped(items)) {
            return items.map(g => {
                const groupItems = getItemsInner(g.items, "tabbed");
                if (groupItems.length > 0) {
                    return (
                        <React.Fragment key={g.heading}>
                            <DropdownItem key={g.heading} className="search-header">{g.heading}</DropdownItem>
                            {groupItems}
                        </React.Fragment>
                    );
                }
            });
        }

        return getItemsInner(items as Array<T>);
    };

    const getLabel = (showLabel: boolean) => {
        if (showLabel)
            return (
                <div className="page-title-label">{props.label}</div>
            );
    }

    const getToggle = (asButton: boolean) => {
        if (asButton) {
            return (
                <DropdownToggle className="hollow-button" title={selectedDisplayText.length > 28 ? selectedDisplayText : undefined} caret >
                    <i className="material-symbols-outlined">{props.materialIcon}</i>
                    <div>{selectedDisplayText}</div>
                </DropdownToggle>
            );
        }

        return (
            <DropdownToggle className="styled-toggle" caret >
                <span className="title-text" title={selectedDisplayText.length > 28 ? selectedDisplayText : undefined}>
                    {selectedDisplayText}
                </span>
            </DropdownToggle>
        );
    }
    
    return (
        <div className="page-title-menu">
            {getLabel(props.showLabel)}
            <Dropdown className="styled-dropdown" isOpen={dropdownOpen} toggle={toggle}>
                {getToggle(props.asButton)}
                <DropdownMenu className={`custom-dropdown-menu${props.asButton ? " spaced": ""}`}>
                    <div className="custom-dropdown">
                        <div className="items-and-search">
                            {props.filterPredicate && <div className="search-container">
                                <div className="search">
                                    <input type="search" autoFocus={true} id="entity-set-search" className="search-input" name="entity-set-search" placeholder="Search" autoComplete="nope" onChange={(e) => setFilter(e.target.value)} />
                                    <label htmlFor="entity-set-search" className="material-symbols-outlined search-icon">search</label>
                                </div>
                            </div>}
                            <div className="items">
                                {getItems(props.items)}
                            </div>
                        </div>
                        {props.renderItemContextPanel && props.renderItemContextPanel(activeItem!, filter.trim())}
                    </div>
                </DropdownMenu>
            </Dropdown>
        </div>
    );
};

export default DropdownSelector;