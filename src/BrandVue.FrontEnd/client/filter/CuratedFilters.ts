import * as BrandVueApi from "../BrandVueApi";
import {
    IAverageDescriptor,
    ComparisonPeriodSelection,
    CompositeFilterModel,
    MeasureFilterRequestModel
} from "../BrandVueApi";
import moment from "moment";
import { filterSet } from "./filterSet";
import { IFilterState, MetricFilterState } from "./metricFilterState";
import { CompletePeriod } from "../helpers/CompletePeriod";
import { DateFormattingHelper } from "../helpers/DateFormattingHelper";
import { getNumberOfPeriodsToShow, isCustomPeriodAverage } from "../components/helpers/PeriodHelper";
import { IEntityConfiguration } from "../entity/EntityConfiguration";
import TotalisationPeriodUnit = BrandVueApi.TotalisationPeriodUnit;
import CalculationPeriodSpan = BrandVueApi.CalculationPeriodSpan;
import { ITimeSelectionOptions } from "../state/ITimeSelectionOptions";

interface ICreateOptions {
    endDate: Date;
    startDate?: Date;
    average?: IAverageDescriptor;
    comparisonPeriodSelection?: ComparisonPeriodSelection;
}

// Whenever the state changes, all handlers registered with addChangeHandler are notified
export class CuratedFilters {
    private readonly _demographicFilter: BrandVueApi.DemographicFilter = new BrandVueApi.DemographicFilter({
        ageGroups: [],
        genders: [],
        regions: [],
        socioEconomicGroups: [],
    });
    private readonly _filterDescriptions: { [key: string]: { name: string; filter: string } } = {};
    private readonly _compositeFilters: BrandVueApi.CompositeFilterModel[] = [];
    private _measureFilters: BrandVueApi.MeasureFilterRequestModel[] = [];
    private _startDate: Date;
    private _endDate: Date;
    private _comparisonPeriodSelection: ComparisonPeriodSelection;
    private _average: IAverageDescriptor;
    
    constructor(filterSet: filterSet, entityConfiguration: IEntityConfiguration | null, metricFilters: MetricFilterState[], previous?: CuratedFilters) {
        if (previous != null) {
            this._demographicFilter = previous._demographicFilter;
            this._filterDescriptions = previous._filterDescriptions;
            this._measureFilters = previous._measureFilters;
            this._compositeFilters = previous._compositeFilters;
            this._startDate = previous._startDate;
            this._endDate = previous._endDate;
            this._comparisonPeriodSelection = previous._comparisonPeriodSelection;
            this._average = previous._average;
        } else {
            for (let filter of filterSet.filters) {
                let description: { name: string; filter: string } | undefined = undefined;
                if (filter.initialDescription != null) {
                    description = { name: filter.field, filter: filter.initialDescription };
                }
                if (filter.field === "Age") {
                    const flattenedValue = this.flatten(filter.getDefaultValue().map((s) => s.split(",")));
                    this.demographicFilter.ageGroups = flattenedValue;
                }
                this.update(filter.field, filter.initialValue, description);
            }
            for (let filter of metricFilters) {
                if (filter.isEnabled()) {
                    this.updateMeasureFilter(
                        filter.name,
                        {
                            entityInstances: filter.entityInstances,
                            values: filter.values,
                            invert: filter.invert,
                            treatPrimaryValuesAsRange: filter.treatPrimaryValuesAsRange,
                        },
                        { name: filter.name, filter: filter.description(filter.entityInstances, filter.valueToString(), entityConfiguration) }
                    );
                }
            }
        }
    }

    static createWithOptions = (options: ICreateOptions, entityConfiguration: any): CuratedFilters => {
        const filters = new filterSet();
        filters.filters = [];
        const curatedFilters = new CuratedFilters(filters, entityConfiguration, []);
        if (options.average) {
            curatedFilters.average = options.average;
        }
        curatedFilters.comparisonPeriodSelection = options.comparisonPeriodSelection ?? ComparisonPeriodSelection.CurrentPeriodOnly;
        const startDate = options.startDate ?? options.endDate; // Default to the same startDate as endDate
        curatedFilters.setDates(startDate, options.endDate);
        return curatedFilters;
    };

    get demographicFilter(): BrandVueApi.DemographicFilter {
        return this._demographicFilter;
    }

    get filterDescriptions(): { [key: string]: { name: string; filter: string } } {
        return this._filterDescriptions;
    }

