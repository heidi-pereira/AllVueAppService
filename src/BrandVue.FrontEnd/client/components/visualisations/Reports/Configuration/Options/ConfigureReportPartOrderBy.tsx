import React from 'react';
import { PartDescriptor, ReportType, ReportOrder } from "../../../../../BrandVueApi";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { descriptionOfOrder } from "../../../../helpers/SurveyVueUtils";
import { PartType } from '../../../../panes/PartType';

interface IConfigureReportPartOrderByProps {
    reportType: ReportType;
    reportOrderBy: ReportOrder;
    part: PartDescriptor;
    savePartChanges(newPart: PartDescriptor): void;
}

const ConfigureReportPartOrderBy = (props: IConfigureReportPartOrderByProps) => {
    const [isDropdownOpen, setIsDropdownOpen] = React.useState<boolean>(false);
    const toggleButtonDropdown = () => setIsDropdownOpen(!isDropdownOpen);

    const order = props.part.reportOrder ?? props.reportOrderBy;
    const useReportSettings = props.part.reportOrder == undefined;
    const reportSettingsDescription = `Default (${descriptionOfOrder(props.reportOrderBy)})`;
    const description = useReportSettings ? reportSettingsDescription : descriptionOfOrder(order);
    const canSetOrder = !(props.part.partType == PartType.ReportsCardText || props.part.partType == PartType.ReportsCardLine);
    const isFunnelPart = props.part.partType === PartType.ReportsCardFunnel;

    const setTheOrder = (newOrder: ReportOrder | undefined) => {
        if (props.part.reportOrder !== newOrder) {
            const modifiedPart = new PartDescriptor(props.part);
            modifiedPart.reportOrder = newOrder;
            props.savePartChanges(modifiedPart);
        }
    }
    
    if (!canSetOrder) {
        return null;
    }

    const orders = [ReportOrder.ScriptOrderDesc, ReportOrder.ScriptOrderAsc, ReportOrder.ResultOrderDesc, ReportOrder.ResultOrderAsc];
    return (
        <>
            <label className="category-label">Sort order</label>
            <div className="category-properties">
                <ButtonDropdown isOpen={isDropdownOpen} toggle={toggleButtonDropdown} className="configure-option-dropdown">
                    <DropdownToggle className="toggle-button" disabled={isFunnelPart}>
                        <span>{description}</span>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        <DropdownItem onClick={() => setTheOrder(undefined)}>{reportSettingsDescription}</DropdownItem>
                        <div className="separator"></div>
                        {orders.map((order, i) =>
                            <DropdownItem key={i} onClick={() => setTheOrder(order)}>{descriptionOfOrder(order)}</DropdownItem>
                        )}
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
        </>
    );
}

export default ConfigureReportPartOrderBy