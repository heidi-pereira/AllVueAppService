import React from "react";
import { DropdownToggle, ButtonDropdown, DropdownMenu, DropdownItem } from 'reactstrap';
import style from "./ButtonWithDropdown.module.less";

interface IDropdownItem {
    itemName: string;
    onClick(): void;
    category?: string;
}

interface IButtonWithDropdownProps {
    dropdownItems: IDropdownItem[];
    toggleElement: React.ReactElement<DropdownToggle>;
}

const ButtonWithDropdown = (props: IButtonWithDropdownProps) => {
    const [isButtonDropdownOpen, setIsButtonDropdownOpen] = React.useState(false);

    const toggleSaveButtonDropdown = () => {
        setIsButtonDropdownOpen(!isButtonDropdownOpen);
    }

    const getDropdownItem = (dropdownItem: IDropdownItem, index: number) => {
        return (
            <DropdownItem key={`item-${index}`} onClick={() => dropdownItem.onClick()}>
                <div className="name-container">
                    <span className='title' title={dropdownItem.itemName}>{dropdownItem.itemName}</span>
                </div>
            </DropdownItem>
        );
    }

    const getCategorisedItems = (items: IDropdownItem[]) => {
        const categorisedItems: {[key: string]: IDropdownItem[]} = {};
        items.forEach(item => {
            if (item.category) {
                if (categorisedItems[item.category]) {
                    categorisedItems[item.category].push(item);
                } else {
                    categorisedItems[item.category] = [item];
                }
            } else {
                if (categorisedItems[""]) {
                    categorisedItems[""].push(item);
                } else {
                    categorisedItems[""] = [item];
                }
            }
        });
        return Object.keys(categorisedItems).map((category, i) => {
            const categoryHeader =
                <DropdownItem header key={`item-header-${i}`} className={style.header}>
                    <div className="name-container">
                        <span className='title' title={category}>{category}</span>
                    </div>
                </DropdownItem>
            const categoryItems = Object.values(categorisedItems[category]).map((item, j) => getDropdownItem(item, j));
            return [categoryHeader, categoryItems];
        });
    }

    const getItemList = (items: IDropdownItem[]) => {
        if (items.some(item => item.category !== undefined)) {
            return getCategorisedItems(items)
        }

        return items.map((di, i) => getDropdownItem(di, i))
    }
    
    return (
            <div className="metric-dropdown-menu">
                <ButtonDropdown isOpen={isButtonDropdownOpen} toggle={toggleSaveButtonDropdown} className="metric-dropdown">
                {props.toggleElement}
                <DropdownMenu className="dropdown-container">
                    {getItemList(props.dropdownItems)}
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
        );
}

export default ButtonWithDropdown;