    set comparisonPeriodSelection(value: ComparisonPeriodSelection) {
        if (this._comparisonPeriodSelection != value) {
            this._comparisonPeriodSelection = value;
        }
    }

    get endDate(): Date {
        return this._endDate;
    }

    get startDate(): Date {
        return this._startDate;
    }

    get comparisonPeriodSelection(): ComparisonPeriodSelection {
        return this._comparisonPeriodSelection;
    }

    get average(): IAverageDescriptor {
        return this._average;
    }

    set average(value: IAverageDescriptor) {
        this._average = value;
    }

    get measureFilters(): MeasureFilterRequestModel[] {
        return this._measureFilters;
    }

    set measureFilters(value: MeasureFilterRequestModel[]) {
        this._measureFilters = value;
    }

    get compositeFilters(): CompositeFilterModel[] {
        return this._compositeFilters;
    }

    public setDates(startDate: Date, endDate: Date) {
        if (!this.dateEqual(this._startDate, startDate) || !this.dateEqual(this._endDate, endDate)) {
            this._startDate = startDate;
            this._endDate = endDate;
        }
    }

    public setEndDate(endDate: Date) {
        if (!this.dateEqual(this._endDate, endDate)) {
            this._endDate = endDate;
        }
    }

    public calcScorecardStartDate(scorecardAverage: IAverageDescriptor): Date {
        return this.calcScorecardStartDateFromEndDate(this._endDate, scorecardAverage);
    }

    private calcScorecardStartDateFromEndDate(endDate: Date, scorecardAverage: IAverageDescriptor): Date {
        const numberOfPeriods = getNumberOfPeriodsToShow(scorecardAverage);
        let startDate = endDate;
        for (let i = 1; i < numberOfPeriods; i++) {
            startDate = CompletePeriod.getLastDayInLastCompletePeriod(
                CompletePeriod.getFirstDayInCurrentPeriod(startDate, scorecardAverage.makeUpTo),
                scorecardAverage.makeUpTo
            );
        }
        return moment.utc(startDate).startOf("day").toDate();
    }

    public comparisonDates(
        isScorecard: boolean,
        timeSelection: ITimeSelectionOptions,
        continuousPeriod: boolean = false,
        comparisonPeriodSelection = this._comparisonPeriodSelection,
        
    ): CalculationPeriodSpan[] {
        const comparisonDates: CalculationPeriodSpan[] = [];

        if (isScorecard) {
            const startDate = this.calcScorecardStartDateFromEndDate(this._endDate, timeSelection.scorecardAverage);
            comparisonDates.push(new CalculationPeriodSpan({ startDate: startDate, endDate: this._endDate }));
            return comparisonDates;
        }

        // Custom period average always uses continuous periods
        if (continuousPeriod || isCustomPeriodAverage(this._average)) {
            comparisonDates.push(new CalculationPeriodSpan({ startDate: this._startDate, endDate: this._endDate }));
        } else {
            const unit = this._average.totalisationPeriodUnit === TotalisationPeriodUnit.Month ? "month" : "day";
            switch (comparisonPeriodSelection) {
                case ComparisonPeriodSelection.CurrentAndPreviousPeriod:
                    comparisonDates.push(this.calculatePeriodSpan(unit, this.getOffset(this._average)));
                    break;
                case ComparisonPeriodSelection.SameLastYear:
                    comparisonDates.push(this.calculatePeriodSpan("year", 1));
                    break;
                case ComparisonPeriodSelection.LastSixMonths:
                    comparisonDates.push(this.calculatePeriodSpan("month", 6));
                    break;
            }
            comparisonDates.push(this.calculatePeriodSpan(unit, 0));
        }

        return comparisonDates;
    }

    // Number of periods in average is not an indicator of the offset required for a date period span. We need to treat fixed and rolling period differently
    private getOffset(averageDescriptor: IAverageDescriptor): number {
        if (averageDescriptor.weightAcross === BrandVueApi.WeightAcross.SinglePeriod) {
            // Are we looking at a fixed period?
            switch (averageDescriptor.makeUpTo) {
                case BrandVueApi.MakeUpTo.WeekEnd:
                    return 7;
                case BrandVueApi.MakeUpTo.MonthEnd:
                    return 1;
                case BrandVueApi.MakeUpTo.QuarterEnd:
                    return 3;
                case BrandVueApi.MakeUpTo.HalfYearEnd:
                    return 6;
                case BrandVueApi.MakeUpTo.CalendarYearEnd:
                    return 12;
                default:
                    return 1;
            }
        }

        return averageDescriptor.numberOfPeriodsInAverage;
    }

