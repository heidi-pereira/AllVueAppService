import React from 'react';
import { PartDescriptor, Report } from "../../../../../BrandVueApi";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { selectCurrentReport } from 'client/state/reportSelectors';
import { useAppSelector } from 'client/state/store';

interface IConfigureReportPartExportOptionsProps {
    part: PartDescriptor;
    savePartChanges(newPart: PartDescriptor): void;
}

function getHideDataLabelsText(hide: boolean) {
    return hide ? "Hide" : "Show";
}

const ConfigureReportPartExportOptions = (props: IConfigureReportPartExportOptionsProps) => {
    const [isDropdownOpen, setIsDropdownOpen] = React.useState<boolean>(false);
    const toggleButtonDropdown = () => setIsDropdownOpen(!isDropdownOpen);
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const hideDataLabels = props.part.hideDataLabels ?? report.hideDataLabels;
    const useReportSettings = props.part.hideDataLabels == undefined;
    const reportSettingsDescription = `Default (${getHideDataLabelsText(report.hideDataLabels)})`;
    const description = useReportSettings ? reportSettingsDescription : getHideDataLabelsText(hideDataLabels);

    const updateHideDataLabels = (hide: boolean | undefined) => {
        if (props.part.hideDataLabels !== hide) {
            props.savePartChanges(new PartDescriptor({
                ...props.part,
                hideDataLabels: hide
            }));
        }
    }

    return (
        <>
            <label className="category-label">Export options</label>
            <div className="category-properties">
                <div className='row-option'>
                    <span>Data labels</span>
                    <ButtonDropdown isOpen={isDropdownOpen} toggle={toggleButtonDropdown} className="configure-option-dropdown">
                        <DropdownToggle className="toggle-button">
                            <span>{description}</span>
                            <i className="material-symbols-outlined">arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu>
                            <DropdownItem onClick={() => updateHideDataLabels(undefined)}>{reportSettingsDescription}</DropdownItem>
                            <div className="separator"></div>
                            {[false, true].map(hideOption =>
                                <DropdownItem key={hideOption.toString()} onClick={() => updateHideDataLabels(hideOption)}>
                                    {getHideDataLabelsText(hideOption)}
                                </DropdownItem>
                            )}
                        </DropdownMenu>
                    </ButtonDropdown>
                </div>
            </div>
        </>
    );
}

export default ConfigureReportPartExportOptions;