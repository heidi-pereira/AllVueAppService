import React from 'react';
import _ from "lodash";
import { Modal} from "reactstrap";
import { ModalBody } from 'react-bootstrap';
import { useSavedReportsContext } from '../SavedReportsContext';
import toast from 'react-hot-toast';
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import ReportSettingsModalDetails from './ReportSettingsModalDetail';
import Throbber from '../../../throbber/Throbber';
import { selectSubsetId } from 'client/state/subsetSlice';
import { useAppSelector } from 'client/state/store';
import { selectCurrentReport } from 'client/state/reportSelectors';
import { ReportWithPage } from '../ReportsPage';

interface ICopyReportModalProps {
    isOpen: boolean;
    setIsOpen(isOpen: boolean): void;
    reportPage: ReportWithPage;
}

const CopyReportModal = (props: ICopyReportModalProps) => {
    const report = props.reportPage.report;
    const page = props.reportPage.page;

    const [reportName, setReportName] = React.useState<string>(page.displayName);
    const [isDefault, setIsDefault] = React.useState<boolean>(false);
    const [shareReport, setShareReport] = React.useState<boolean>(report.isShared);
    const [isLoading, setIsLoading] = React.useState<boolean>(false);
    const { reportsDispatch } = useSavedReportsContext();
    const stateSubsetId = useAppSelector(selectSubsetId);
    const [subsetId, setSubsetId] = React.useState<string>(report.subsetId ?? stateSubsetId);

    const closeModal = () => {
        props.setIsOpen(false);
        setReportName(page.displayName);
        setIsDefault(false);
        setShareReport(report.isShared);
        setIsLoading(false);
    }

    async function copyReport() {
        setIsLoading(true);
        const data = {
            reportId: report.savedReportId,
            page: page,
            newName: reportName,
            isShared: shareReport,
            isDefault: isDefault
        };
        reportsDispatch({type: "COPY_REPORT", data: data})
            .then(() => {
                toast.success(`Created report ${reportName}`);
                closeModal();
            })
            .catch(error => handleError(error))
            .finally(() => setIsLoading(false));
    }

    return (
        <Modal isOpen={props.isOpen} className="copy-report-modal" centered keyboard={false} autoFocus={false}>
            <ModalBody>
                <button onClick={closeModal} className="modal-close-button" title="Close">
                    <i className="material-symbols-outlined">close</i>
                </button>
                <div className="header">
                    Copy as new report
                </div>
                <div className="content-and-buttons">
                    <div className="content">
                        {isLoading &&
                            <div className="throbber-container-fixed">
                                <Throbber />
                            </div>
                        }
                        {!isLoading &&
                            <ReportSettingsModalDetails
                                subsetId={subsetId}
                                reportName={reportName}
                                isDefaultReport={isDefault}
                                shareReport={shareReport}
                                setReportName={setReportName}
                                setIsDefault={setIsDefault}
                                setShareReport={setShareReport}
                                idPrefix="copy-report-modal"
                                onSubsetChange={setSubsetId}
                            />
                        }
                    </div>
                    {!isLoading &&
                        <div className="modal-buttons">
                            <button className="modal-button secondary-button" onClick={closeModal}>Cancel</button>
                            <button className="modal-button primary-button" onClick={copyReport}>Create report</button>
                        </div>
                    }
                </div>
            </ModalBody>
        </Modal>
    )
}

export default CopyReportModal;