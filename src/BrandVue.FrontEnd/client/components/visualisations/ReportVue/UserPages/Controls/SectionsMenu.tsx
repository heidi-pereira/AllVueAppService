import React from "react";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap";
import { ReportPage } from "../../Visualization/ReportElements/ReportPage";
import { ReportSection } from "../../Visualization/ReportElements/ReportSection";
import { ReportStructure } from "../../Visualization/ReportElements/ReportStructure";

import style from "./Menus.module.less";

interface ISectionsMenu {
    reportStructure: ReportStructure | undefined;
    reportPage: ReportPage;
    onSelectSection: (section: ReportSection) => void;
}

const SectionsMenu = (props: ISectionsMenu) => {

    const [isSectionDropdownOpen, setIsSectionDropdownOpen] = React.useState<boolean>(false);

    const [sections, setSections] = React.useState<ReportSection[]>([]);
    const [section, setSection] = React.useState<ReportSection>();

    React.useEffect(() => {
        const validSections: ReportSection[] = [];
        if (props.reportStructure && props.reportStructure.Sections) {
            props.reportStructure.Sections.forEach(section => {
                let isValidSection = false;
                section.Pages.forEach(page => {
                    if (props.reportStructure?.DoesPageContentExist(page)) {
                        isValidSection = true;
                    }
                    if (page.Id == props.reportPage.Id) {
                        setSection(section);
                        props.onSelectSection(section);
                    }
                });
                if (isValidSection) {
                    validSections.push(section);
                }
            }
            )
        }
        setSections(validSections);
    }, [props.reportPage]);

    const setMySection = (value: ReportSection) => {
        setSection(value);
        props.onSelectSection(value);
    }
    if (sections.length <= 1) {
        return (<></>)
    }
    return (<div className={style.container}>
        <label className={style.label} >{props.reportStructure?.SectionDefinition.Plural()}:</label>
            <ButtonDropdown isOpen={isSectionDropdownOpen} toggle={() => setIsSectionDropdownOpen(!isSectionDropdownOpen)} className={"calculation-type-dropdown"}>
            <DropdownToggle className={style.dropDownToggle + (section ? "" : " " + style.disabled)}>
                <div className={style.dropDownItem} >{section?.Name ?? "No Section available"}</div>
                    {sections.length > 1 &&
                    <div><i className="material-symbols-outlined">arrow_drop_down</i></div>
                    }
                </DropdownToggle>
                {sections.length > 1 &&
                <DropdownMenu >
                        <div className={style.dropdownMenu} >
                        {sections.map((validSection, id) =>
                            <DropdownItem key={id}
                                className={validSection.Name == section?.Name ? style.dropDownItemActive : style.dropDownItem}
                                onClick={() => setMySection(validSection)}
                                active={validSection.Name == section?.Name}>
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

export default SectionsMenu;
