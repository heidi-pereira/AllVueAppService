import React from 'react';
import { DropdownItem } from 'reactstrap';
import { EntitySet } from "../entity/EntitySet";
import SearchInput from "./SearchInput";

interface IFilteredEntitySetListProps {
    onItemClick(entitySet: EntitySet): void,
    onItemHover(entitySet: EntitySet): void,
    onFilterQueryUpdate(filterQuery: string): void,
    getItemMarkup(name: string): React.ReactElement,
    filteredItems: EntitySet[],
    activeEntitySet: EntitySet,
    hoveredSetName: string,
}

export const FilteredEntitySetList = (props: IFilteredEntitySetListProps) => {

    const [searchText, setSearchText] = React.useState<string>("");
    const myEntitySets: EntitySet[] = [];
    const sectorSets: EntitySet[] = [];

    React.useEffect(() => {
        window.requestAnimationFrame(() => {
            const selectedElement = document.getElementById("activeSetDropdownItem");
            if (selectedElement) {
                setTimeout(() => selectedElement.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "start" }), 50);
            }
        });
    }, [props.filteredItems]);

    props.filteredItems.forEach(set => {
        if (set.isSectorSet) {
            sectorSets.push(set);
        } else {
            myEntitySets.push(set);
        }
    });

    const getClassName = (isActiveSet: boolean, tabbed: boolean) => {
        var className = "search-item";
        if (isActiveSet) {
            className += " selected-dropdown-item";
        }
        if (tabbed) {
            className += " tabbed";
        }
        return className;
    }

    const getDropdownItem = (set: EntitySet, tabbed: boolean) => {
        const isActiveSet = set.name === props.hoveredSetName;
        const properties = {
            key: set.name,
            className: getClassName(isActiveSet, tabbed),
            onClick: () => props.onItemClick(set),
            onMouseEnter: () => props.onItemHover(set),
            id: isActiveSet ? "activeSetDropdownItem" : "",
        }

        return (
            <DropdownItem {...properties}>
                {props.getItemMarkup(set.name)}
            </DropdownItem>
        );
    }

    const getDropdownItems = () => {

        if (myEntitySets.length === 0) {
            return (
                sectorSets.map(set => getDropdownItem(set, false)
                )
            );
        }

        if (sectorSets.length === 0) {
            return (
                myEntitySets.map(set => getDropdownItem(set, false)
                )
            );
        }

        return (
            <>
                <DropdownItem className="search-header">
                    <span>My {props.activeEntitySet.type.displayNameSingular.toLocaleLowerCase()} groups</span>
                </DropdownItem>

                {myEntitySets.map(set => getDropdownItem(set, true)
                )}

                <DropdownItem className="search-header">
                    <span>Sector groups</span>
                </DropdownItem>

                {sectorSets.map(set => getDropdownItem(set, true)
                )}
            </>
        );
    };

    return (
        <div className="items-and-search">
            <div className="search-container">
                <SearchInput id="entity-set-search" onChange={(text) => { setSearchText(text); props.onFilterQueryUpdate(text); }} text={searchText} />
            </div>
            <div className="items">
                {getDropdownItems()}
            </div>
        </div>
    );
};
