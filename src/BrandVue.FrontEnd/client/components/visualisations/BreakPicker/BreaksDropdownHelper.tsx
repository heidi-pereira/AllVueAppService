import { DropdownItem } from "reactstrap";
import { SavedBreakCombination } from "../../../BrandVueApi";

export enum BreakPickerParent {
    Crosstab = "crosstab",
    Report = "report",
    Modal = "report-modal",
}

export const getMatchedSavedBreaks = (breaks: SavedBreakCombination[],
    searchQuery: string,
    addBreak: (savedBreak: SavedBreakCombination) => void) => {

    let matchedBreaks = breaks;

    if (searchQuery && searchQuery.trim() != '') {
        matchedBreaks = savedBreaksThatMatchSearchText(searchQuery, breaks);
    }

    const [sharedBreaks, privateBreaks] = separateBreaksByGenerationType(matchedBreaks);

    return (
        <>
            {sharedBreaks?.length > 0 && <div className="dropdown-item title">Shared Breaks</div>}
            {sharedBreaks.map(savedBreak => getSavedBreakElement(savedBreak, addBreak))}
            {privateBreaks.length > 0 && <div className="dropdown-item title">My Breaks</div>}
            {privateBreaks?.map(savedBreak => getSavedBreakElement(savedBreak, addBreak))}
        </>
    )
}

export const separateBreaksByGenerationType = (breaks: SavedBreakCombination[]) => {
    const sharedBreaks = breaks.filter(b => b.isShared == true);
    const privateBreaks = breaks.filter(b => b.isShared == false);

    return [sharedBreaks, privateBreaks]
}

export const savedBreaksThatMatchSearchText = (searchText: string, savedBreaks: SavedBreakCombination[]): SavedBreakCombination[] => {
    const normalizedSearchText = searchText.trim().toLocaleUpperCase();
    return savedBreaks.filter(savedBreak => savedBreak.name.toLocaleUpperCase().includes(normalizedSearchText));
};

const getSavedBreakElement = (savedBreak: SavedBreakCombination,
    addBreak: (b:SavedBreakCombination) => void) => {
    return (
        <DropdownItem key={savedBreak.id} onClick={() => addBreak(savedBreak)} title={savedBreak.name} className="tabbed">
            <div className="flex-container">
                <span className={"name-display"}>{savedBreak.name}</span>
            </div>
        </DropdownItem>
    );
}
