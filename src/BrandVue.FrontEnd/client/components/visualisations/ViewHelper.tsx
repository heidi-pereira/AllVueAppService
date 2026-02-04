import { CuratedFilters } from "../../filter/CuratedFilters";
import * as BrandVueApi from "../../BrandVueApi";
import { Metric } from "../../metrics/metric";
import { EntitySet } from "../../entity/EntitySet";
import { FilterInstance } from "../../entity/FilterInstance";
import DataSortOrder = BrandVueApi.DataSortOrder;
import {

    BaseExpressionDefinition,
    ComparisonPeriodSelection,
    DemographicFilter,
    FilterOperator,
    SigConfidenceLevel,
} from "../../BrandVueApi";
import { ITimeSelectionOptions } from "../../state/ITimeSelectionOptions";

export interface IRequestModelOverrideParams {
    continuousPeriod?: boolean;
    ordering?: string[];
    orderingDirection?: DataSortOrder;
    useScorecardDates?: boolean;
    comparisonPeriodSelection?: ComparisonPeriodSelection;
    baseExpressionOverride?: BaseExpressionDefinition;
}

interface IMultiEntityRequestModelParams {
    curatedFilters: CuratedFilters;
    metric: Metric;
    splitBySet: EntitySet;
    filterInstances: FilterInstance[];
    continuousPeriod: boolean;
    subsetId: string;
    baseExpressionOverride?: BaseExpressionDefinition;
    focusEntityId?: number;
    includeSignificance?: boolean;
    sigConfidenceLevel?: SigConfidenceLevel;
}

interface IMultiEntityRequestModelForInstancesParams {
    curatedFilters: CuratedFilters;
    metric: Metric;
    splitBySet?: EntitySet;
    splitByEntityInstanceIds: number[];
    filterInstances: FilterInstance[];
    continuousPeriod: boolean;
    subsetId: string;
    baseExpressionOverride?: BaseExpressionDefinition;
    focusEntityInstanceId?: number;
    includeSignificance?: boolean;
    sigConfidenceLevel?: SigConfidenceLevel;
}

export class ViewHelper {
    static createAverageRequestModelOrNull(
        brandIds: number[],
        metrics: Metric[],
        curatedFilters: CuratedFilters,
        activeBrandId: number,
        requestModelOverrideParams: IRequestModelOverrideParams,
        subsetId: string,
        timeSelection: ITimeSelectionOptions,
    ) {
        const thereAreNoBrandsToCompareAgainst = !brandIds.some((b) => b !== activeBrandId);
        if (thereAreNoBrandsToCompareAgainst) return null;

        const paramsForCurrentPeriodOnly = {
            ...requestModelOverrideParams,
            comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly,
        };
        return this.createCuratedRequestModel(brandIds, metrics, curatedFilters, activeBrandId, paramsForCurrentPeriodOnly, subsetId, timeSelection);
    }

    static createCompetitionAverageRequestModelOrNull(
        brandIds: number[],
        metrics: Metric[],
        curatedFilters: CuratedFilters,
        activeBrandId: number,
        comparison: ComparisonPeriodSelection,
        subsetId: string,
        timeSelection: ITimeSelectionOptions,
    ) {
        const thereAreNoBrandsToCompareAgainst = !brandIds.some((b) => b !== activeBrandId);
        if (thereAreNoBrandsToCompareAgainst) return null;

        const paramsForPeriod: IRequestModelOverrideParams = {
            comparisonPeriodSelection: comparison,
        };
        return this.createCuratedRequestModel(brandIds, metrics, curatedFilters, activeBrandId, paramsForPeriod, subsetId, timeSelection);
    }

    static createAverageRequestModelForMultipleEntities(
        curatedFilters: CuratedFilters,
        metric: Metric,
        splitBySet: EntitySet,
        filterInstances: FilterInstance[],
        continuousPeriod: boolean,
        subsetId: string,
        timeSelection: ITimeSelectionOptions,
        requestModelOverrideParams?: IRequestModelOverrideParams
    ): BrandVueApi.MultiEntityRequestModel {
        return new BrandVueApi.MultiEntityRequestModel({
            measureName: metric.name,
            subsetId: subsetId,
            period: new BrandVueApi.Period({
                average: curatedFilters.average.averageId,
                comparisonDates: curatedFilters.comparisonDates(
                    false,
                    timeSelection,
                    continuousPeriod,
                    requestModelOverrideParams?.comparisonPeriodSelection ?? ComparisonPeriodSelection.CurrentPeriodOnly
                ),
            }),
            demographicFilter: curatedFilters.demographicFilter,
            dataRequest: new BrandVueApi.EntityInstanceRequest({
                type: splitBySet.type.identifier,
                entityInstanceIds: splitBySet
                    .getInstances()
                    .getAll()
                    .map((b) => b.id),
            }),
            filterBy: filterInstances.map(
                (filterInstance) =>
                    new BrandVueApi.EntityInstanceRequest({
                        type: filterInstance.type.identifier,
                        entityInstanceIds: [filterInstance.instance.id],
                    })
            ),
            filterModel: new BrandVueApi.CompositeFilterModel({
                filterOperator: FilterOperator.And,
                filters: curatedFilters.measureFilters,
                compositeFilters: curatedFilters.compositeFilters,
            }),
            additionalMeasureFilters: [],
            baseExpressionOverrides: [],
            includeSignificance: false,
            sigConfidenceLevel: SigConfidenceLevel.NinetyFive,
        });
    }