    private calculatePeriodSpan(unit: "day" | "month" | "year", offset: number): CalculationPeriodSpan {
        const averageUnit = this._average.totalisationPeriodUnit === TotalisationPeriodUnit.Month ? "month" : "day";

        const endDate = moment.utc(this._endDate).subtract(offset, unit).endOf(averageUnit).startOf("day").toDate();

        return new CalculationPeriodSpan({ startDate: endDate, endDate: endDate, name: DateFormattingHelper.formatDateRange(endDate, this._average) });
    }

    private flatten<T>(arr: T[][]) {
        return arr.reduce((first, second) => first.concat(second), []);
    }

    public initializeMeasureFilters(metricFilters: MetricFilterState[], entityConfiguration) {
        for (let filter of metricFilters) {
            if (filter.isEnabled()) {
                this.updateMeasureFilter(
                    filter.name,
                    {
                        entityInstances: filter.entityInstances,
                        values: filter.values,
                        invert: filter.invert,
                        treatPrimaryValuesAsRange: filter.treatPrimaryValuesAsRange,
                    },
                    { name: filter.name, filter: filter.description(filter.entityInstances, filter.valueToString(), entityConfiguration) }
                );
            } else {
                this.removeMeasureFilter(filter.name);
            }
        }
    }

    public updateMeasureFilter(measureName: string, filterState: IFilterState, description: { name: string; filter: string } | undefined) {
        const measureFilter = this._measureFilters.find((f) => f.measureName === measureName);
        if (measureFilter) {
            measureFilter.entityInstances = filterState.entityInstances;
            measureFilter.values = filterState.values;
            measureFilter.invert = filterState.invert;
            measureFilter.treatPrimaryValuesAsRange = filterState.treatPrimaryValuesAsRange;
        } else {
            this._measureFilters.push(
                new BrandVueApi.MeasureFilterRequestModel({
                    measureName: measureName,
                    entityInstances: filterState.entityInstances,
                    values: filterState.values!,
                    invert: filterState.invert,
                    treatPrimaryValuesAsRange: filterState.treatPrimaryValuesAsRange,
                })
            );
        }
        this.updateDescription(measureName, description);
    }

    public addCompositeFilter(compositeFilter: CompositeFilterModel) {
        this._compositeFilters.push(compositeFilter);
    }

    public removeAllCompositeFilters() {
        this._compositeFilters.splice(0, this._compositeFilters.length);
    }

    private updateDescription(key: string, description?: { name: string; filter: string } | undefined) {
        if (description !== undefined) {
            const existing = this._filterDescriptions[key];
            if (!existing || existing.name !== description.name || existing.filter !== description.filter) {
                this._filterDescriptions[key] = description;
            }
        } else {
            delete this._filterDescriptions[key];
        }
    }

    public removeMeasureFilter(measureName: string) {
        const measureFilterIndex = this._measureFilters.findIndex((f) => f.measureName === measureName || f.measureName === "f" + measureName);
        if (measureFilterIndex >= 0) {
            this._measureFilters.splice(measureFilterIndex, 1);
            this.updateDescription(measureName, undefined);
        }
    }

    public removeAllMeasureFilters() {
        this._measureFilters.splice(0, this._measureFilters.length);
        for (var key in this._filterDescriptions) {
            delete this._filterDescriptions[key];
        }
    }

    public update(fieldName: string, value: string | string[] | number | undefined, description: { name: string; filter: string } | undefined) {
        let flattenedValue: string[] = [];
        if (value != undefined) {
            flattenedValue = this.flatten((!Array.isArray(value) ? [value.toString()] : value).map((s) => s.split(",")));
        }

        switch (fieldName) {
            case "Gender":
                this.demographicFilter.genders = flattenedValue;
                break;
            case "Region":
                this.demographicFilter.regions = flattenedValue;
                break;
            case "Seg":
                this.demographicFilter.socioEconomicGroups = flattenedValue;
                break;
            default:
                console.log("Ignore filter= " + fieldName);
                return;
        }
        this.updateDescription(fieldName, description);
    }

    private dateEqual(x: Date, y: Date): boolean {
        if (!x && !y) return true;
        if (!x || !y) return false;
        return x.getTime() === y.getTime();
    }
}
