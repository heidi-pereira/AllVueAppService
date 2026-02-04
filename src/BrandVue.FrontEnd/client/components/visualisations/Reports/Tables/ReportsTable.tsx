import {useRef} from "react";
import {
    CalculationType,
    CrossMeasure,
    Report,
    MainQuestionType,
    ReportOrder,
} from "../../../../BrandVueApi";
import {CuratedFilters} from "../../../../filter/CuratedFilters";
import { IGoogleTagManager } from "../../../../googleTagManager";
import {PartWithExtraData} from "../ReportsPageDisplay";
import CrosstabsContainer from "../../Crosstab/CrosstabsContainer";
import {
    getReportPartBaseExpressionOverride,
    getReportPartDisplayText,
    getSplitByAndFilterByEntityTypesForPart,
    hasSingleEntityInstance
} from "../../../helpers/SurveyVueUtils";
import ReportsTableTextPage from './ReportsTableTextPage';
import TablePaginationControls from "../../TablePaginationControls";
import {PaginationData} from "../../PaginationData";
import {hasNoAnswersSelected, NothingSelected} from "../../Cards/NothngSelectedCard";
import { PageHandler } from "../../../PageHandler";
import { useEntityConfigurationStateContext } from "../../../../entity/EntityConfigurationStateContext";
import { SortAverages } from "../../AverageHelper";
import { useMetricStateContext } from "../../../../metrics/MetricStateContext";
import BrandVueOnlyLowSampleHelper from "../../BrandVueOnlyLowSampleHelper";
import { useAppSelector } from "client/state/store";
import { selectCurrentReport } from "client/state/reportSelectors";

interface IProps {
    curatedFilters: CuratedFilters;
    canEditReport: boolean;
    selectedPart: PartWithExtraData | undefined;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    isTableConfigurationVisible: boolean;
    categories: CrossMeasure[];
    setIsTableConfigurationVisible(isVisible: boolean): void;
    questionTypeLookup: {[key: string]: MainQuestionType};
    setIsLowSample(isLowSample: boolean): void;
    isDataWeighted: boolean;
    setPagination: (pageNo: number, noOfTablesPerPage: number, totalNoOfTables: number) => void;
    maxNoOfTablesPerPage: number;
    paginationData: PaginationData
}

const ReportsTable = (props: IProps) => {
    const entityConfiguration = useEntityConfigurationStateContext();
    const tableRef = useRef<HTMLTableElement>(null);
    const { metricsForReports } = useMetricStateContext();
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    if (!props.selectedPart?.metric) {
        return (
            <div className="reports-table">
                <div className="table-error">
                    <i className="material-symbols-outlined no-symbol-fill">info</i>
                    <div>There was an error loading results</div>
                </div>
            </div>
        )
    }

    const getReportOrder = (): ReportOrder => {
        if (props.selectedPart && props.selectedPart.part.reportOrder) {
            return props.selectedPart.part.reportOrder;
        }
        return report.reportOrder;
    }

    const baseExpressionOverride = getReportPartBaseExpressionOverride(props.selectedPart, report.baseTypeOverride, report.baseVariableId);

    const getTableContent = () => {
        if (props.selectedPart && props.selectedPart.metric &&
            (props.selectedPart.selectedEntitySet || props.selectedPart.metric.entityCombination.length == 0) &&
            props.selectedPart.metric.calcType != CalculationType.Text)
        {
            const selectedInstances = props.selectedPart.selectedEntitySet?.getInstances().getAll().map(i => i.id);
            const enabledAverages = hasSingleEntityInstance(props.selectedPart.metric, selectedInstances) ?
                [] : [...props.selectedPart.part.averageTypes].sort((a,b) => SortAverages(a,b));
            return <CrosstabsContainer metric={props.selectedPart.metric}
                        activeEntitySet={props.selectedPart.selectedEntitySet}
                        secondaryEntitySets={[]}
                        curatedFilters={props.curatedFilters}
                        categories={props.categories}
                        includeCounts={report.includeCounts}
                        highlightLowSample={report.highlightLowSample}
                        highlightSignificance={report.highlightSignificance}
                        displaySignificanceDifferences={report.displaySignificanceDifferences}
                        significanceType={report.significanceType}
                        resultSortingOrder={getReportOrder()}
                        decimalPlaces={report.decimalPlaces}
                        setCanDownload={() => {}}
                        hideEmptyRows={report.hideEmptyRows}
                        hideEmptyColumns={report.hideEmptyColumns}
                        showTop={props.selectedPart.part.showTop}
                        baseExpressionOverride={baseExpressionOverride}
                        isUserAdmin={props.canEditReport}
                        setIsLowSample={(isLowSample) => {props.setIsLowSample(isLowSample)}}
                        allMetrics={metricsForReports}
                        isSurveyVue={true}
                        setCanIncludeCounts={() => {}}//bv only
                        isDataWeighted={props.isDataWeighted}
                        currentPaginationData={props.paginationData}
                        averageTypes={enabledAverages}
                        displayMeanValues={props.selectedPart.part.displayMeanValues}
                        splitBy={undefined}
                        sigConfidenceLevel={report.sigConfidenceLevel}
                        hideTotalColumn={report.hideTotalColumn}
                        showMultipleTablesAsSingle={report.showMultipleTablesAsSingle}
                        calculateIndexScores={report.calculateIndexScores}
                        lowSampleThreshold={report.lowSampleThreshold ?? BrandVueOnlyLowSampleHelper.lowSampleForEntity}
                        displayStandardDeviation={props.selectedPart.part.displayStandardDeviation}
            />
        }
    }

    const multipleEntitySplitByAndFilterBy = props.selectedPart?.metric &&
        getSplitByAndFilterByEntityTypesForPart(props.selectedPart.part, props.selectedPart.metric, entityConfiguration.entityConfiguration);
    const helptext = props.selectedPart.metric.isAutoGeneratedNumeric() ?
        "Auto grouped: " + getReportPartDisplayText(props.selectedPart) :
        getReportPartDisplayText(props.selectedPart);
    return (
        <div className="reports-table">
            <div className="reports-header">
                <div className="question-text bg-transparent">{helptext}</div>
                {props.canEditReport &&
                    <button className={"hollow-button toggle-configure-button" + (props.isTableConfigurationVisible ? " hidden" : "")} onClick={() => props.setIsTableConfigurationVisible(!props.isTableConfigurationVisible)}>
                        <i className="material-symbols-outlined">mode_edit</i>
                        <div className="new-variable-button-text">Configure</div>
                    </button>
                }
            </div>
            <div className={`masterTable ${props.selectedPart.metric?.entityCombination.length <= 1 ? ' single fit-to-page' : ' multi'}`} ref={tableRef}>
                {props.selectedPart.metric && multipleEntitySplitByAndFilterBy && props.selectedPart.metric.calcType == CalculationType.Text &&
                    <ReportsTableTextPage
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        metric={props.selectedPart.metric}
                        curatedFilters={props.curatedFilters}
                        selectedPart={props.selectedPart}
                        multipleEntitySplitByAndFilterBy={multipleEntitySplitByAndFilterBy}
                        baseExpressionOverride={baseExpressionOverride}
                        setIsLowSample={props.setIsLowSample}
                    />
                }
                {hasNoAnswersSelected(props.selectedPart) ? <NothingSelected/> : getTableContent()}
            </div>
            <div>
                <TablePaginationControls
                    currentPaginationData={props.paginationData}
                    setPagination={props.setPagination}
                    maxNoOfTablesPerPage={props.maxNoOfTablesPerPage}
                />
            </div>
        </div>
    );
}

export default ReportsTable;