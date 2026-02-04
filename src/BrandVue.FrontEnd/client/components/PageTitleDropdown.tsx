import React from 'react';
import { Dropdown, DropdownItem, DropdownMenu, DropdownToggle } from 'reactstrap';

interface IPageTitleDropdownProps
{
    itemList: string[];
    title: string;
    activeOption: string;
    updateSession: (newValue: string) => void;
}

interface IPageTitleDropdownState
{
    dropdownOpen: boolean;
}

export default class PageTitleDropdown extends React.Component<IPageTitleDropdownProps, IPageTitleDropdownState> {

    constructor(props) {
        super(props);

        this.state = {
            dropdownOpen: false
        };
    }

    toggle() {
        this.setState(prevState => ({
            dropdownOpen: !prevState.dropdownOpen
        }));
    }

    render() {

        return (
            <div className="page-title-menu">
                <div className="page-title-label">{this.props.title}</div>
                <Dropdown className="styled-dropdown" isOpen={this.state.dropdownOpen} toggle={() => this.toggle()} >
                    <DropdownToggle id="viewSelectorToggle" className="styled-toggle" caret>
                        {this.props.activeOption}
                    </DropdownToggle>
                    <DropdownMenu>
                        {this.props.itemList.map(item => 
                           <DropdownItem 
                               key={item}
                               className={"search-item"}
                               title={item}
                                onClick={() => this.props.updateSession(item)}>
                                <span>{item}</span>
                            </DropdownItem>
                           )}
                    </DropdownMenu>
                </Dropdown>
            </div>
        );
    }
}