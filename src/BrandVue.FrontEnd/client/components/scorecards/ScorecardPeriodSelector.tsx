import React from "react";
import { Dropdown, DropdownToggle, DropdownMenu, DropdownItem, Button } from "reactstrap";
import * as PageHandler from "../PageHandler";
import { dsession } from "../../dsession";
import { IAverageDescriptor, MakeUpTo } from "../../BrandVueApi";
import { useState } from "react";
import { useAppDispatch, useAppSelector } from "client/state/store";
import { setScorecardPeriod } from "client/state/timeSelectionSlice";

interface IScorecardPeriodSelectorProps {
    session: dsession;
    pageHandler: PageHandler.PageHandler;
    averages: IAverageDescriptor[];
    handlePeriodChange(s: IAverageDescriptor);
}

const scorecardAverages = new Set([MakeUpTo.WeekEnd, MakeUpTo.MonthEnd, MakeUpTo.QuarterEnd, MakeUpTo.CalendarYearEnd]);

const ScorecardPeriodSelector = (props: IScorecardPeriodSelectorProps) => {
    const [dropdownOpen, setDropdown] = useState<boolean>(false);
    const dispatch = useAppDispatch();
    const scPeriodId = useAppSelector((state) => state.timeSelection.scorecardPeriod);
    const scPeriod = props.session.getScoreCardAverageByIdOrDefault(scPeriodId ?? undefined);
    const change = (e, period: IAverageDescriptor) => {
        props.handlePeriodChange(period);
        dispatch(setScorecardPeriod(period.averageId));
    };

    const toggle = () => {
        setDropdown(!dropdownOpen);
    };

    const validAverages = props.averages.filter((a) => scorecardAverages.has(a.makeUpTo));
    var availableNumberOfAverages = validAverages.length;
    if (availableNumberOfAverages == 0) {
        return <></>;
    }
    if (availableNumberOfAverages == 1) {
        return (
            <Button className="averageSelector me-3">
                <span> {scPeriod.displayName}</span>
            </Button>
        );
    }

    return (
        <div>
            <Dropdown isOpen={dropdownOpen} toggle={toggle} className="d-inline-block styled-dropdown">
                <DropdownToggle caret className="btn-menu styled-toggle">
                    {scPeriod.displayName}
                </DropdownToggle>
                <DropdownMenu>
                    {validAverages.map((v, i) => (
                        <DropdownItem key={i} onClick={(e) => change(e, v)}>
                            {v.displayName}
                        </DropdownItem>
                    ))}
                </DropdownMenu>
            </Dropdown>
        </div>
    );
};
export default ScorecardPeriodSelector;
