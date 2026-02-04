import React from 'react';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { dsession } from "../dsession";
import {QueryStringParamNames, useWriteVueQueryParams} from './helpers/UrlHelper';
import {useState} from "react";
import { IGoogleTagManager } from '../googleTagManager';
import { MixPanel } from './mixpanel/MixPanel';
import {ComparisonPeriodSelection} from "../BrandVueApi";
import {useLocation, useNavigate} from "react-router-dom";
import { useSelectedBreaks } from "../state/entitySelectionHooks";

const ComparisonPeriodSelector = (props: { session: dsession, googleTagManager: IGoogleTagManager, comparisonPeriodSelection: ComparisonPeriodSelection }) => {
    const [dropdownOpen, setDropdownOpen] = useState(false);
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    const activeBreaks = useSelectedBreaks();
    const toggle = () => {
        setDropdownOpen(!dropdownOpen);
    }

    const select = (comparisonPeriodSelection: ComparisonPeriodSelection) => {
        MixPanel.track("comparisonPeriodChanged");
        props.session.activeView.curatedFilters.comparisonPeriodSelection = comparisonPeriodSelection;
        setQueryParameter(QueryStringParamNames.period, comparisonPeriodSelection);
        props.googleTagManager.addEvent("changeComparisonPeriod", props.session.pageHandler, { value: comparisonPeriodSelection.toString() });
    }

    const getText = (comparisonPeriodSelection: ComparisonPeriodSelection) => {
        switch (comparisonPeriodSelection) {
            case ComparisonPeriodSelection.CurrentPeriodOnly:
                return "Current period";
            case ComparisonPeriodSelection.CurrentAndPreviousPeriod:
                return "Current & previous period";
            case ComparisonPeriodSelection.SameLastYear:
                return "Current & same period last year";
            case ComparisonPeriodSelection.LastSixMonths:
                return "Current & six months ago";
        }
    }

    const shouldDisablePeriod = (comparisonPeriodSelection: ComparisonPeriodSelection) => {
        if (props.session.pageHandler.currentPanesShouldRestrictToCurrentPeriod(activeBreaks)) {
            return comparisonPeriodSelection !== ComparisonPeriodSelection.CurrentPeriodOnly;
        }
        return false;
    }
    
    return (



        <span className="not-exported periodsSelector">
            <ButtonDropdown isOpen={dropdownOpen} toggle={toggle} className="styled-dropdown">
                <DropdownToggle caret className="btn-menu styled-toggle">
                    <span className="selectorShowPrefix" />{getText(props.comparisonPeriodSelection)}
                </DropdownToggle>
                <DropdownMenu>
                    {Object.keys(ComparisonPeriodSelection)
                        .map(k => {
                            const period = ComparisonPeriodSelection[k];
                            const periodTxt = getText(period);
                            if (periodTxt !== undefined) {
                                return (

                                    <DropdownItem key={k} onClick={() => select(period)} disabled={shouldDisablePeriod(period)}>
                                        {getText(period)}
                                    </DropdownItem>
                                );
                            }
                            return;
                        })
                    }
                </DropdownMenu>
            </ButtonDropdown>
        </span>
    );
}
export default ComparisonPeriodSelector