    static createCompetitionAverageRequestModelForMultipleEntities(
        curatedFilters: CuratedFilters,
        metric: Metric,
        splitBySet: EntitySet,
        filterInstances: FilterInstance[],
        subsetId: string,
        timeSelection: ITimeSelectionOptions
    ): BrandVueApi.MultiEntityRequestModel {
        return new BrandVueApi.MultiEntityRequestModel({
            measureName: metric.name,
            subsetId: subsetId,
            period: new BrandVueApi.Period({
                average: curatedFilters.average.averageId,
                comparisonDates: curatedFilters.comparisonDates(false,  timeSelection),
            }),
            demographicFilter: curatedFilters.demographicFilter,
            dataRequest: new BrandVueApi.EntityInstanceRequest({
                type: splitBySet.type.identifier,
                entityInstanceIds: splitBySet
                    .getInstances()
                    .getAll()
                    .map((b) => b.id),
            }),
            filterBy: filterInstances.map(
                (filterInstance) =>
                    new BrandVueApi.EntityInstanceRequest({
                        type: filterInstance.type.identifier,
                        entityInstanceIds: [filterInstance.instance.id],
                    })
            ),
            filterModel: new BrandVueApi.CompositeFilterModel({
                filterOperator: FilterOperator.And,
                filters: curatedFilters.measureFilters,
                compositeFilters: curatedFilters.compositeFilters,
            }),
            additionalMeasureFilters: [],
            baseExpressionOverrides: [],
            includeSignificance: false,
            sigConfidenceLevel: SigConfidenceLevel.NinetyFive,
        });
    }

    static createCuratedRequestModel(
        brandIds: number[],
        metrics: Metric[],
        curatedFilters: CuratedFilters,
        activeBrandId: number,
        {
            continuousPeriod = false,
            ordering = [],
            orderingDirection = DataSortOrder.Ascending,
            useScorecardDates = false,
            comparisonPeriodSelection = curatedFilters.comparisonPeriodSelection,
            baseExpressionOverride = undefined,
        }: IRequestModelOverrideParams,
        subsetId: string,
        timeSelection: ITimeSelectionOptions,
        includeSignificance?: boolean
    ) {
        const average = useScorecardDates ? timeSelection.scorecardAverage : curatedFilters.average;
        const comparisonDates = curatedFilters.comparisonDates(useScorecardDates, timeSelection, continuousPeriod, comparisonPeriodSelection);

        const period = new BrandVueApi.Period({
            average: average.averageId,
            comparisonDates: comparisonDates,
        });

        const sigDiffOptions = new BrandVueApi.SigDiffOptions({
            highlightSignificance: includeSignificance ?? false,
            sigConfidenceLevel: SigConfidenceLevel.NinetyFive,
            displaySignificanceDifferences: BrandVueApi.DisplaySignificanceDifferences.None,
            significanceType: BrandVueApi.CrosstabSignificanceType.CompareToTotal,
        });

        return new BrandVueApi.CuratedResultsModel({
            entityInstanceIds: brandIds,
            measureName: metrics.map((m) => m.name),
            subsetId: subsetId,
            period: period,
            demographicFilter: metrics.some((m) => !m.isMetricFilterable()) ? new DemographicFilter() : curatedFilters.demographicFilter,
            activeBrandId: activeBrandId,
            filterModel: new BrandVueApi.CompositeFilterModel({
                filterOperator: FilterOperator.And,
                filters: metrics.some((m) => !m.isMetricFilterable()) ? [] : curatedFilters.measureFilters,
                compositeFilters: metrics.some((m) => !m.isMetricFilterable()) ? [] : curatedFilters.compositeFilters,
            }),
            ordering: ordering,
            orderingDirection: orderingDirection,
            additionalMeasureFilters: [],
            baseExpressionOverride: baseExpressionOverride,
            includeSignificance: sigDiffOptions.highlightSignificance,
            sigConfidenceLevel: sigDiffOptions.sigConfidenceLevel,
            sigDiffOptions,
        });
    }

