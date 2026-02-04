import _ from "lodash";
import {
    BaseDefinitionType,
    BaseExpressionDefinition,
    CalculationType,
    CrossMeasureFilterInstance,
    ICustomPeriod,
    MultipleEntitySplitByAndFilterBy,
    PartDescriptor,
    ReportOrder,
    IEntityType,
    WeightingMethod,
    VariableConfigurationModel,
    AverageType,
    ReportType,
    MainQuestionType,
    EntityTypeAndInstance,
    ReportOverTimeConfiguration,
    TotalisationPeriodUnit,
    WeightedDailyResult,
    MakeUpTo,
    CustomDateRange,
    PeriodType,
    IAverageDescriptor,
    SwaggerException,
} from "../../BrandVueApi";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { PartWithExtraData } from "../visualisations/Reports/ReportsPageDisplay";
import { isCustomPeriodAverage } from "./PeriodHelper";
import { MetricFilterState } from "../../filter/metricFilterState";
import { applyMetricFiltersToCuratedFilters } from "../visualisations/Reports/Filtering/FilterHelper";
import { EntitySet } from "../../entity/EntitySet";
import { FilterInstance } from "../../entity/FilterInstance";
import { ActionEventName } from "../../googleTagManager";
import { VueEventName } from "../mixpanel/MixPanelHelper";
import { PartType } from "../panes/PartType";
import { ApplicationConfiguration } from "client/ApplicationConfiguration";
import { customRangeCalculation, DateRange, getMomentUnitsFromPeriodType, rangeCalculations } from "./DateHelper";
import moment from "moment";
import { UnreachableCaseError } from "client/helpers/UnreachableCaseError";
import { XAxisFormatting } from "client/helpers/XAxisFormatting";
import toast from "react-hot-toast";

export function getMetricsValidAsBreaks(metrics: Metric[]) {
    return metrics.filter(m => {
        return m.eligibleForCrosstabOrAllVue &&
            m.calcType !== CalculationType.Text &&
            m.entityCombination.length <= 1 &&
            (m.entityCombination.length > 0 || canUseFilterValueMappingAsBreak(m));
    });
}

export function shouldUseFilterValueMappingAsBreak(metric: Metric, multipleChoiceByValue: boolean, isBasedOnSingleChoice: boolean) {
    const useFilterValueMapping = metric.entityCombination.length == 0 || isBasedOnSingleChoice || multipleChoiceByValue;
    return canUseFilterValueMappingAsBreak(metric) && useFilterValueMapping;
}

export function canUseFilterValueMappingAsBreak(metric: Metric) {
    return metric.filterValueMapping.length > 0 && !metric.filterValueMapping.some(f => f.fullText.startsWith("Range"))
}

export const getAvailableCrossMeasureFilterInstances = (metric: Metric, entityConfiguration: IEntityConfiguration,
    multipleChoiceByValue: boolean, isBasedOnSingleChoice: boolean): CrossMeasureFilterInstance[] => {
    if (shouldUseFilterValueMappingAsBreak(metric, multipleChoiceByValue, isBasedOnSingleChoice)) {
        return metric.filterValueMapping.map(filterValue => new CrossMeasureFilterInstance({
            filterValueMappingName: filterValue.fullText,
            instanceId: -1,
        }));
    } else {
        const entityInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(metric.entityCombination[0]);
        return entityInstances.map(instance => new CrossMeasureFilterInstance({
            filterValueMappingName: "",
            instanceId: instance.id,
        }));
    }
}

export const getMetricDisplayName = (metric: Metric | undefined): string => {
    if (!metric) {
        return "";
    }

    return metric.displayName;
}

export const stripHtmlTagsFromHelpText = (helpText: string): string => {
    if (!helpText) {
        return "";
    }
    return helpText.replace('<br>', ' ').replace('<br/>', ' ').replace('<br />', ' ').replace(/<[^>]*>/gm, '');
}

export const getReportPartDisplayText = (reportPart: PartWithExtraData) => {
    if (reportPart.part.helpText && reportPart.part.helpText.trim().length > 0) {
        return stripHtmlTagsFromHelpText(reportPart.part.helpText);
    }
    if (reportPart.metric) {
        return getMetricDisplayName(reportPart.metric);
    }
    return reportPart.part.spec1;
}

