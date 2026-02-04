import { useSavedReportsContext } from '../SavedReportsContext';
import toast from 'react-hot-toast';
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import DeleteModal from '../../../DeleteModal';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { PageHandler } from '../../../PageHandler';
import {useNavigate} from "react-router-dom";
import { useAppSelector } from 'client/state/store';
import { selectCurrentReport } from 'client/state/reportSelectors';
import { ReportWithPage } from '../ReportsPage';

interface IDeleteReportModalProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    isOpen: boolean;
    reportsPageUrl: string;
    closeModal(isDeleted: boolean): void;
    reportPage: ReportWithPage;
}

const DeleteReportModal = (props: IDeleteReportModalProps) => {
    const { reportsDispatch } = useSavedReportsContext();
    const report = props.reportPage.report;
    const page = props.reportPage.page;

    const navigate = useNavigate();
    const deleteReport = () => {
        props.googleTagManager.addEvent("reportsPageDelete", props.pageHandler);
        reportsDispatch({type: "DELETE_REPORT", data: {reportId: report.savedReportId}})
            .then(() => {
                toast.success("Report deleted");
                props.closeModal(true);
                navigate(props.reportsPageUrl);
            })
            .catch(error => {
                handleError(error);
            })
    }

    const isSharedReport = report.isShared == true;

    return (
        <DeleteModal
            isOpen={props.isOpen}
            thingToBeDeletedName={page.displayName}
            thingToBeDeletedType='report'
            delete={deleteReport}
            closeModal={props.closeModal}
            affectAllUsers={isSharedReport}
            delayClick={isSharedReport}
        />
    );
}

export default DeleteReportModal;