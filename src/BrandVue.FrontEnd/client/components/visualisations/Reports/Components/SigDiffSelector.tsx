import { CrosstabSignificanceType, DisplaySignificanceDifferences, SigConfidenceLevel } from "../../../../BrandVueApi";
import style from "./SigDiffSelector.module.less";
import { useCrosstabPageStateContext } from "../../Crosstab/CrosstabPageStateContext";
import { Tooltip } from "@mui/material";
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap";
import { useState } from "react";

export interface ISigDiffSelectorProps {
    highlightSignificance: boolean;
    updateHighlightSignificance(highlightSignificance: boolean): void
    displaySignificanceDifferences: DisplaySignificanceDifferences;
    updateDisplaySignificanceDifferences(displayedSignificanceDifferences: DisplaySignificanceDifferences): void;
    significanceType: CrosstabSignificanceType;
    setSignificanceType(significanceType: CrosstabSignificanceType): void;
    disableSignificanceTypeSelector: boolean;
    downIsGood: boolean;
    significanceLevel: SigConfidenceLevel;
    setSignificanceLevel(significanceLevel: SigConfidenceLevel): void;
    isAllVue: boolean;
}

const SigDiffSelector = (props: ISigDiffSelectorProps) => {
    const [significanceTypeDropdownOpen, setSignificanceTypeDropdownOpen] = useState<boolean>(false);
    const [significanceLevelDropdownOpen, setSignificanceLevelDropdownOpen] = useState<boolean>(false);
    const { crosstabPageState } = useCrosstabPageStateContext();

    const upDownCheckboxTooltipText = crosstabPageState.significanceType === CrosstabSignificanceType.CompareWithinBreak
        ? "This option is not available when comparing within breaks"
        : "";

    const significanceTypeDropdownTooltipText = props.disableSignificanceTypeSelector ? "This report doesn't allow changing the comparison type" : "";

    const significanceLevels: SigConfidenceLevel[] = Object.values(SigConfidenceLevel);
    
    const getSignificanceTypeText = (type: CrosstabSignificanceType): string => {
        switch (type) {
            case CrosstabSignificanceType.CompareToTotal:
                return "Compare values to total";
            case CrosstabSignificanceType.CompareWithinBreak:
                return "Compare values within each break";
        }
    }

    const getSignificanceLevelText = (level: SigConfidenceLevel): string => {
        switch (level) {
            case SigConfidenceLevel.Ninety:
                return "90%";
            case SigConfidenceLevel.NinetyEight:
                return "98%";
            case SigConfidenceLevel.NinetyNine:
                return "99%";
            case SigConfidenceLevel.NinetyFive:
            default:
                return "95% (Default)";
        }
    };

    return (
        <div className="significance-selector">
            <input type="checkbox"
                className="checkbox"
                id="significance-checkbox"
                data-testid="significance-checkbox"
                checked={props.highlightSignificance}
                onChange={() => props.updateHighlightSignificance(!props.highlightSignificance)} />
            <label htmlFor="significance-checkbox">
                Highlight significant values
            </label>
            { !props.isAllVue &&
                <div className="option-hint">A 95% confidence level is used to test for significance</div>
            }
            <Tooltip id="significanceTypeDropdown" placement="bottom" title={significanceTypeDropdownTooltipText} >            
                <ButtonDropdown className="significance-type-dropdown" 
                    isOpen={significanceTypeDropdownOpen}
                    toggle={() => setSignificanceTypeDropdownOpen(!significanceTypeDropdownOpen)} >
                    <DropdownToggle
                        className="toggle-button"
                        data-testid="toggle-button"
                        disabled={props.disableSignificanceTypeSelector}
                    >
                        <span>{getSignificanceTypeText(props.significanceType)}</span>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        <DropdownItem onClick={() => props.setSignificanceType(CrosstabSignificanceType.CompareToTotal)}>
                            {getSignificanceTypeText(CrosstabSignificanceType.CompareToTotal)}
                        </DropdownItem>
                        <DropdownItem onClick={() => props.setSignificanceType(CrosstabSignificanceType.CompareWithinBreak)}>
                            {getSignificanceTypeText(CrosstabSignificanceType.CompareWithinBreak)}
                        </DropdownItem>
                    </DropdownMenu>
                </ButtonDropdown>
            </Tooltip>
            { props.isAllVue && (
                <>
                    <div className={style.significanceLevelContainer}>
                        <span className={style.significanceLevelSpan}>
                            Significance 
                        </span>
                        <Tooltip id="significanceLevelDropdown" placement="bottom" title={getSignificanceLevelText(props.significanceLevel)}>            
                            <ButtonDropdown className="significance-type-dropdown significance-level-dropdown" 
                                isOpen={significanceLevelDropdownOpen}
                                toggle={() => setSignificanceLevelDropdownOpen(!significanceLevelDropdownOpen)} >
                                <DropdownToggle
                                    className="toggle-button"
                                    data-testid="sig-level-toggle-button"
                                    disabled={!props.highlightSignificance}
                                >
                                    <span>{getSignificanceLevelText(props.significanceLevel)}</span>
                                    <i className="material-symbols-outlined">arrow_drop_down</i>
                                </DropdownToggle>
                                <DropdownMenu>
                                {significanceLevels.map(level => (
                                    <DropdownItem
                                        key={level}
                                        onClick={() => props.setSignificanceLevel(level)}
                                    >
                                        {getSignificanceLevelText(level)}
                                    </DropdownItem>
                                ))}
                            </DropdownMenu>
                            </ButtonDropdown>
                        </Tooltip>
                    </div>
                    <Tooltip id="upDownCheckbox" placement="bottom" title={upDownCheckboxTooltipText} >
                        <div className={style.indentedOptions}>
                            <div className={style.hideDeviation}>
                                <input
                                    type="checkbox"
                                    className="checkbox"
                                    id="upwards-checkbox"
                                    data-testid="upwards-checkbox"
                                    disabled={!props.highlightSignificance 
                                        || props.significanceType === CrosstabSignificanceType.CompareWithinBreak
                                        || props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowUp}
                                    checked={props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowUp
                                        || props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowBoth
                                        && props.significanceType == CrosstabSignificanceType.CompareToTotal}
                                    onChange={() => props.updateDisplaySignificanceDifferences(DisplaySignificanceDifferences.ShowUp)}
                                />
                                <label htmlFor="upwards-checkbox">
                                    <ArrowUpwardIcon style={{ color: props.downIsGood ? "red" : "green", verticalAlign: "middle", marginRight: 4 }} fontSize="small" />
                                    Show increases
                                </label>
                            </div>
                            <div className={style.hideDeviation}>
                                <input
                                    type="checkbox"
                                    className="checkbox"
                                    id="downwards-checkbox"
                                    data-testid="downwards-checkbox"
                                    disabled={!props.highlightSignificance 
                                        || props.significanceType === CrosstabSignificanceType.CompareWithinBreak
                                        || props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowDown}
                                    checked={props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowDown
                                        || props.displaySignificanceDifferences == DisplaySignificanceDifferences.ShowBoth
                                        && props.significanceType == CrosstabSignificanceType.CompareToTotal}
                                    onChange={() => props.updateDisplaySignificanceDifferences(DisplaySignificanceDifferences.ShowDown)}
                                />
                                <label htmlFor="downwards-checkbox">
                                    <ArrowDownwardIcon style={{ color: props.downIsGood ? "green" : "red", verticalAlign: "middle", marginRight: 4 }} fontSize="small" />
                                    Show decreases
                                </label>
                            </div>
                        </div>
                    </Tooltip>
                </>
            )}
        </div>
    );
};

export default SigDiffSelector;