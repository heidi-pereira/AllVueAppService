import { CalculationType, DisplaySignificanceDifferences, FeatureCode, MainQuestionType, Report, ReportType, Significance } from "../../../../BrandVueApi";
import { getColourMapReverse } from "../../../../components/helpers/ChromaHelper";
import { Metric } from "../../../../metrics/metric";
import { isFeatureEnabled } from "../../../helpers/FeaturesHelper";
import { PartType } from "../../../panes/PartType";
import { PartWithExtraData } from "../ReportsPageDisplay";
import { getPartTypeForMetric } from "../Utility/ReportPageBuilder";

export const getReverseLegendMap = (colours: string[], orderedEntityNames: string[]) => {
    let legendMap: Map<string, string> = new Map();
    let storeColours = false;

    const colourLookup = new Map(
        colours.map(c => {
            const [entity, colour] = c.split(':');
            return [entity, colour];
        })
    );

    if(orderedEntityNames.every(e => colourLookup.has(e))) {
        orderedEntityNames.forEach(name => {
            const colour = colourLookup.get(name);
            legendMap.set(name, colour!);
        });
    } else {
        legendMap = getColourMapReverse(orderedEntityNames.map(n => n));
        storeColours = true;
    }
    return ({legendMap, storeColours});
}

export const isUsingBreaks = (reportPart: PartWithExtraData, report: Report) => {
    const part = reportPart.part;
    const partBreaks = part.breaks;
    return (part.overrideReportBreaks && partBreaks.length > 0) ||
            (!part.overrideReportBreaks && report.breaks.length > 0);
}

export const canSelectFilterInstances = (reportPart: PartWithExtraData, report: Report) => {

    if(!reportPart.metric) return false;

    const isMultiEntity = reportPart.metric!.entityCombination.length > 1;
    const isMultiEntityTextQuestion = reportPart.metric!.calcType === CalculationType.Text &&
        reportPart.metric!.entityCombination.length > 0;

    return isMultiEntityTextQuestion ||
        report.reportType === ReportType.Chart &&
        (
            reportPart.part.partType === PartType.ReportsCardMultiEntityMultipleChoice ||
            (isMultiEntity && reportPart.part.partType === PartType.ReportsCardLine) ||
            (isMultiEntity && reportPart.part.partType === PartType.ReportsCardDoughnut) ||
            (reportPart.part.partType === PartType.ReportsCardStackedMulti && isUsingBreaks(reportPart, report))
        );
}

export enum ChartType {
    Bar = "Bar/column chart",
    Stacked = "Stacked column chart",
    Line = "Line chart",
    Doughnut = "Doughnut chart",
    Heatmap = "Heat map",
    Funnel = "Funnel",
    Unknown = "Unknown chart"
}

export const getCurrentChartType = (partType: string, isUsingWaves: boolean, isUsingOvertime: boolean, metric: Metric, questionTypeLookup: { [key: string]: MainQuestionType }): ChartType => {
    switch (partType) {
        case PartType.ReportsCardHeatmapImage:
            return ChartType.Heatmap;
        case PartType.ReportsCardLine:
            return isUsingWaves || isUsingOvertime ? ChartType.Line :
                getCurrentChartType(getPartTypeForMetric(metric!, questionTypeLookup, ReportType.Chart, false), isUsingWaves, isUsingOvertime, metric, questionTypeLookup);
        case PartType.ReportsCardChart:
        case PartType.ReportsCardMultiEntityMultipleChoice:
            return ChartType.Bar;
        case PartType.ReportsCardStackedMulti:
            return ChartType.Stacked;
        case PartType.ReportsCardDoughnut:
            return ChartType.Doughnut;
        case PartType.ReportsCardFunnel:
            return ChartType.Funnel;
        default:
            return ChartType.Unknown;
    }
}

export const getShouldShowSignificance = (dataPointSignificance: Significance | undefined, displaySignificanceDifferences: DisplaySignificanceDifferences) => {
    if(!dataPointSignificance || dataPointSignificance == Significance.None) {
        return Significance.None;
    }

    if(dataPointSignificance == Significance.Up 
        && (displaySignificanceDifferences == DisplaySignificanceDifferences.ShowUp || displaySignificanceDifferences == DisplaySignificanceDifferences.ShowBoth)) {
        return Significance.Up;
    }

    if(dataPointSignificance == Significance.Down 
        && (displaySignificanceDifferences == DisplaySignificanceDifferences.ShowDown || displaySignificanceDifferences == DisplaySignificanceDifferences.ShowBoth)) {
        return Significance.Down;
    }

    return Significance.None;
}

export const isUsingOverTime = (report: Report, reportPart: PartWithExtraData) => {
    const isOverTimeFeatureEnabled = isFeatureEnabled(FeatureCode.Overtime_data);
    const isUsingReportOverTimeSetting = reportPart.part.showOvertimeData == undefined;
    return isOverTimeFeatureEnabled && (
        isUsingReportOverTimeSetting ? report.overTimeConfig != undefined : reportPart.part.showOvertimeData === true
    );
}

export const isUsingWaves = (report: Report, reportPart: PartWithExtraData) => {
    const isUsingReportPartWaves = reportPart.part.waves != null;
    const reportPartWaves = reportPart.part.waves?.waves;
    // We need to enforce the priority of checking reportPartWaves over report.waves
    return isUsingReportPartWaves ? reportPartWaves != undefined : report.waves != null
};