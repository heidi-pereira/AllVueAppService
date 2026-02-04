import { FilterValueMapping } from "../../../../metrics/metricSet";
import { CompositeFilterModel, DefaultReportFilter, DemographicFilter, FilterOperator, MeasureFilterRequestModel } from "../../../../BrandVueApi";
import { MetricFilterState } from "../../../../filter/metricFilterState";
import { Metric } from "../../../../metrics/metric";
import { CuratedFilters } from "../../../../filter/CuratedFilters";
import { EntityInstance } from "../../../../entity/EntityInstance";
import { IEntityConfiguration } from "../../../../entity/EntityConfiguration";
import { ArrayHelper } from "../../../helpers/ArrayHelper";

export interface IFilterAndDefaultInstances {
    defaultReportFilter: DefaultReportFilter | undefined;
    metric: Metric | undefined;
    filters: MetricFilterState[];
}


export function getMetricFilter(metric: Metric, filterValueMapping: FilterValueMapping, entityInstances: {[entityType: string]: number[]}): MetricFilterState {
    const values = [...filterValueMapping.values];

    const invert = values[0].startsWith("!");
    if (invert) {
        values[0] = values[0].slice(1);
    }

    var newFilter = new MetricFilterState();
    newFilter.entityInstances = entityInstances;
    newFilter.metric = metric;
    newFilter.name = metric.name;
    newFilter.isAdvanced = true;
    newFilter.isRange = false;
    newFilter.treatPrimaryValuesAsRange = false;
    newFilter.invert = invert;

    const rangeValues = MetricFilterState.getFilterRangeValues(values.join(","));
    if (rangeValues) {
        newFilter.values = rangeValues;
        newFilter.treatPrimaryValuesAsRange = true;

    } else {
        newFilter.values = values.map(v => parseInt(v));
        newFilter.treatPrimaryValuesAsRange = false;
    }

    return newFilter;
}

export function getMetricFilterFromDefault(defaultFilter: DefaultReportFilter, metrics: Metric[], entityConfuration: IEntityConfiguration) : IFilterAndDefaultInstances {

    const metric = metrics.find(m => m.name === defaultFilter.measureName);
    const resultFilters: MetricFilterState[] = metric == null ? [] :
        defaultFilter.filters.map(instance => {
            const filter = new MetricFilterState();
            filter.metric = metric;
            filter.name = metric.name;
            filter.invert = instance.invert;
            filter.treatPrimaryValuesAsRange = instance.treatPrimaryValuesAsRange;
            filter.values = instance.values;
            filter.entityInstances = {};
            for(var key of metric.entityCombination) {
                if (Object.keys(instance.entityInstances).length == 0 || instance.entityInstances[key.identifier].some(x=> x == EntityInstance.AllInstancesId)) {
                    filter.entityInstances[key.identifier] = [EntityInstance.AllInstancesId];
                } else {
                    filter.entityInstances[key.identifier] = entityConfuration.getAllEnabledInstancesForTypeOrdered(key).map(x=>x.id);
                }
            }
            filter.entityInstances = instance.entityInstances;
            filter.isAdvanced = true;
            filter.isRange = false;
            return filter;
        });

    return {
        defaultReportFilter: defaultFilter,
        metric: metric,
        filters: resultFilters
    };
}

export function metricValidAsFilter(metric: Metric): boolean {
    return metric.eligibleForCrosstabOrAllVue &&
        !metric.disableFilter &&
        metric.entityCombination.length <= 1 &&
        metric.originalMetricName == undefined &&
        metric.filterValueMapping.every(f => f.text) &&
        metric.filterValueMapping.every(f => f.fullText != "Range") && //TODO: Handle ranges - see https://app.shortcut.com/mig-global/story/53639/what-to-do-for-range-filters
        metric.filterValueMapping.length > 0
}

export function groupMetricFiltersByMeasureName(metricFilters: MetricFilterState[]) : MetricFilterState[][] {
    return Object.values(ArrayHelper.groupBy(metricFilters, filter => filter.metric.name));
}