export const getReportPartBaseExpressionOverride = (
    reportPart: PartWithExtraData | undefined,
    reportBaseTypeOverride: BaseDefinitionType | undefined,
    reportBaseVariableOverride: number | undefined): BaseExpressionDefinition | undefined => {
    if (!reportPart?.metric?.hasCustomBase) {
        if (reportPart?.part.baseExpressionOverride) {
            return reportPart?.part.baseExpressionOverride;
        }

        const isBaseOverrideForPartDisabled = reportPart?.part.partType == PartType.ReportsCardFunnel;

        if (reportBaseTypeOverride && reportPart?.metric) {
            return new BaseExpressionDefinition({
                baseType: isBaseOverrideForPartDisabled ? BaseDefinitionType.SawThisQuestion : reportBaseTypeOverride,
                baseVariableId: reportPart.metric.baseVariableConfigurationId ?? isBaseOverrideForPartDisabled ? undefined : reportBaseVariableOverride,
                baseMeasureName: reportPart.metric.name,
            });
        }
    }
}

export function baseExpressionDefinitionsAreEqual(a: BaseExpressionDefinition | undefined, b: BaseExpressionDefinition | undefined): boolean {
    return a?.baseType === b?.baseType &&
        a?.baseMeasureName === b?.baseMeasureName &&
        a?.baseVariableId === b?.baseVariableId;
}

export const getEntityInstanceIdsFromEntitySet = (selectedEntitySet: EntitySet | undefined): number[] => {
    return selectedEntitySet ? selectedEntitySet.getInstances().getAll().map(entitySet => entitySet.id) : []
}

export const getFormattedValueText = (value: number, metric: Metric, decimalPlaces: number) => {
    switch (decimalPlaces) {
        case 1:
            return metric.longFmt(value);
        case 2:
            return metric.extraLongFmt(value);
        default:
            return metric.fmt(value);
    }
}

export const descriptionOfOrder = (order: ReportOrder): string => {
    switch (order) {
        case order = ReportOrder.ResultOrderAsc:
            return "Ascending";
        case order = ReportOrder.ResultOrderDesc:
            return "Descending";
        case order = ReportOrder.ScriptOrderDesc:
            return "Script order";
        case order = ReportOrder.ScriptOrderAsc:
            return "Reverse script order";
        default:
            return "";
    }
}

export const baseTypeDisplayName = (baseType: BaseDefinitionType | undefined): string => {
    switch (baseType) {
        case BaseDefinitionType.AllRespondents:
            return "All respondents";
        case BaseDefinitionType.SawThisQuestion:
            return "Respondents who saw the question";
        case BaseDefinitionType.SawThisChoice:
            return "Respondents who saw the choice";
        default:
            return "";
    }
}

export const baseExpressionDefinitionDisplayName = (baseDefinition: BaseExpressionDefinition, baseVariables: VariableConfigurationModel[]): string => {
    if (baseDefinition.baseVariableId) {
        const variable = baseVariables.find(v => v.id === baseDefinition.baseVariableId);
        return variable?.displayName ?? "Custom base";
    }

    return baseTypeDisplayName(baseDefinition.baseType);
}

export type SplitByAndFilterByEntityTypes = {
    splitByEntityType: IEntityType | undefined;
    filterByEntityTypes: IEntityType[];
}

export const getSplitByAndFilterByEntityTypesForMetric = (metric: Metric | undefined, entityConfiguration: IEntityConfiguration): SplitByAndFilterByEntityTypes => {
    if (metric)
        return getSplitByAndFilterByEntityTypes(metric, metric.defaultSplitByEntityTypeName, entityConfiguration);

    return { splitByEntityType: undefined, filterByEntityTypes: [] };
}

export const getSplitByAndFilterByEntityTypesForPart = (part: PartDescriptor, metric: Metric | undefined, entityConfiguration: IEntityConfiguration): SplitByAndFilterByEntityTypes | undefined => {
    if (metric) {
        if (part.multipleEntitySplitByAndFilterBy) {
            return getEntityTypesFromMultipleEntity(metric, part.multipleEntitySplitByAndFilterBy);
        }
        //TODO: can this be removed?
        return getSplitByAndFilterByEntityTypes(metric, part.defaultSplitBy, entityConfiguration);
    }
}

