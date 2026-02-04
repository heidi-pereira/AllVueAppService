import React from "react";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem, Button } from "reactstrap";
import { MakeUpTo } from "../../BrandVueApi";
import FixedPeriodUnitDescriptions from "../helpers/FixedPeriodUnitDescriptions";
import { IDropdownToggleAttributes } from "../helpers/DropdownToggleAttributes";

interface IFixedPeriodSelectorProps {
    period: number;
    periods: { value: number, valid: boolean }[];
    makeUpTo: MakeUpTo;
    onSelectPeriod: (periodNumber: number, periodGranularity: MakeUpTo) => void;
    periodDescription: string;
    dropdownClass?: string;
    buttonAttr?: IDropdownToggleAttributes;
}

interface IFixedPeriodSelectorState {
    dropdownOpen: boolean
}

export default class FixedPeriodSelector extends React.Component<IFixedPeriodSelectorProps, IFixedPeriodSelectorState> {
    constructor(props) {
        super(props);

        this.toggle = this.toggle.bind(this);
        this.state = {
            dropdownOpen: false
        };
    }

    toggle() {
        this.setState({
            dropdownOpen: !this.state.dropdownOpen
        });
    }

    render() {

        const { period, periods, makeUpTo, onSelectPeriod, periodDescription, dropdownClass, buttonAttr } = this.props;

        const periodValue = period || 0;
        const periodDescriptionRender = periodDescription ?
            <React.Fragment>
                <DropdownItem divider />
                <DropdownItem disabled>
                    <span dangerouslySetInnerHTML={{ __html: periodDescription }} />
                </DropdownItem>
            </React.Fragment> : null;

        return (
            <div>
                <ButtonDropdown isOpen={this.state.dropdownOpen} toggle={this.toggle} className={makeUpTo + " " + (dropdownClass ?? "styled-dropdown")}>
                    <DropdownToggle caret className="btn-menu styled-toggle" aria-label={FixedPeriodUnitDescriptions.getPeriodLabel(makeUpTo)} {...buttonAttr}>
                        {FixedPeriodUnitDescriptions.getSelectedDropdownValueText(periodValue, makeUpTo)}
                    </DropdownToggle>

                    <DropdownMenu>
                        {
                            periods.map((period, i) => {
                                const primaryOptionText = FixedPeriodUnitDescriptions.getPrimaryOptionsDropdownValueText(period.value, makeUpTo);
                                let primaryElement: JSX.Element | null = null;
                                if (primaryOptionText) {
                                    primaryElement = <strong>{primaryOptionText} </strong>;
                                }

                                return (
                                    <DropdownItem disabled={!period.valid} key={i} onClick={() => onSelectPeriod(period.value, makeUpTo)}>
                                        {primaryElement}
                                        {FixedPeriodUnitDescriptions.getSecondaryOptionsDropdownValueText(period.value, makeUpTo)}
                                    </DropdownItem>
                                );
                            }
                            )
                        }
                        {periodDescriptionRender}
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
        );
    }
}