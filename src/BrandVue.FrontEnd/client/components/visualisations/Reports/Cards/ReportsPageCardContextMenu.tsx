import React from 'react';
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { useState } from "react";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { dataPageUrl } from '../../../helpers/PagesHelper';
import {Metric} from "../../../../metrics/metric";
import { PartDescriptor} from "../../../../BrandVueApi";
import { PageHandler } from '../../../PageHandler';
import {useNavigate} from "react-router-dom";


interface IReportsPageCardContextMenuProps {
    metric: Metric | undefined;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    canEditReport: boolean;
    removeFromReport(): void;
    canExploreData: boolean;
    duplicatePart(partDescriptor: PartDescriptor): void;
    currentPart: PartDescriptor|undefined;
}

const ReportsPageCardContextMenu = (props: IReportsPageCardContextMenuProps) => {
    const [dropdownOpen, setDropdownOpen] = useState<boolean>(false);
    const navigate = useNavigate();
    const toggleDropdown = (e: React.MouseEvent) => {
        e.stopPropagation();
        setDropdownOpen(!dropdownOpen);
    }

    const goToData = (e: React.MouseEvent) => {
        e.stopPropagation();
        props.googleTagManager.addEvent("reportsPageGoToCrosstab", props.pageHandler);
        navigate(dataPageUrl(props.metric?.urlSafeName));
    }

    const removeFromReport = (e: React.MouseEvent) => {
        e.stopPropagation();
        props.removeFromReport();
    }

    const duplicateTable = (event: React.MouseEvent) => {
        event.stopPropagation();
        if (props.currentPart) {
            props.duplicatePart(props.currentPart);
        }
    }


    const menuClass = `styled-dropdown card-menu ${dropdownOpen ? 'dropdownopen' : ''}`;
    if (props.canEditReport) {
        return (
            <>
                <ButtonDropdown isOpen={dropdownOpen} toggle={() => setDropdownOpen(!dropdownOpen)} className={menuClass}>
                    <div onClick={toggleDropdown}>
                       <DropdownToggle className={"btn-menu styled-toggle"}>
                            <i className="material-symbols-outlined">more_vert</i>
                        </DropdownToggle>
                    </div>
                    <DropdownMenu end>
                        {props.canExploreData &&
                            <DropdownItem onClick={goToData}>
                                <i className="material-symbols-outlined">grid_on</i>Explore data
                            </DropdownItem>
                        }
                        {props.currentPart &&
                            <DropdownItem onClick={duplicateTable}>
                                <i className="material-symbols-outlined menu-icon">content_copy</i>Duplicate
                            </DropdownItem>
                        }

                        <DropdownItem onClick={(e) => removeFromReport(e)}>
                            <i className="material-symbols-outlined" >delete</i>Remove
                        </DropdownItem>

                    </DropdownMenu>
                </ButtonDropdown>
            </>
        );
    }
    return null;
}

export default ReportsPageCardContextMenu;