export const getEntityTypesFromMultipleEntity = (metric: Metric, multiEntityConfig: MultipleEntitySplitByAndFilterBy): SplitByAndFilterByEntityTypes => {
    const splitBy = multiEntityConfig.splitByEntityType;
    const filterBy = multiEntityConfig.filterByEntityTypes.map(t => t.type);
    return {
        splitByEntityType: metric.entityCombination.find(type => type.identifier === splitBy),
        filterByEntityTypes: metric.entityCombination.filter(type => filterBy.includes(type.identifier))
    };
}

export const getSplitByAndFilterByEntityTypes = (metric: Metric, defaultSplitByEntityTypeName: string, entityConfiguration: IEntityConfiguration): SplitByAndFilterByEntityTypes => {
    //zero entity
    if (metric.entityCombination.length == 0) {
        return { splitByEntityType: undefined, filterByEntityTypes: [] };
    }

    //single entity
    if (metric.entityCombination.length == 1) {
        return { splitByEntityType: metric.entityCombination[0], filterByEntityTypes: [] };
    }

    //multientity by default split by type
    if (defaultSplitByEntityTypeName && defaultSplitByEntityTypeName.trim().length > 0) {
        const splitByType = metric.entityCombination.find(t => t.identifier == defaultSplitByEntityTypeName) ?? metric.entityCombination[0];
        const filterByTypes = metric.entityCombination.filter(t => t.identifier != splitByType.identifier);

        if (filterByTypes.length == 0) {
            throw new Error("Failed to match entity types to metric");
        }

        return { splitByEntityType: splitByType, filterByEntityTypes: filterByTypes };
    }

    //multientity by best guess
    const orderedTypes = metric.entityCombination.map(type => {
        return {
            type: type,
            numberInstances: entityConfiguration.getAllEnabledInstancesOrderedAsSet(type).getInstances().getAll().length
        };
    }).sort((a, b) => a.numberInstances - b.numberInstances).map(a => a.type);
    return { splitByEntityType: orderedTypes[0], filterByEntityTypes: orderedTypes.slice(1) };
}

export type PrimaryAndSecondaryFilterInstances = {
    primaryFilterInstance?: FilterInstance;
    secondaryFilterInstances: FilterInstance[];
}

export const getFilterInstancesForPart = (part: PartDescriptor, entityTypes: SplitByAndFilterByEntityTypes, entityConfiguration: IEntityConfiguration): PrimaryAndSecondaryFilterInstances => {
    const unionedTypes = [entityTypes.splitByEntityType, ...entityTypes.filterByEntityTypes]
        .filter((t): t is IEntityType => t != undefined);
    const getFilterInstance = (typeAndInstance: EntityTypeAndInstance) => {
        const type = unionedTypes.find(t => t.identifier === typeAndInstance.type);
        if (!type) {
            throw new Error("Failed to match filter by types");
        }
        const allInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(type);
        return new FilterInstance(type, allInstances.find(i => i.id === typeAndInstance.instance) ?? allInstances[0]);
    }

    const primaryFilterInstance = entityTypes.splitByEntityType ?
        getFilterInstance(new EntityTypeAndInstance({ type: entityTypes.splitByEntityType.identifier, instance: part.multiBreakSelectedEntityInstance ?? -1 })) :
        undefined;
    const secondaryFilterInstances = part.multipleEntitySplitByAndFilterBy.filterByEntityTypes.map(getFilterInstance);
    return {
        primaryFilterInstance: primaryFilterInstance,
        secondaryFilterInstances: secondaryFilterInstances
    };
}

