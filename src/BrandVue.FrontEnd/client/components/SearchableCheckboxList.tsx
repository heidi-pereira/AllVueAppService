import React from 'react';
import SearchInput from './SearchInput';

export type ListItem = {
    Id: number,
    Name: string,
    Keywords: string[]
}

interface ISearchableCheckboxListProps {
    availableItems: ListItem[];
    selectedItems: ListItem[];
    updateSelectedItems(selected: ListItem[]): void;
    additionalButton?: JSX.Element;
    disableSelectAll?: boolean;
}

const SearchableCheckboxList = (props: ISearchableCheckboxListProps) => {
    const [searchText, setSearchText] = React.useState<string>("");

    const getItemsToShow = () => {
        const textToSearch = searchText.trim().toLowerCase();
        return props.availableItems.filter(i =>
            i.Name.toLowerCase().includes(textToSearch) ||
            (i.Keywords?.length > 0 && i.Keywords.filter(k => k.toLowerCase().includes(textToSearch)).length > 0));
    }

    const itemsToShow = getItemsToShow();

    const toggleItem = (item: ListItem) => {
        const index = props.selectedItems.findIndex(i => i.Name === item.Name);
        if (index >= 0) {
            const newItems = [...props.selectedItems];
            newItems.splice(index, 1);
            props.updateSelectedItems(newItems);
        } else {
            props.updateSelectedItems([
                ...props.selectedItems,
                item
            ]);
        }
    }

    const selectAll = () => {
        const newItems = [
            ...props.selectedItems,
            ...itemsToShow.filter(i => !isItemSelected(i)),
        ];
        props.updateSelectedItems(newItems);
    }

    const clearSelected = () => {
        props.updateSelectedItems([]);
    }

    const isItemSelected = (item: ListItem) => {
        return props.selectedItems.some(i => i.Name === item.Name);
    }

    const noResults = itemsToShow.length == 0;

    return (
        <div className="searchable-checkbox-list">
            <div className="search-container">
                <SearchInput id="search-input-component" onChange={(text) => setSearchText(text)} className="flat-search" text={searchText} />
            </div>
            <div className="count-and-buttons">
                <div className="buttons">
                    {!props.disableSelectAll && <button className="secondary-button" onClick={selectAll}>Select all</button>}
                    <button className="secondary-button" onClick={clearSelected}>Clear all</button>
                    {props.additionalButton}
                </div>
                <div className="count">
                    <strong>{props.selectedItems.length}</strong> of {props.availableItems.length} selected
                </div>
            </div>
            <div className="item-list">
                {itemsToShow.length > 0 &&
                    itemsToShow.map((item, i) => {
                            const key = `item-${item.Name}-${i}`;
                            return <div className="item" key={key}>
                                <input type="checkbox" className="checkbox" id={key} checked={isItemSelected(item)} onChange={() => toggleItem(item)} />
                                <label htmlFor={key}><strong>{item.Name}</strong></label>
                            </div>
                        })
                }
                {noResults &&
                    <div className="no-results">No results</div>
                }
            </div>
        </div>
    );
};

export default SearchableCheckboxList;