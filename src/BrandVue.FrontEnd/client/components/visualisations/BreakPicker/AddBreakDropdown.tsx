import React from "react";
import { DropdownToggle } from "reactstrap";
import { Metric } from "../../../metrics/metric";
import { ReportVariableAppendType, SavedBreakCombination } from "../../../BrandVueApi";
import { useSavedBreaksContext } from "../Crosstab/SavedBreaksContext";
import { SharedDropdownMenu } from "../shared/SharedDropdownMenu";
import { getMatchedSavedBreaks } from "./BreaksDropdownHelper";
import { getMatchedMetrics } from "../../../metrics/MetricDropdownHelper";

interface IAddBreakDropdownProps {
    metrics: Metric[];
    isPrimaryAction?: boolean;
    isDisabled?: boolean;
    addBreak: (newBreak: SavedBreakCombination | Metric) => void;
    groupCustomVariables: boolean;
    showCreateVariableButton?: boolean | undefined;
}

const AddBreakDropdown = (props: IAddBreakDropdownProps) => {
    const [searchQuery, setSearchQuery] = React.useState<string>("");
    const { savedBreaks } = useSavedBreaksContext();

    const getToggleElement = () => {
        return (
            <DropdownToggle
                caret
                className={props.isPrimaryAction ? "primary-button" : "hollow-button"}
                tag="button"
                disabled={props.isDisabled}
            >
                <i className="material-symbols-outlined">add</i>
                <div>Add break</div>
            </DropdownToggle>
        );
    };

    const getDropdownItems = () => {
        return (
            <>
                {getMatchedSavedBreaks(savedBreaks, searchQuery, props.addBreak)}
                {getMatchedMetrics(props.metrics, searchQuery, props.groupCustomVariables, props.addBreak, false)}
            </>
        );
    };

    return (
        <SharedDropdownMenu
            dropdownItems={getDropdownItems()}
            toggleElement={getToggleElement()}
            selectNone={() =>{}}
            showCreateVariableButton={props.showCreateVariableButton}
            disabled={props.isDisabled}
            shouldCreateWaveVariable={false}
            reportVariableAppendType={ReportVariableAppendType.Breaks}
            searchQuery={searchQuery}
            setSearchQuery={setSearchQuery}
        />
    );
};

export default AddBreakDropdown;