export const updateFiltersWithSelectedProperies = (originalFilters: CuratedFilters,
    averages: IAverageDescriptor[],
    isDataWeighted: boolean,
    filters: MetricFilterState[],
    isSurveyVue: boolean,
    selectedWave?: ICustomPeriod,
    startDate?: Date,
    endDate?: Date,
    average?: IAverageDescriptor
): CuratedFilters => {
    if (isSurveyVue) {
        const newFilters = _.cloneDeep(originalFilters);
        if (selectedWave && (originalFilters.startDate !== selectedWave.startDate || originalFilters.endDate !== selectedWave.endDate)) {
            newFilters.setDates(selectedWave.startDate, selectedWave.endDate);
        }
        if (isDataWeighted) {
            if (!isCustomPeriodAverage(newFilters.average)) {
                throw "Weighting periods other than 'all data' is not yet supported for this view, please contact support if this is unexpected.";
            }
            const newAverage = averages.find(a => isCustomPeriodAverage(a) && a.weightingMethod === WeightingMethod.QuotaCell);
            if (newAverage == null) {
                throw "Weighted average is not supported for this dashboard.";
            }
            newFilters.average = newAverage;
        }

        if (startDate && endDate) {
            newFilters.setDates(startDate, endDate);
        }
        if (average) {
            newFilters.average = average;
        }

        applyMetricFiltersToCuratedFilters(newFilters, filters);
        return newFilters;
    }

    return originalFilters;
}

export const getDefaultWave = (originalFilters: CuratedFilters): ICustomPeriod => ({
    name: "Remove filter",
    startDate: originalFilters.startDate,
    endDate: originalFilters.endDate,
    productShortCode: "",
    subProductId: "",
    organisation: "",
    id: -1
});

export const getAnalyticsAverageAddedEvent = (averageType: AverageType, reportType: ReportType): ActionEventName => {
    switch (averageType) {
        case AverageType.Mean: return reportType == ReportType.Chart ? 'allVueChartAverageMeanAdded' : 'allVueTableAverageMeanAdded';
        case AverageType.Median: return reportType == ReportType.Chart ? 'allVueChartAverageMedianAdded' : 'allVueTableAverageMedianAdded';
        case AverageType.Mentions: return reportType == ReportType.Chart ? 'allVueChartAverageMentionsAdded' : 'allVueTableAverageMentionsAdded';
        default: throw Error('Unhandled average type: ' + averageType);
    }
}

export const getAnalyticsAverageRemovedEvent = (averageType: AverageType, reportType: ReportType): ActionEventName => {
    switch (averageType) {
        case AverageType.Mean: return reportType == ReportType.Chart ? 'allVueChartAverageMeanRemoved' : 'allVueTableAverageMeanRemoved';
        case AverageType.Median: return reportType == ReportType.Chart ? 'allVueChartAverageMedianRemoved' : 'allVueTableAverageMedianRemoved';
        case AverageType.Mentions: return reportType == ReportType.Chart ? 'allVueChartAverageMentionsRemoved' : 'allVueTableAverageMentionsRemoved';
        default: throw Error('Unhandled average type: ' + averageType);
    }
}

export const getMixPanelAddAverageEvent = (averageType: AverageType): VueEventName => {
    switch (averageType) {
        case AverageType.Mean: return 'addedMeanAverage';
        case AverageType.Median: return 'addedMedianAverage';
        case AverageType.Mentions: return 'addedAverageMentions';
        default: throw Error('Unhandled average type: ' + averageType);
    }
}

export const getMixPanelRemoveAverageEvent = (averageType: AverageType): VueEventName => {
    switch (averageType) {
        case AverageType.Mean: return 'removedMeanAverage';
        case AverageType.Median: return 'removedMedianAverage';
        case AverageType.Mentions: return 'removedAverageMentions';
        default: throw Error('Unhandled average type: ' + averageType);
    }
}

export function getTypedEmptyArray<Type>(): Type[] {
    //this is needed because with 'noImplicitAny', declaring empty array without type treats
    //it as never[] which generates type errors when passed to things expecting the specific Type
    const typedArray: Type[] = [];
    return typedArray;
}

export function hasSingleEntityInstance(metric: Metric | undefined, instanceIds: number[] | undefined) {
    //disable averages if there is only 1 entity instance, as the result would be the same as the entity instance result
    //this is not the case for numeric variables which would have a different result (percentage for entity instance and numeric average for average)
    if (metric && instanceIds) {
        return metric.entityCombination.length == 0 || (!metric.isNumericVariable && instanceIds.length <= 1);
    }
    return true;
}

export function isInfoPageMetric(metric: Metric, questionTypeLookup: { [key: string]: MainQuestionType }) {
    const questionType = questionTypeLookup[metric.name];
    return metric.entityCombination.length == 0 && (
        questionType == MainQuestionType.SingleChoice || questionType == MainQuestionType.MultipleChoice
    );
}