    static createMultiEntityRequestModel({
            curatedFilters,
            metric,
            splitBySet,
            filterInstances,
            continuousPeriod,
            baseExpressionOverride,
            focusEntityId,
            includeSignificance,
            subsetId,
            sigConfidenceLevel = SigConfidenceLevel.NinetyFive,
        }: IMultiEntityRequestModelParams, 
        timeSelection: ITimeSelectionOptions): BrandVueApi.MultiEntityRequestModel {
        const splitByEntityInstanceIds = splitBySet
            ? splitBySet
                  .getInstances()
                  .getAll()
                  .map((b) => b.id)
            : [];
        if (splitBySet?.type.isBrand && splitBySet.mainInstance != null && !splitByEntityInstanceIds.includes(splitBySet.mainInstance.id)) {
            splitByEntityInstanceIds.push(splitBySet.mainInstance.id);
        }
        return ViewHelper.createMultiEntityRequestModelForInstances({
            curatedFilters,
            metric,
            splitBySet,
            splitByEntityInstanceIds,
            filterInstances,
            continuousPeriod,
            baseExpressionOverride,
            focusEntityInstanceId: focusEntityId,
            includeSignificance,
            subsetId,
            sigConfidenceLevel,
        }, timeSelection);
    }

    static createMultiEntityRequestModelForInstances({
        curatedFilters,
        metric,
        splitBySet,
        splitByEntityInstanceIds,
        filterInstances,
        continuousPeriod,
        baseExpressionOverride,
        focusEntityInstanceId,
        includeSignificance,
        subsetId,
        sigConfidenceLevel,
    }: IMultiEntityRequestModelForInstancesParams, 
    timeSelection: ITimeSelectionOptions): BrandVueApi.MultiEntityRequestModel {
        let activeInstanceId = focusEntityInstanceId;
        if (activeInstanceId && splitBySet) {
            if (splitBySet.getInstances().getById(activeInstanceId) == null) {
                if (splitBySet.mainInstance) {
                    activeInstanceId = splitBySet.mainInstance.id;
                }
                if (splitBySet.getInstances().getById(activeInstanceId) == null) {
                    activeInstanceId = undefined;
                }
            }
        }

        const entityInstanceRequest = (entitySet: EntitySet | undefined, entityInstanceIds: number[]) => {
            if (entitySet) {
                return new BrandVueApi.EntityInstanceRequest({
                    type: entitySet.type.identifier,
                    entityInstanceIds: entityInstanceIds,
                });
            }
        };
        return new BrandVueApi.MultiEntityRequestModel({
            measureName: metric.name,
            subsetId: subsetId,
            period: new BrandVueApi.Period({
                average: curatedFilters.average.averageId,
                comparisonDates: curatedFilters.comparisonDates(false, timeSelection, continuousPeriod, curatedFilters.comparisonPeriodSelection),
            }),
            demographicFilter: curatedFilters.demographicFilter,
            dataRequest: entityInstanceRequest(splitBySet, splitByEntityInstanceIds),
            filterBy: filterInstances.map(
                (filterInstance) =>
                    new BrandVueApi.EntityInstanceRequest({
                        type: filterInstance.type.identifier,
                        entityInstanceIds: [filterInstance.instance.id],
                    })
            ),
            filterModel: new BrandVueApi.CompositeFilterModel({
                filterOperator: FilterOperator.And,
                filters: curatedFilters.measureFilters,
                compositeFilters: curatedFilters.compositeFilters,
            }),
            additionalMeasureFilters: [],
            baseExpressionOverrides: baseExpressionOverride ? [baseExpressionOverride] : [],
            focusEntityInstanceId: activeInstanceId,
            includeSignificance: includeSignificance ?? false,
            sigConfidenceLevel: sigConfidenceLevel ?? SigConfidenceLevel.NinetyFive,
        });
    }

    static createStackedMultiEntityRequestModel(
        curatedFilters: CuratedFilters,
        metric: Metric,
        splitBySet: EntitySet,
        filterBySet: EntitySet,
        continuousPeriod: boolean,
        subsetId: string,
        timeSelection: ITimeSelectionOptions,
        baseExpressionOverride?: BaseExpressionDefinition
    ): BrandVueApi.StackedMultiEntityRequestModel {
        const splitByEntityInstanceIds = splitBySet
            .getInstances()
            .getAll()
            .map((b) => b.id);
        if (splitBySet.type.isBrand && splitBySet.mainInstance != null && !splitByEntityInstanceIds.includes(splitBySet.mainInstance.id)) {
            splitByEntityInstanceIds.push(splitBySet.mainInstance.id);
        }

        return this.createStackedMultiEntityRequestModelFromInstances(
            curatedFilters,
            metric,
            splitByEntityInstanceIds,
            splitBySet.type.identifier,
            filterBySet,
            continuousPeriod,
            subsetId,
            timeSelection,
            baseExpressionOverride
        );
    }

