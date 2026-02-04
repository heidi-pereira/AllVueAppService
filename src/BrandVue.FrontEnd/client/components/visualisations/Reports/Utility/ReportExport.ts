import toast from "react-hot-toast";
import { ComparisonPeriodSelection, Period, ReportExportRequest, ReportPartExportRequest, ReportType } from "../../../../BrandVueApi";
import { IEntityConfiguration } from "../../../../entity/EntityConfiguration";
import { CuratedFilters } from "../../../../filter/CuratedFilters";
import { Metric } from "../../../../metrics/metric";
import { getSplitByAndFilterByEntityTypesForPart } from "../../../helpers/SurveyVueUtils";
import { PartType } from "../../../panes/PartType";
import { getCompositeFilterModel, getDemographicFilterModel } from "../Filtering/FilterHelper";
import { ReportWithPage } from "../ReportsPage";
import { ITimeSelectionOptions } from "../../../../state/ITimeSelectionOptions";

const LARGE_EXPORT_SIZE = 500;

function getExportReportPowerpointRequest(
    reportPage: ReportWithPage,
    curatedFilters: CuratedFilters,
    overTimeFilters: CuratedFilters,
    useGenerativeAi: boolean,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
): ReportExportRequest | undefined {
    const parts = reportPage.page.panes[0].parts;
    if (parts.length === 0 || parts.every((part) => part.partType === PartType.ReportsCardText)) {
        toast.error("No exportable charts in report");
        return undefined;
    }

    return getReportExportRequest(reportPage.report.savedReportId, curatedFilters, overTimeFilters, useGenerativeAi, subsetId, timeSelection);
}

function getExportReportPartPowerpointRequest(
    reportId: number,
    partId: number,
    curatedFilters: CuratedFilters,
    overTimeFilters: CuratedFilters,
    useGenerativeAi: boolean,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
): ReportPartExportRequest {
    return new ReportPartExportRequest({
        ...getReportExportRequest(reportId, curatedFilters, overTimeFilters, useGenerativeAi, subsetId, timeSelection),
        partId: partId,
    });
}

function getExportReportTablesRequest(
    reportPage: ReportWithPage,
    curatedFilters: CuratedFilters,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
): ReportExportRequest | undefined {
    const parts = reportPage.page.panes[0].parts;
    if (parts.length === 0) {
        toast.error("No exportable tables in report");
        return undefined;
    }

    //tables do not support overtime yet
    const overTimeFilters = curatedFilters;
    return getReportExportRequest(reportPage.report.savedReportId, curatedFilters, overTimeFilters, false, subsetId, timeSelection);
}

function getReportExportRequest(
    reportId: number,
    curatedFilters: CuratedFilters,
    overTimeFilters: CuratedFilters,
    useGenerativeAi: boolean,
    subsetId: string,
    timeSelection: ITimeSelectionOptions
): ReportExportRequest {
    return new ReportExportRequest({
        subsetId: subsetId,
        period: new Period({
            average: curatedFilters.average.averageId,
            comparisonDates: curatedFilters.comparisonDates(false, timeSelection, false, ComparisonPeriodSelection.CurrentPeriodOnly),
        }),
        overTimePeriod: new Period({
            average: overTimeFilters.average.averageId,
            comparisonDates: overTimeFilters.comparisonDates(false, timeSelection, true, ComparisonPeriodSelection.CurrentPeriodOnly),
        }),
        demographicFilter: getDemographicFilterModel(curatedFilters),
        filterModel: getCompositeFilterModel(curatedFilters),
        savedReportId: reportId,
        useGenerativeAi: useGenerativeAi,
    });
}

function isLargeExport(reportPage: ReportWithPage, validMetrics: Metric[], entityConfiguration: IEntityConfiguration): boolean {
    const parts = reportPage.page.panes[0].parts;
    if (reportPage.report.reportType === ReportType.Chart) {
        return parts.filter((p) => p.partType !== PartType.ReportsCardText).length >= LARGE_EXPORT_SIZE;
    } else {
        const multiEntityParts = parts
            .map((p) => {
                const metric = validMetrics.find((m) => m.name === p.spec1);
                return {
                    part: p,
                    metric: metric,
                };
            })
            .filter((p) => p.metric && p.metric.entityCombination.length > 1);
        const numOtherParts = parts.length - multiEntityParts.length;
        const tableCounts = multiEntityParts.map((p) => {
            const entities = getSplitByAndFilterByEntityTypesForPart(p.part, p.metric, entityConfiguration);
            if (!entities) {
                return 1;
            }
            const instanceCounts = entities.filterByEntityTypes.map((type) => entityConfiguration.getAllEnabledInstancesForType(type).length);
            return instanceCounts.reduce((a, b) => a * b, 1);
        });
        return numOtherParts + tableCounts.reduce((a, b) => a + b, 0) >= LARGE_EXPORT_SIZE;
    }
}

export default { getExportReportPowerpointRequest, getExportReportPartPowerpointRequest, getExportReportTablesRequest, isLargeExport, LARGE_EXPORT_SIZE };
