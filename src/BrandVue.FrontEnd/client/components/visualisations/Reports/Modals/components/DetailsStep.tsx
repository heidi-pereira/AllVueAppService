import { useAppSelector } from 'client/state/store';
import { ReportType } from '../../../../../BrandVueApi';
import Tooltip from 'client/components/Tooltip';

export interface IDetailsStepProps {
    numberOfPages: number;
    reportName: string;
    setReportName: (s: string) => void;
    reportType: ReportType;
    setReportType: (t: ReportType) => void;
    shareReport: boolean;
    toggleShareReport: () => void;
    isDefault: boolean;
    setIsDefault: (b: boolean) => void;
    onCancel: () => void;
    onNext: () => void;
    createFromTemplate: boolean;
    setCreateFromTemplate: (b: boolean) => void;
    isCreatingFromDataTab: boolean;
};

const DetailsStep = (props: IDetailsStepProps) => {
    const { templates: existingTemplates } = useAppSelector(state => state.templates);
    const noTemplates = !existingTemplates || existingTemplates.length === 0;
    const detailsPageValid = props.reportName.trim().length > 0;

    const getGroupItemClassName = (type: ReportType) => {
        let className = "type";

        if (props.createFromTemplate) 
        {
            return className += " disabled";
        }

        if (props.reportType === type) {
            return className += " selected";
        }
        return className;
    }

    const getTemplateCheckbox = () => {
        const checkbox = (
            <div className="template">
                <input
                    type="checkbox"
                    className="checkbox"
                    id="from-template-checkbox"
                    disabled={noTemplates}
                    checked={props.createFromTemplate}
                    onChange={() => props.setCreateFromTemplate(!props.createFromTemplate)}
                />
                <label htmlFor="from-template-checkbox">Create from template</label>
            </div>
        );

        if (noTemplates) {
            return (
                <Tooltip placement="top" title="There are no saved templates available">
                    {checkbox}
                </Tooltip>
            )
        }

        return checkbox;
    }

    const getSharingCheckboxesAndType = () => {
        const checkboxesAndReportType =  (
            <div className="checkboxes-and-report-type">
                <div className="report-type">
                    <label className="report-label">Type</label>
                    <div className="report-type-selector">
                        <div className={getGroupItemClassName(ReportType.Chart)} onClick={() => props.setReportType(ReportType.Chart)} tabIndex={0}>
                            <i className="material-symbols-outlined rotate">bar_chart</i>
                            <div className="type-name">Charts</div>
                        </div>
                        <div className={getGroupItemClassName(ReportType.Table)} onClick={() => props.setReportType(ReportType.Table)} tabIndex={0}>
                            <i className="material-symbols-outlined">table_chart</i>
                            <div className="type-name">Tables</div>
                        </div>
                    </div>
                </div>
                <div className="report-properties">
                    <label className="report-label">Sharing</label>
                    <input type="checkbox"
                        className="checkbox"
                        id="share-report-checkbox"
                        checked={props.shareReport && !props.createFromTemplate}
                        onChange={props.toggleShareReport}
                        disabled={props.createFromTemplate}
                    />
                    <label htmlFor="share-report-checkbox">Share with other users</label>
                    <div className="default-report-checkbox">
                        <input
                            type="checkbox"
                            className="checkbox"
                            id="default-report-checkbox"
                            checked={props.isDefault && !props.createFromTemplate}
                            onChange={() => props.setIsDefault(!props.isDefault)}
                            disabled={!props.shareReport || props.createFromTemplate}
                        />
                        <label htmlFor="default-report-checkbox">Set as default</label>
                        <div className="info-text">The default report is loaded automatically for all users</div>
                    </div>
                </div>
            </div>
        )

        if (props.createFromTemplate) {
            return (
                <Tooltip placement="top" title="Options will be defined by the imported template">
                    {checkboxesAndReportType}
                </Tooltip>
            )
        }

        return checkboxesAndReportType;
    }

    return (
        <>
            <div className="details">1 of {props.numberOfPages}: Report details</div>
            <div className="content-and-buttons">
                <div className="content">
                    <div className="flex">
                        <div className="report-name">
                            <label className="report-label">Report name</label>
                            <input
                                type="text"
                                id="report-name"
                                name="report-name-input"
                                className="report-name-input"
                                value={props.reportName}
                                onChange={(e) => props.setReportName(e.target.value)}
                                autoFocus
                                autoComplete="off"
                            />
                        </div>
                        {
                            props.isCreatingFromDataTab && getTemplateCheckbox()
                        }
                    </div>
                    {getSharingCheckboxesAndType()}
                </div>
                <div className="modal-buttons">
                    <button className="modal-button secondary-button" onClick={props.onCancel}>Cancel</button>
                    <button className="modal-button primary-button" onClick={props.onNext} disabled={!detailsPageValid}>Next</button>
                </div>
            </div>
        </>
    )
};

export default DetailsStep;
