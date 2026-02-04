import React from 'react';
import { Modal } from "reactstrap";
import { ModalBody } from 'react-bootstrap';
import { Metric } from '../../../../metrics/metric';
import toast from "react-hot-toast";
import { useSavedReportsContext } from '../SavedReportsContext';
import { useAppSelector } from '../../../../state/store';
import { selectSubsetId } from '../../../../state/subsetSlice';
import * as BrandVueApi from "../../../../BrandVueApi";
import { useCrosstabPageStateContext } from '../../Crosstab/CrosstabPageStateContext';
import AvailableReport from './AvailableReport';
import { getPartFromMetric } from '../Utility/ReportPageBuilder';
import { getReportPagesFrom } from '../Utils/ReportHelpers';
import { ReportWithPage } from '../ReportsPage';
import { getReportsPage } from '../../../helpers/PagesHelper';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import style from './AddToReportModal.module.less';
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import Throbber from 'client/components/throbber/Throbber';
import { selectAllReports } from 'client/state/reportSlice';

interface IAddToReportModal {
    isOpen: boolean;
    setIsOpen(isVisible: boolean): void;
    preSelectedMetric: Metric;
}

const AddToReportModal = (props: IAddToReportModal) => {
    const { reportsDispatch } = useSavedReportsContext();
    const [isUpdatingReport, setIsUpdatingReport] = React.useState<boolean>(false);
    const rootReportsPage = getReportsPage();
    const subsetId = useAppSelector(selectSubsetId);
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { questionTypeLookup } = useMetricStateContext();
    const { crosstabPageState } = useCrosstabPageStateContext();
    const allReports = useAppSelector(selectAllReports);

    const getReportPages = (reports: BrandVueApi.Report[], rootReportsPage: BrandVueApi.PageDescriptor | undefined): ReportWithPage[] => {
        const reportsForSubset = reports.filter(r => r.subsetId === subsetId || !r.subsetId);
        return getReportPagesFrom(reportsForSubset, rootReportsPage)
    }

    const [allReportsWithPage, setAllReportsWithPage] = React.useState<ReportWithPage[]>(getReportPages(allReports, rootReportsPage));
    const [selectedReportWithPage, setSelectedReportWithPage] = React.useState<ReportWithPage | undefined>(allReportsWithPage[0] ?? undefined);

    React.useEffect(() => {
        if (rootReportsPage) {
            const reportPages = getReportPages(allReports, rootReportsPage);
            setAllReportsWithPage(reportPages);
            setSelectedReportWithPage(reportPages[0]);
        }
    }, [allReports, rootReportsPage]);

    const definePartFromMetricAndCrosstabState = (() => {
        if(!selectedReportWithPage || !props.preSelectedMetric) {
            throw new Error("No report or metric selected");
        }

        const reportLength = selectedReportWithPage.page.panes[0].parts.length;

        let part = getPartFromMetric(props.preSelectedMetric,
            selectedReportWithPage.page?.panes[0].id,
            reportLength + 1,
            entityConfiguration,
            questionTypeLookup,
            selectedReportWithPage.report.reportType,
            selectedReportWithPage.report.waves != undefined);

        part.breaks = crosstabPageState.categories;
        part.overrideReportBreaks = true;
        part.averageTypes = crosstabPageState.selectedAverages;
        part.reportOrder = crosstabPageState.resultSortingOrder;
        return part;
    })
    
    async function updateReport() {
        if(!selectedReportWithPage) {
            toast.error("No report selected");
            return;
        }

        setIsUpdatingReport(true);
        const tabDefinedPart = definePartFromMetricAndCrosstabState();

        reportsDispatch({ type: "ADD_PARTS", data: { report: selectedReportWithPage.report, parts: [tabDefinedPart] } })
            .then(() => {
                toast.success(`Updated report`);
                props.setIsOpen(false);
            })
            .catch(error => handleError(error))
            .finally(() => setIsUpdatingReport(false));
    }
    
    return (
        <Modal isOpen={props.isOpen} centered={true} className="report-modal" keyboard={false} autoFocus={false}>
            <ModalBody>
                <div className="top-buttons">
                    <button onClick={() => props.setIsOpen(false)} className="modal-close-button">
                        <i className="material-symbols-outlined">close</i>
                    </button>
                </div>
                <div className="header">
                    Add {props.preSelectedMetric.name} to report
                </div>
                <div className={style.contentAndButtons}>
                    {isUpdatingReport ?
                        <div className={style.throbber}>
                            <Throbber />
                        </div>
                        :
                        <div>
                            <div className={style.availableReportsList}>
                                {allReportsWithPage.map( r => (
                                    <AvailableReport key={r.report.savedReportId}
                                        report={r.report}
                                        page={r.page}
                                        selected={selectedReportWithPage?.report.savedReportId === r.report.savedReportId}
                                        onSelect={() => setSelectedReportWithPage(r)} /> 
                                ))}
                            </div>
                            <div className="button-container">
                                <button onClick={() => props.setIsOpen(false)} className="secondary-button">Cancel</button>
                                <button onClick={updateReport} className="primary-button" data-testid="primary-button">
                                    Add to report
                                </button>
                            </div>
                        </div>
                    }
                </div>
            </ModalBody>
        </Modal>
    );
};

export default AddToReportModal;