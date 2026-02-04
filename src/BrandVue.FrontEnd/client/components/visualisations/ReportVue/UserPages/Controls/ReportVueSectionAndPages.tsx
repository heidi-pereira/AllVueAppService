import React from "react";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap";
import { ReportPage } from "../../Visualization/ReportElements/ReportPage";
import { ReportSection } from "../../Visualization/ReportElements/ReportSection";
import { ReportStructure } from "../../Visualization/ReportElements/ReportStructure";
import style from "./ReportVueUserFilter.module.less";

interface IReportVueSectionAndPages {
    reportStructure: ReportStructure;
    reportPage: ReportPage;
    setReportPage: (reportPage: ReportPage) => void;
}


const ReportVueSectionAndPages = (props: IReportVueSectionAndPages) => {

    const [isDropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const [sections, setSections] = React.useState<ReportSection[]>([]);
    const [selectedSection, setSelectedSection] = React.useState<string>("");

    React.useEffect(() => {
        const validSections: ReportSection[] = props.reportStructure ? props.reportStructure.GetValidSections() : [];
        setSections(validSections);
    }, [props.reportStructure]);

    return (
        <div className={style.filterContainer}>
            <label className={style.label} >Section:</label>
            <ButtonDropdown isOpen={isDropdownOpen} toggle={() => setDropdownOpen(!isDropdownOpen)} className={"calculation-type-dropdown"}>
                <DropdownToggle className={"toggle-button"}>
                    <div>{selectedSection && selectedSection.length > 0 ? selectedSection : "-"}</div>
                    {sections.length > 1 &&
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    }
                </DropdownToggle>
                {sections.length > 1 &&
                    <DropdownMenu>
                        <div className={style.dropdownMenu} >
                            {sections.map((validSection, id) =>
                                <DropdownItem key={id} onClick={() => setSelectedSection(validSection.Name)} active={validSection.Name == selectedSection}>
                                    {validSection.Name}
                                </DropdownItem>
                            )}
                        </div>
                    </DropdownMenu>
                }
            </ButtonDropdown>
        </div>
    );
}

export default ReportVueSectionAndPages;
