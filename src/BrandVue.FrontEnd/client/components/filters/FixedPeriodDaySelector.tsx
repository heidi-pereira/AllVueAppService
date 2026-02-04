import React from "react";
import { Dropdown, DropdownToggle, DropdownMenu } from "reactstrap";
import Calendar from "react-calendar";
import moment from "moment";
import { getDateInUtc } from "../helpers/PeriodHelper";
import FixedPeriodUnitDescriptions from "../helpers/FixedPeriodUnitDescriptions";
import { MakeUpTo } from "../../BrandVueApi";
import { IDropdownToggleAttributes } from "../helpers/DropdownToggleAttributes";

interface IFixedPeriodDaySelectorProps {
    day: Date;
    dateStart: Date;
    dateEnd: Date;
    onSelectDay: (day: Date) => void;
    buttonAttr?: IDropdownToggleAttributes;
}

interface IFixedPeriodDaySelectorState {
    dropdownOpen: boolean;
}

export default class FixedPeriodDaySelector extends React.Component<IFixedPeriodDaySelectorProps, IFixedPeriodDaySelectorState> {
    constructor(props) {
        super(props);

        this.toggle = this.toggle.bind(this);
        this.onChange = this.onChange.bind(this);
        this.state = {
            dropdownOpen: false
        };
    }

    toggle() {
        this.setState(prevState => ({
            dropdownOpen: !prevState.dropdownOpen
        }));
    }

    onChange(date: Date | Date[]) {
        let changedDate: Date = new Date();
        if (Array.isArray(date)) {
            if (date.length > 0) {
                changedDate = date[0];
            }
        } else {
            changedDate = date;
        }
        const utcDate = getDateInUtc(changedDate.getFullYear(), changedDate.getMonth() + 1, changedDate.getDate());
        this.toggle();
        this.props.onSelectDay(utcDate);
    }

    render() {
        return (
            <div>
                <div className="btn-group bootstrap-select fit-width">
                    <Dropdown isOpen={this.state.dropdownOpen} toggle={this.toggle} className="styled-dropdown" >
                        <DropdownToggle caret className="styled-toggle" aria-label={FixedPeriodUnitDescriptions.getPeriodLabel(MakeUpTo.Day)} {...this.props.buttonAttr}>
                            {moment.utc(this.props.day).format("MMMM D, YYYY")}
                        </DropdownToggle>
                        <DropdownMenu>
                            <Calendar
                                onChange={this.onChange}
                                value={this.props.day}
                                minDate={this.props.dateStart}
                                maxDate={this.props.dateEnd}

                            />
                        </DropdownMenu>
                    </Dropdown>
                </div>
            </div>
        );
    }
}