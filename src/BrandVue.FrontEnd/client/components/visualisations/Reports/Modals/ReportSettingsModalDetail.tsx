import SubsetSelector from '../../Crosstab/SubsetSelector';

interface IReportSettingsModalDetailsProps {
    setReportName(name: string): void;
    setIsDefault(name: boolean): void;
    setShareReport(name: boolean): void;
    reportName: string;
    shareReport: boolean;
    isDefaultReport: boolean;
    idPrefix: string;
    subsetId: string;
    onSubsetChange(subsetId: string): void;
}

const ReportSettingsModalDetails = (props: IReportSettingsModalDetailsProps) => {

    const toggleShareReport = () => {
        if (props.shareReport) {
            props.setIsDefault(false);
        }
        props.setShareReport(!props.shareReport);
    }

    return (
        <>
            <div className="report-name">
                <label className="report-label">Name</label>
                <input type="text"
                    id={`${props.idPrefix}report-name`}
                    name="report-name-input"
                    className="report-name-input"
                    value={props.reportName}
                    onChange={(e) => props.setReportName(e.target.value)}
                    autoFocus
                    autoComplete="off" />
            </div>
            <div>
                <label className="report-label">Sharing</label>
                <div className="report-properties">
                    <input type="checkbox" className="checkbox" id={`${props.idPrefix}share-report-checkbox`} checked={props.shareReport} onChange={toggleShareReport} />
                    <label htmlFor={`${props.idPrefix}share-report-checkbox`}>Share with other users</label>
                    <div className="default-report-checkbox">
                        <input type="checkbox" className="checkbox" id={`${props.idPrefix}default-report-checkbox`} checked={props.isDefaultReport} onChange={() => props.setIsDefault(!props.isDefaultReport)} disabled={!props.shareReport} />
                        <label htmlFor={`${props.idPrefix}default-report-checkbox`}>Set this report as default</label>
                        <div className="info-text">The default report is loaded automatically for all users</div>
                    </div>
                </div>
            </div>
            <SubsetSelector
                subsetId={props.subsetId} 
                updateUrlOnChange={false}
                onSubsetChange={props.onSubsetChange}
            />
        </>
    )
}

export default ReportSettingsModalDetails;