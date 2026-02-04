import React from "react";
import { UserContext } from "../../../GlobalContext";
import { Metric } from "../../../metrics/metric";
import { AverageType, CuratedResultsModel, CrosstabRequestModel, CrosstabExportRequest, PermissionFeaturesOptions } from "../../../BrandVueApi";
import { ExportType, useAsyncExportContext } from "../Reports/Utility/AsyncExportContext";
import LargeExportPopover from "../Reports/Components/LargeExportPopover";
import ReportExport from "../Reports/Utility/ReportExport";
import { getVerifiedAverageType } from "../AverageHelper";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";
import { selectHydratedVariableConfiguration } from '../../../state/variableConfigurationSelectors';
import { useAppSelector } from "../../../state/store";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap";
import { hasAllVuePermissionsOrSystemAdmin } from "../../helpers/FeaturesHelper";
import { useCrosstabPageStateContext } from "./CrosstabPageStateContext";

interface ICrosstabExcelDownloadProps {
    selectedMetric: Metric | undefined;
    getRequestModel(): CrosstabRequestModel | CuratedResultsModel | undefined;
    numTables: number;
    canDownload: boolean;
    averages: AverageType[];
    displayMeanValues: boolean;
    setCreateNewReportModalVisibility(isVisible: boolean, preSelectedMetric?: Metric): void;
    showAddToReportModal(): void;
}

const CrosstabExcelDownload = (props: ICrosstabExcelDownloadProps) => {
    const { pendingExports, exportDispatch } = useAsyncExportContext();
    const isLoading = pendingExports.some(e => e.exportType === ExportType.Crosstab && e.metricName === props.selectedMetric?.name);
    const [exportPopoverOpen, setExportPopoverOpen] = React.useState<boolean>(false);
    const { selectableMetricsForUser: metrics, questionTypeLookup } = useMetricStateContext();
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const [isOpen, setIsOpen] = React.useState(false);
    const canEditReports = hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.ReportsAddEdit]);
    const { crosstabPageState } = useCrosstabPageStateContext();
    const exportSingle = () => {
        if (props.numTables >= ReportExport.LARGE_EXPORT_SIZE) {
            setExportPopoverOpen(true);
        } else {
            exportExcel();
        }
    }

    const exportExcel = () => {
        setExportPopoverOpen(false);
        if (props.selectedMetric) {
            const confirmedAverages = props.averages.map(a => {
                return getVerifiedAverageType(a, props.selectedMetric!, questionTypeLookup, productConfiguration.isSurveyVue(), metrics, variables)
            });

            const requestModel = props.getRequestModel();
            if (requestModel instanceof CrosstabRequestModel) {
                requestModel.pageNo = undefined;
                requestModel.noOfCharts = undefined;
                const exportRequest = new CrosstabExportRequest({
                    requestModel: requestModel,
                    resultSortingOrder: crosstabPageState.resultSortingOrder,
                    includeCounts: crosstabPageState.includeCounts,
                    highlightLowSample: crosstabPageState.highlightLowSample,
                    decimalPlaces: crosstabPageState.decimalPlaces,
                    hideEmptyRows: false,
                    hideEmptyColumns: false,
                    hideTotalColumn: crosstabPageState.hideTotalColumn,
                    showMultipleTablesAsSingle: crosstabPageState.showMultipleTablesAsSingle,
                    averages: confirmedAverages,
                    displayMeanValues: (confirmedAverages.includes(AverageType.EntityIdMean) || confirmedAverages.includes(AverageType.Median)) && props.displayMeanValues,
                    calculateIndexScores: crosstabPageState.calculateIndexScores,
                    lowSampleThreshold: crosstabPageState.lowSampleThreshold,
                    displayStandardDeviation: crosstabPageState.displayStandardDeviation
                });
                exportDispatch({type: 'EXPORT_CROSSTAB_MULTI_ENTITY', data: {request: exportRequest, label: props.selectedMetric.displayName}});
            } else if (requestModel instanceof CuratedResultsModel) {
                exportDispatch({type: 'EXPORT_CROSSTAB_TEXT', data: { request: requestModel, label: props.selectedMetric.displayName}});
            } else {
                throw new Error("No export request model");
            }
        }
    }

    const exportButtonId = 'excelDownloadButton';
    const user = React.useContext(UserContext);
    const isExportForbidden = user?.isTrialUser;
    return (
        <>
            <div className="add-filter-button">
                <div className="metric-dropdown-menu">
                    <ButtonDropdown isOpen={isOpen} toggle={() => setIsOpen(!isOpen)} className="metric-dropdown" id={exportButtonId}>
                        <DropdownToggle caret className={`hollow-button excelDownload ${isLoading ? "loading" : ""}`} tag="button" disabled={isLoading || isExportForbidden || !props.canDownload}>
                            <i className="material-symbols-outlined">file_download</i>
                            <div>Save data</div>
                        </DropdownToggle>
                        <DropdownMenu>
                            <DropdownItem
                                disabled={isLoading || isExportForbidden || !props.canDownload} 
                                onClick={exportSingle}>
                                    Download data
                            </DropdownItem>
                            {canEditReports && productConfiguration.isSurveyVue() &&
                                <>
                                    <DropdownItem onClick={() => {props.showAddToReportModal()}}>Add to existing report</DropdownItem>
                                    <DropdownItem onClick={() => {props.setCreateNewReportModalVisibility(true, props.selectedMetric)}}>Save to new report</DropdownItem>
                                </>
                            }
                        </DropdownMenu>
                    </ButtonDropdown>
                </div>
            </div>
            <LargeExportPopover
                isOpen={exportPopoverOpen}
                attachedElementId={exportButtonId}
                close={() => setExportPopoverOpen(false)}
                doExport={exportExcel}
            />
        </>
    );
}
export default CrosstabExcelDownload;


