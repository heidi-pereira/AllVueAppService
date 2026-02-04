import { Metric } from "../metrics/metric";
import {IEntityConfiguration} from "../entity/EntityConfiguration";
import { EntityInstance } from "../entity/EntityInstance";
import { throwIfNullish } from "../components/helpers/ThrowHelper";

export const dashSeparatorRegex = /-(?=-?\d)/;
const cSharpMinValue = -(2 ** 31);
const cSharpMaxValue = (2 ** 31) - 1;
export interface IFilterState {
    entityInstances: {[i: string]: number[]};
    values: number[];
    invert: boolean;
    treatPrimaryValuesAsRange: boolean;
}

export interface IFilterStateCondensed {
    e: {[entityType: string]: number[]};
    v: number[];
    i: boolean;
    r: boolean;
}

export class MetricFilterState implements IFilterState {
    public metric: Metric;
    public name: string;
    public entityInstances: { [entityType: string]: number[] };
    //get brands: 
    public values: number[];
    public invert: boolean;
    public treatPrimaryValuesAsRange: boolean;
    public isAdvanced: boolean;
    public isRange: boolean;

    public isEnabled() {
        return this.values?.length > 0;
    }

    constructor(prev?: MetricFilterState, next?: IFilterState) {
        if (prev) {
            this.metric = prev.metric;
            this.name = prev.name;
            if (next) {
                this.entityInstances = next!.entityInstances ?? prev.entityInstances;
                this.values = next!.values ?? prev.values;
                this.invert = next!.invert ?? prev.invert;
                this.treatPrimaryValuesAsRange = next!.treatPrimaryValuesAsRange ?? prev.treatPrimaryValuesAsRange;
            } else {
                this.entityInstances = prev.entityInstances;
                this.values = prev.values;
                this.invert = prev.invert;
                this.treatPrimaryValuesAsRange = prev.treatPrimaryValuesAsRange;
            }
            this.isAdvanced = prev.isAdvanced;
            this.isRange = prev.isRange;
        }
    }

    public valueToString(): string {
        let result: string;
        const valuesToUse = this.values == undefined ? [cSharpMinValue, cSharpMaxValue] : this.values;
        if (this.treatPrimaryValuesAsRange || this.values == undefined) {
            result = valuesToUse.join("-");
        } else {
            result = valuesToUse.join(",");
        }
        if (this.invert) {
            result = "!" + result;
        }
        return result;
    }

    public static getArrayValueArgumentsFromString(valueString: string, isRange: boolean): { values: number[], treatPrimaryValuesAsRange: boolean, invert: boolean } {
        const invert = valueString.startsWith("!");
        const valuesString = invert ? valueString.substring(1) : valueString;

        let values = MetricFilterState.splitCommaSeparatedStringToNumberArray(valuesString);

        let treatPrimaryValuesAsRange = isRange;

        //Attempt to extract range if it is of that format
        const filterRangeValues = MetricFilterState.getFilterRangeValues(valuesString);
        if (filterRangeValues) {
            values = filterRangeValues ?? values;
            treatPrimaryValuesAsRange = true;
        }
        return { values, treatPrimaryValuesAsRange, invert }
    }
    
    private static splitCommaSeparatedStringToNumberArray(stringToSplit: string) {
        return stringToSplit
            .split(",") // Split the string by commas
            .map(x => x.trim()) // Remove any leading or trailing whitespace
            .filter(x => x !== "") // Remove any empty strings
            .map(x => Number(x)) // Convert each string to a number
            .filter(x => !isNaN(x)); // Remove any NaN values
    }

    private getRangeFilterDescription(): string {
        let filterText: string = "";
        if (this.values) {
            if (this.values[0] < 0) {
                filterText = "<=" + this.values[1];
            } else if (this.values[1] < 0) {
                filterText = ">=" + this.values[0];
            } else if (this.values[0] === this.values[1]) {
                filterText = "" + this.values[0];
            } else {
                filterText = this.values[0] + "-" + this.values[1];
            }
        }
        return filterText;
    }

    private getMultiFilterDescription(value: string): string {
        let filterText: string = "";

        var values = value.split(',');

        let currentGroup = "";
        let groupCount: number;
        let inGroup: any[] = [];

        this.metric.filterValueMapping.forEach((filterMapping, filterMappingIndex) => {
            if (filterMapping.values[0] !== "*") {
                if (filterMapping.values.every(x => values.some(y => x === y))) {
                    inGroup.push(filterMapping.fullText);
                }
                groupCount++;
            }

            if ((filterMappingIndex === this.metric.filterValueMapping.length - 1
                || filterMapping.values[0] === "*") && inGroup.length) {

                if (filterText.length) {
                    filterText += ",";
                }
                if (inGroup.length === groupCount) {
                    filterText += currentGroup;
                } else {
                    filterText += inGroup.join(',');
                }
            }

            if (filterMapping.values[0] === "*") {
                currentGroup = filterMapping.fullText;
                inGroup = [];
                groupCount = 0;
            }

        });

        return filterText;
    }

    private getSimpleFilterDescription(value: string): string {
        const filterItem = this.metric
            .filterValueMapping
            .find(f => f.values.join(",") === value);
        const filterText = filterItem ? filterItem.fullText : "";
        return filterText;
    }

