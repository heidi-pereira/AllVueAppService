import React from 'react';
import style from './ChooseTemplateStep.module.less';
import { ReportTemplate, ReportType } from 'client/BrandVueApi';
import FuzzyDate from 'client/components/helpers/FuzzyDate';
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from 'reactstrap';

interface ITemplateListItemProps {
    template: ReportTemplate;
    selected: boolean;
    onSelect: () => void;
    setDeleteConfirmationModalVisible: () => void;
}

const TemplateListItem = (props: ITemplateListItemProps) => {
    const [isHamburgerOpen, setIsHamburgerOpen] = React.useState(false);

    return (
        <li className={`${props.selected ? style.itemSelected : style.item} ${style.itemWrapper}`} onClick={props.onSelect}>
            <div className={style.itemTopRow}>
                <div className={style.left}>
                    <div className={style.itemIcon}>
                        {props.template.savedReportTemplate.reportType === ReportType.Chart ?
                            <i className="material-symbols-outlined rotate">bar_chart</i>
                            : <i className="material-symbols-outlined">table_chart</i>
                        }
                    </div>
                    <div className={style.itemMain}>
                        <div className={style.itemName}>{props.template.templateDisplayName}</div>
                        <div className={style.itemDesc}>{props.template.templateDescription}</div>
                    </div>
                </div>
                <div className={style.right}>
                    <ButtonDropdown
                        isOpen={isHamburgerOpen}
                        toggle={e => setIsHamburgerOpen(!isHamburgerOpen)}
                        className={style.hamburger}
                    >
                        <DropdownToggle className={'btn-menu styled-toggle'}>
                            <i className={`${style.toggle} material-symbols-outlined`}>more_vert</i>
                        </DropdownToggle>
                        <DropdownMenu end>
                            <DropdownItem className="dropdown-item" onClick={props.setDeleteConfirmationModalVisible}>
                                <i className="material-symbols-outlined">delete</i>Delete Template
                            </DropdownItem>
                        </DropdownMenu>
                    </ButtonDropdown>
                </div>
            </div>
            <div className={style.itemMetaRow}>
                <div className={style.itemMetaLeft}>
                    {props.template.reportTemplateParts ? props.template.reportTemplateParts.length : 0} item{props.template.reportTemplateParts && props.template.reportTemplateParts.length === 1 ? '' : 's'}
                </div>
                <div className={style.itemMetaRight}>
                    <i className={`${style.calendar} material-symbols-outlined`}>calendar_month</i>
                    <FuzzyDate date={props.template.createdAt} lowerCase includePastFuture />
                </div>
            </div>
        </li>
    );
};

export default TemplateListItem;
