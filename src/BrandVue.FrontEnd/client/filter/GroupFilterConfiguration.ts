import { EntityInstance } from "../entity/EntityInstance";
import { Metric } from "../metrics/metric";
import { IFilterState } from "./metricFilterState";

export class GroupFilterConfiguration {
    public metric: Metric;
    public name: string;
    public state: IFilterState;
    public isAdvanced: boolean;
    public isRange: boolean;
    
    public isEnabled() { return this.state.values.length > 0}
    
    public value(): string {
        let result = "";
        const valuesToUse = this.state.values == null ? [] : this.state.values;
        if (this.state.treatPrimaryValuesAsRange || this.state.values == null) {
            result = valuesToUse.join("-");
        } else {
            result = valuesToUse.join(",");
        }
        if (this.state.invert) {
            result = "!" + result;
        }
        return result;
    }
    
    private getRangeFilterDescription(): string {
        let filterText: string;

        if (this.state.values[0] < 0) {
            filterText = "<=" + this.state.values[1];
        }
        else if (this.state.values[1] < 0) {
            filterText = ">=" + this.state.values[0];
        }
        else if (this.state.values[0] === this.state.values[1]) {
            filterText = "" + this.state.values[0];
        }
        else {
            filterText = this.state.values[0] + "-" + this.state.values[1];
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
                    inGroup.push(filterMapping.text);
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
                currentGroup = filterMapping.text;
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
        const filterText = filterItem ? filterItem.text : "";
        return filterText;
    }

    public description(entity: EntityInstance | null, value: string): string {
        if (!this.isEnabled()) {
            return "";
        }
        
        let filterText = "";

        if (this.isRange) {
            filterText = this.getRangeFilterDescription();
        } else {
            if (this.metric.filterMulti) {
                filterText = this.getMultiFilterDescription(value);
            } else {
                filterText = this.getSimpleFilterDescription(value);
            }
        }

        return filterText + (entity == null ? "" : "(" + entity!.name + ")");
    }

    private static isFilterARange(valuesString: string): boolean {
        const rangeFormatRegex = new RegExp(/-?[0-9]+-{1,2}[0-9]+/gm);
        return rangeFormatRegex.test(valuesString);
    }

    static getFilterRangeValues(valuesString: string): [number, number] | undefined {
        let values: [number, number] | undefined;
        if (this.isFilterARange(valuesString)) {
            const indexOfDigitThenHyphen = valuesString.search(/[0-9]-/gm);
            const firstNumber = +valuesString.slice(0, indexOfDigitThenHyphen + 1);
            const secondNumber = +valuesString.slice(indexOfDigitThenHyphen + 2, valuesString.length);
            values = [firstNumber, secondNumber];
        }
        return values;
    }
}