    static createStackedMultiEntityRequestModelFromInstances(
        curatedFilters: CuratedFilters,
        metric: Metric,
        entityInstanceIds: number[],
        entityType: string,
        filterBySet: EntitySet,
        continuousPeriod: boolean,
        subsetId: string,
        timeSelection: ITimeSelectionOptions,
        baseExpressionOverride?: BaseExpressionDefinition
    ): BrandVueApi.StackedMultiEntityRequestModel {
        const filterByEntityInstanceIds = filterBySet
            .getInstances()
            .getAll()
            .map((b) => b.id);
        if (filterBySet.type.isBrand && filterBySet.mainInstance != null && !filterByEntityInstanceIds.includes(filterBySet.mainInstance.id)) {
            filterByEntityInstanceIds.push(filterBySet.mainInstance.id);
        }

        return new BrandVueApi.StackedMultiEntityRequestModel({
            measureName: metric.name,
            subsetId: subsetId,
            period: new BrandVueApi.Period({
                average: curatedFilters.average.averageId,
                comparisonDates: curatedFilters.comparisonDates(false, timeSelection, continuousPeriod, curatedFilters.comparisonPeriodSelection),
            }),
            demographicFilter: curatedFilters.demographicFilter,
            splitBy: new BrandVueApi.EntityInstanceRequest({
                type: entityType,
                entityInstanceIds: entityInstanceIds,
            }),
            filterBy: new BrandVueApi.EntityInstanceRequest({
                type: filterBySet.type.identifier,
                entityInstanceIds: filterByEntityInstanceIds,
            }),
            filterModel: new BrandVueApi.CompositeFilterModel({
                filterOperator: FilterOperator.And,
                filters: curatedFilters.measureFilters,
                compositeFilters: curatedFilters.compositeFilters,
            }),
            additionalMeasureFilters: [],
            baseExpressionOverride: baseExpressionOverride,
        });
    }

    static createAverageDescription(metrics: Metric[]): string {
        if (metrics.some((m) => !m.isBrandMetric())) return "Average";

        const weightedMetrics = metrics.filter((m) => m.averageDescription).length;
        if (weightedMetrics === metrics.length) return "Audience average - all competitors";
        if (weightedMetrics === 0) return "Brand average - all competitors";
        console.error(`Inconsistent average type for metrics: ${JSON.stringify(metrics.map((m) => m.name))}`);
        return "Average - all competitors";
    }

    private static calcMaxValue(series: any): number {
        let result = Number.MIN_SAFE_INTEGER;
        series.map((s: any) =>
            s.data.map((d: any) => {
                if (d.y > result) result = d.y;
            })
        );
        return result;
    }

    private static calcMinValue(series: any): number {
        let result = Number.MAX_SAFE_INTEGER;
        series.map((s: any) =>
            s.data.map((d: any) => {
                if (d.y < result) result = d.y;
            })
        );
        return result;
    }

    static calcMaxMinusMinValue(series: any): number {
        let result = 0;

        const max = this.calcMaxValue(series);
        if (max !== Number.MIN_SAFE_INTEGER) {
            const min = this.calcMinValue(series);
            if (min !== Number.MAX_SAFE_INTEGER) {
                result = max - min;
            }
        }
        return result;
    }
    static calcMaxMinusMinValueBySeries(series: any): number {
        let result = Number.MAX_SAFE_INTEGER;

        series.map((s: any) => {
            let max = Number.MIN_SAFE_INTEGER;
            let min = Number.MAX_SAFE_INTEGER;
            s.data.map((d: any) => {
                if (d.y < min) min = d.y;
                if (d.y > max) max = d.y;
            });
            if (max !== Number.MIN_SAFE_INTEGER) {
                let localMinMax = max - min;
                if (localMinMax < result) result = localMinMax;
            }
        });
        if (result !== Number.MAX_SAFE_INTEGER) return result;
        return 0;
    }

    //
    //https://api.highcharts.com/highcharts/plotOptions.series.marker.enabledThreshold
    //
    // The threshold for how dense the point markers should be before they are hidden, given that enabled is not defined.
    // The number indicates the horizontal distance between the two closest points in the series, as multiples of the marker.radius.
    // In other words, the default value of 2 means points are hidden if overlapping horizontally.
    //
    //31 was arbitaryly chosen because we did not want to show points for days in a month...
    static enabledThreshold: number = 31;
}