    public filterDescription(entityConfiguration: IEntityConfiguration | null): string {
        if (!this.isEnabled()) {
            return "";
        }
        let filterText: string;
        const entityInstances = this.entityInstances;
        const value = this.valueToString()
        if (this.isRange) {
            filterText = this.getRangeFilterDescription();
        } else {
            if (this.metric.filterMulti) {
                filterText = this.getMultiFilterDescription(value);
            } else {
                filterText = this.getSimpleFilterDescription(value);
            }
        }
        const lengthOfKeys = Object.keys(entityInstances).length;
        if (lengthOfKeys) {
            const manyKeys = lengthOfKeys > 1;
            if (manyKeys) {
                filterText += this.getInstanceDescription(entityConfiguration!)
            }
            else {
                filterText = "";
                for (var key of Object.keys(entityInstances)) {
                    entityConfiguration!.getEnabledInstancesById(key, entityInstances[key]).forEach((instance, index) => {
                        filterText += instance.name;
                        if (index < entityInstances[key].length - 1) {
                            filterText += ", ";
                        }
                    });
                }
            }
        }
        return filterText;
    }

    public description(entityInstances: { [index: string]: number[] }, value: string, entityConfiguration: IEntityConfiguration | null): string {
        if (!this.isEnabled()) {
            return "";
        }
        let filterText: string;
        if (this.isRange) {
            filterText = this.getRangeFilterDescription();
        } else {
            if (this.metric.filterMulti) {
                filterText = this.getMultiFilterDescription(value);
            } else {
                filterText = this.getSimpleFilterDescription(value);
            }
        }
        return filterText + (Object.keys(entityInstances).length ? this.getInstanceDescription(entityConfiguration!) : "");
    }

    private static isFilterARange(valuesString: string): boolean {
        const rangeFormatRegex = new RegExp(/-?[0-9]+-{1,2}[0-9]+/gm);
        return rangeFormatRegex.test(valuesString);
    }

    static getFilterRangeValues(valuesString: string): [number, number] | undefined {
        let values: [number, number] | undefined;
        if (MetricFilterState.isFilterARange(valuesString)) {
            const indexOfDigitThenHyphen = valuesString.search(/[0-9]-/gm);
            const firstNumber = +valuesString.slice(0, indexOfDigitThenHyphen + 1);
            const secondNumber = +valuesString.slice(indexOfDigitThenHyphen + 2, valuesString.length);
            values = [firstNumber, secondNumber];
        }
        return values;
    }

    private getInstanceDescription(entityConfiguration: IEntityConfiguration): string {
        const entityInstances = this.entityInstances;
        let filterText = " for ";
        for (var key of Object.keys(entityInstances)) {
            const instances = entityInstances[key]
            const entityType = throwIfNullish(entityConfiguration.getEntityType(key), `Entity type ${key}`);
            if (filterText.length > 5) {
                filterText += ", ";
            }
            if (instances.some(x => x == EntityInstance.AllInstancesId)) {
                filterText += "all " + entityType.displayNamePlural;
            } else {
                if (instances.length) {
                    filterText += entityType.displayNameSingular + " (";
                    entityConfiguration.getEnabledInstancesById(key, entityInstances[key]).forEach((instance, index) => {
                        filterText += instance.name;
                        if (index < entityInstances[key].length - 1) {
                            filterText += ", ";
                        }
                    });
                    filterText += ")";
                }
            }
        }
        return filterText;
    }

    public withCleared(): MetricFilterState {
        return new MetricFilterState({...this, values: [], entityInstances: {} });
    }

    public withInstance(entityInstanceType: string, entityInstanceId: string): MetricFilterState {
        return this.withInstances(entityInstanceType, [parseInt(entityInstanceId)], false);
    }

    public withInstances(entityInstanceType: string, entityInstanceIds: number[], invert: boolean): MetricFilterState {
        const entityInstances = { ...this.entityInstances, [entityInstanceType]: [...entityInstanceIds] };
        return new MetricFilterState({ ...this, entityInstances, invert: invert });
    }

    public withRange(min: number, max: number): MetricFilterState {
        return this.withValues(min + '-' + max, false);
    }

    /**
     * The values are used as the arbiter of whether the filter is enabled
     * We also need to send something valid to the backend that won't filter anything out in general
     */
    public withConstantValues(): MetricFilterState {
        const filterValueMapping = this.metric.filterValueMapping;
        if (filterValueMapping.length == 1) {
            const values = filterValueMapping.flatMap(f => f.values).join(',');
            return this.withValues(values, this.invert);
        }
        
        const allHaveInstances = Object.values(this.entityInstances).every(e => e.length > 0);
        return this.withValues(
            // In most cases, setting this to the full range of integers would be fine.
            // But when there's exactly one non-brand entity (legacyEntityMapping case), it needs to match up so the OptionSelector will be checked
            // Since these are entity set mappings we must force the outgoing values to respect the inversion
            allHaveInstances ? Object.values(this.entityInstances).flat().join(',') : ""
            , this.invert
        );
    }

    public withValues(values: string, forceInvert: boolean = false): MetricFilterState {
        return new MetricFilterState({
            ...this, ...MetricFilterState.getArrayValueArgumentsFromString((forceInvert ? "!" : "") + values, this.isRange)
        });
    }
}