export function getCompositeFilterModel(curatedFilters: CuratedFilters, metric?: Metric) {
    const isMetricFilterable = !metric || metric.isMetricFilterable();
    return new CompositeFilterModel({
        filterOperator: FilterOperator.And,
        filters: isMetricFilterable ? curatedFilters.measureFilters: [],
        compositeFilters: isMetricFilterable ? curatedFilters.compositeFilters : [],
    });
}

export function getDemographicFilterModel(curatedFilters: CuratedFilters, metric?: Metric){
    if (!metric || metric.isMetricFilterable()) {
        return curatedFilters.demographicFilter;
    }
    return new DemographicFilter();
}

export function applyMetricFiltersToCuratedFilters(curatedFilters: CuratedFilters, filters: MetricFilterState[]) {
    var initialFilter = curatedFilters;

    initialFilter.removeAllMeasureFilters();
    initialFilter.removeAllCompositeFilters();

    const filtersGroupedByMeasure = groupMetricFiltersByMeasureName(filters);

    filtersGroupedByMeasure.forEach(group => {
        initialFilter.addCompositeFilter(buildCompositeFilterForGroup(group));
    });

    return initialFilter;
}

function range (start:number, end:number): number[] { return [...Array(1+end-start)].map(v => start+v) }

function buildCompositeFilterForGroup(filtersForMeasure: MetricFilterState[]): CompositeFilterModel {
    const compositeFilter = new CompositeFilterModel({
        filterOperator: FilterOperator.Or,
        filters: [],
        compositeFilters: [],
    });

    filtersForMeasure.forEach(filter => {
        if (Object.keys(filter.entityInstances).length == 0 || Object.values(filter.entityInstances).some(x => x.some(x => x === EntityInstance.AllInstancesId))) {
            if (Object.values(filter.entityInstances).length != 1) {
                console.error(`Ignoring instances: ${Object.keys(filter.entityInstances).join(', ')}`);
            }
            //
            //If no entity instances are specified copy over the filters ontop of the entity instances
            //
            // We assume that measure that is being used has only 1 entity combination or we are using the first one
            //
            let values = filter.values;

            if (filter.treatPrimaryValuesAsRange) {
                const min = Math.min(...filter.values!);
                const max = Math.max(...filter.values!);
                values = range(min, max + 1);
            }
            const identifier = filter.metric.entityCombination.length ? filter.metric.entityCombination[0].identifier : undefined;
            if (filter.invert) {
                //invert with multiple values needs to be nested inside another composite filter
                //e.g. "!1,2,3" needs to turn into !1 AND !2 AND !3
                const filters = values.map(v => new MeasureFilterRequestModel({
                    measureName: filter.name,
                    entityInstances: identifier ? { [identifier]: [v] } : {},
                    values: [v],
                    invert: filter.invert,
                    treatPrimaryValuesAsRange: false
                }));
                compositeFilter.compositeFilters.push(new CompositeFilterModel({
                    name: filter.name,
                    filterOperator: FilterOperator.And,
                    filters: filters,
                    compositeFilters: [],
                }));
            } else {
                //individual filters for each value in a FilterValueMapping e.g. "1,2,3"
                compositeFilter.filters.push(new MeasureFilterRequestModel({
                    measureName: filter.name,
                    entityInstances: identifier ? { [identifier]: values } : {},
                    values: filter.values,
                    invert: filter.invert,
                    treatPrimaryValuesAsRange: filter.treatPrimaryValuesAsRange,
                }));
            }
        } else {
            compositeFilter.filters.push(new MeasureFilterRequestModel({
                measureName: filter.name,
                entityInstances: filter.entityInstances,
                values: filter.values,
                invert: filter.invert,
                treatPrimaryValuesAsRange: filter.treatPrimaryValuesAsRange,
            }));
        }
    })
    return compositeFilter;
}