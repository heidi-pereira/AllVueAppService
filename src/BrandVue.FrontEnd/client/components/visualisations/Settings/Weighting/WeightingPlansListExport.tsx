import React from "react";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from "reactstrap";
import style from "./WeightingPlansListExport.module.less"
import { DropDownItemDescription } from "./WeightingPlansListItem";
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from "./Controls/MaterialSymbol";

interface WeightingPlansListExportProps {
    exportButtonMenuItems: DropDownItemDescription[];
}

const WeightingPlansListExport = (props: WeightingPlansListExportProps) => {
    const [exportDropdownOpen, setExportDropdownOpen] = React.useState<boolean>(false);

    const toggleExportDropdownOpen = (e: React.MouseEvent) => {
        e.stopPropagation();
        setExportDropdownOpen(!exportDropdownOpen);
    }

    return  <ButtonDropdown isOpen={exportDropdownOpen} toggle={toggleExportDropdownOpen} id="dropdown-button-drop-up" drop="start" key="up" className={`${style.exportButton}`} >
        <DropdownToggle caret className="btn-menu styled-dropnone" aria-label="Select a period">
            <MaterialSymbol symbolType={MaterialSymbolType.download} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} />
            </DropdownToggle>
        <div className={style.exportWeightsDropdownMenu}>
                <DropdownMenu>
                    {props.exportButtonMenuItems.map(menuItem =>
                        <DropdownItem
                            key={menuItem.Id}
                            header={menuItem.IsHeader}
                            onClick={(e) => menuItem.onClicked?.()}>
                            {menuItem.Symbol && <MaterialSymbol symbolType={menuItem.Symbol} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} />}
                            {menuItem.Text}
                        </DropdownItem>
                    )
                    }
                </DropdownMenu>
            </div>
            </ButtonDropdown>
}


export default WeightingPlansListExport;