export const getMetricDisplayText = (metric: Metric | undefined): string => {
    if (!metric) {
        return "";
    }

    if (metric.helpText && metric.helpText.trim().length > 0) {
        return stripHtmlTagsFromHelptext(metric.helpText);
    }

    return metric.displayName;
}

export const stripHtmlTagsFromHelptext = (helptext: string): string => {
    if (!helptext) {
        return "";
    }
    return helptext.replace('<br>', ' ').replace('<br/>', ' ').replace('<br />', ' ').replace(/<[^>]*>/gm, '');
}

export function getDateRangeLookup(applicationConfiguration: ApplicationConfiguration): DateRange[] {
    const minimum = moment.utc(applicationConfiguration.dateOfFirstDataPoint);
    const maximum = moment.utc(applicationConfiguration.dateOfLastDataPoint);
    const now = () => moment.utc(maximum);

    return rangeCalculations(now, minimum, maximum)
        .filter(r => r.start.isSameOrAfter(minimum));
}

export function getDefaultOverTimeSettings(config: ReportOverTimeConfiguration | undefined, userVisibleAverages: IAverageDescriptor[], dateRangeLookup: DateRange[], applicationConfiguration: ApplicationConfiguration) {
    let startDate: Date;
    let endDate: Date;
    let average: IAverageDescriptor;

    if (config?.customRange) {
        const { start, end } = customRangeCalculation(config.customRange, applicationConfiguration);
        startDate = start.toDate();
        endDate = end.toDate();
    } else {
        const dateRange = dateRangeLookup.find(r => r.url === config?.range) ??
            dateRangeLookup.find(r => r.name === "This quarter") ??
            dateRangeLookup[0];
        startDate = dateRange.start.toDate();
        endDate = dateRange.end.toDate();
    }
    average = userVisibleAverages.find(a => a.averageId === config?.averageId) ??
        userVisibleAverages.find(a => a.isDefault) ?? userVisibleAverages[0];

    return {
        startDate: startDate,
        endDate: endDate,
        average: average
    };
}

export function getUserVisibleAverages(applicationConfiguration: ApplicationConfiguration,
    averages: IAverageDescriptor[],
    isDataWeighted: boolean,
    subsetId: string): IAverageDescriptor[] {
    const averagesForSubset = subsetId 
        ? averages.filter(a => a.subset.some(s => s.id == subsetId) || a.subset.length == 0 || a.subset == undefined)
        : averages;

    const first = moment.utc(applicationConfiguration.dateOfFirstDataPoint);
    const last = moment.utc(applicationConfiguration.dateOfLastDataPoint);

    function isAverageWithinData(a: IAverageDescriptor): boolean {
        let requiredStart: moment.Moment;
        switch (a.totalisationPeriodUnit) {
            case TotalisationPeriodUnit.All:
                return true;
            case TotalisationPeriodUnit.Day:
                requiredStart = last.clone().subtract(a.numberOfPeriodsInAverage, "days");
                break;
            case TotalisationPeriodUnit.Month:
                requiredStart = last.clone().subtract(a.numberOfPeriodsInAverage, "months");
                break;
            default:
                throw new UnreachableCaseError(a.totalisationPeriodUnit);
        }

        return requiredStart.isSameOrAfter(first, "day");
    }

    const requiredWeightingMethod = isDataWeighted ? WeightingMethod.QuotaCell : WeightingMethod.None;
    return averagesForSubset.filter(a => !a.isHiddenFromUsers 
        && a.weightingMethod === requiredWeightingMethod && isAverageWithinData(a));
}

export function getOverTimeChartCategories(average: IAverageDescriptor, results: WeightedDailyResult[]) {
    return results.map(r => formatOverTimeDate(average, r.date));
}

export function formatOverTimeDate(average: IAverageDescriptor, date: Date) {
    const { labelXformatter } = XAxisFormatting.formatAll(average.makeUpTo);
    const formatter = labelXformatter.replace("<br>", " ");
    const dateToFormat = moment(date);

    if (average.makeUpTo === MakeUpTo.HalfYearEnd) {
        return dateToFormat.format(formatter).replace("$$HY$$", dateToFormat.month() < 6 ? "1st" : "2nd");
    }

    return dateToFormat.format(formatter);
}

