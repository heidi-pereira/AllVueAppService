import React from "react";
import "@Styles/searchBar.scss";

interface ISearchBarProps  {
    search: string,
    setSearch: (search: string) => void,
    autoFocus: boolean
}

const SearchBar = (props: ISearchBarProps) => {
    
    return (
        <div className='search'>
            <input
                type="text"
                id="Search"
                placeholder="Search"
                className="form-control"
                value={props.search}
                onChange={(e) => {props.setSearch(e.currentTarget.value)}}
                autoFocus={props.autoFocus}
            />
            <label htmlFor="Search" className="material-symbols-outlined search-icon">search</label>
        </div>
    );
}

export default SearchBar
