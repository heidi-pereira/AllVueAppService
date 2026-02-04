import { EntityInstance } from "../../../../../entity/EntityInstance";

export const getDropdownLabel = (selectedInstances: EntityInstance[] ) => {
    if (selectedInstances.length === 0) {
        return "Select...";
    } 
    if (selectedInstances.length == 1) {
        if (selectedInstances[0].name.length >= 35) {
            return `${selectedInstances[0].name.substring(0, 35)}...`;
        }
        return selectedInstances[0].name;
    }
    return `${selectedInstances.length} selected`;
}