export function customDateRangeToText(customDateRange: CustomDateRange) {
    const period = customDateRange.periodType.toLowerCase();
    if (customDateRange.numberOfPeriods === 1) {
        return `Last ${period}`;
    }
    return `Last ${customDateRange.numberOfPeriods} ${period}s`;
}

export function getDateRangePickerTitleFromDates(startDate?: Date, endDate?: Date) {
    if (startDate && endDate) {
        return getDateRangePickerTitleFromMoments(moment.utc(startDate), moment.utc(endDate));
    }
    return "Select dates";
}

export function getDateRangePickerTitleFromMoments(start?: moment.Moment, end?: moment.Moment) {
    if (start && end) {
        if (
            start.isSame(end, "month") &&
            start.clone().startOf("month").isSame(start, "day") &&
            end.clone().endOf("month").isSame(end, "day")
        ) {
            return start.format("MMM YY");
        }

        const totalMonths = end.diff(start, "months", true);

        if (totalMonths < 1) {
            if (start.month() === end.month()) {
                return `${start.format("D")}-${end.format("D MMM YY")}`;
            } else {
                return `${start.format("D MMM")} - ${end.format("D MMM YY")}`;
            }
        }

        return `${start.format("MMM YY")} - ${end.format("MMM YY")}`;
    }
    return "Select dates";
}

export function getFullPeriodsWithin(startDate: Date, endDate: Date): PeriodType[] {
    const start = moment.utc(startDate);
    const end = moment.utc(endDate);

    function fits(periodType: PeriodType): boolean {
        const { duration, period } = getMomentUnitsFromPeriodType(periodType);
        let first = start.clone().startOf(period);
        if (first.isBefore(start)) first.add(1, duration);
        let last = first.clone().endOf(period);
        return last.isSameOrBefore(end);
    }

    const conditionalPeriodTypes = [PeriodType.Week, PeriodType.Month, PeriodType.Quarter, PeriodType.Year];
    return [
        PeriodType.Day,
        ...conditionalPeriodTypes.filter(periodType => fits(periodType))
    ];
}

export function handleError(error: any): void {
    const errorMessage = getErrorMessage(error);
    const errorDisplayDurationMs = 5000;
    toast.error(errorMessage, { duration: errorDisplayDurationMs });
}

export function handleErrorWithCustomText(error: any, text: string): void {
    const errorDisplayDurationMs = 5000;

    if (error && SwaggerException.isSwaggerException(error)) {
        try {
            const swaggerException = error as SwaggerException;
            const responseJson = JSON.parse(swaggerException.response);
            const detail = responseJson?.detail ?? responseJson?.message ?? JSON.stringify(responseJson);
            const message = text + ". " + detail;
            toast.error(message, { duration: errorDisplayDurationMs });
            return;
        } catch {
            // fall through to generic message below
        }
    }

    toast.error(text, { duration: errorDisplayDurationMs });
}

export function getErrorMessage(error: any): string {
    const defaultMessage = "An error occurred";
    if (!error) return defaultMessage;

    // Swagger generated exceptions often carry a JSON response string
    if (error && SwaggerException.isSwaggerException(error)) {
        try {
            const swaggerException = error as SwaggerException;
            const parsed = JSON.parse(swaggerException.response);
            if (typeof parsed === 'string') return parsed;
            if (parsed?.detail) return parsed.detail;
            if (parsed?.message) return parsed.message;
            return swaggerException.response;
        } catch (e) {
            // If parsing fails, fall back to the raw response or the default
            const swaggerException = error as SwaggerException;
            return (swaggerException.response && swaggerException.response.toString()) || defaultMessage;
        }
    }

    // plain string
    if (typeof error === 'string') return error;

    // JS Error instance
    if (error instanceof Error) return error.message || defaultMessage;

    // objects with message property - ensure message is a string
    if (error?.message) {
        const m = error.message;
        if (typeof m === 'string') return m;
        try {
            return JSON.stringify(m);
        } catch {
            return defaultMessage;
        }
    }

    return defaultMessage;
}