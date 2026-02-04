import * as BrandVueApi from "./BrandVueApi";
import {Metric} from "./metrics/metric";

const MAX_SAFE_INTEGER = 2147483647;

export class DataSubsetManager {

    private static _subsets: BrandVueApi.Subset[] = [];
    private static _subsetsById: { [id: string]: BrandVueApi.Subset } = {};
    public static selectedSubset: BrandVueApi.Subset;
    public static selectedParentGroup: string | undefined;

    public static Initialize(subsets: BrandVueApi.Subset[], defaultSubsetId: string) {
        DataSubsetManager._subsets = subsets || [];

        DataSubsetManager._subsets.sort(
            (subsetA, subsetB) =>
            (subsetA.order ?? MAX_SAFE_INTEGER) -
            (subsetB.order ?? MAX_SAFE_INTEGER));

        DataSubsetManager._subsetsById = {};
        for (let subset of DataSubsetManager._subsets) {
            DataSubsetManager._subsetsById[subset.id.toLowerCase()] = subset;
        }
        DataSubsetManager.setSelectedSubsetById(defaultSubsetId);
    }

    public static setSelectedParentGroup(parentGroup: string | undefined) {
        this.selectedParentGroup = parentGroup;
    }

    private static setSelectedSubsetById(selectedSubsetId: string) {
        let selectedSubset = DataSubsetManager.getById(selectedSubsetId);
        if (!selectedSubset || selectedSubset.disabled) {
            selectedSubset = DataSubsetManager.getAll().filter(s => !s.disabled)[0];
        }
        DataSubsetManager.selectedSubset = selectedSubset;
        DataSubsetManager.selectedParentGroup = selectedSubset.parentGroupName;
    }

    public static getById(id: string): BrandVueApi.Subset | undefined {
        return id ? DataSubsetManager._subsetsById[id.toLowerCase()] : undefined;
    }

    public static getByIso2LetterCountryCode(countryCode: string): BrandVueApi.Subset[] {
        countryCode = countryCode.toLowerCase();
        const result: BrandVueApi.Subset[] = [];
        DataSubsetManager._subsets.forEach(subset => {
            if (subset.iso2LetterCountryCode.toLowerCase() === countryCode) {
                result.push(subset);
            }
        });
        return result;
    }

    public static getAll(): BrandVueApi.Subset[] {
        return DataSubsetManager._subsets;
    }

    public static getAllParentGroups(): (string | undefined)[] {
        // Get all distinct parent groups while preserving the subset order
        const parentGroups = DataSubsetManager._subsets.map(subset => subset.parentGroupName);
        return parentGroups.filter((item, index) => parentGroups.indexOf(item) === index);
    }

    public static getAllByParentGroup(parentGroup: string | undefined): BrandVueApi.Subset[] {
        return DataSubsetManager._subsets.filter(s => s.parentGroupName === parentGroup);
    }

    public static parseSupportedSubsets(rawSubsetSpecification: any): BrandVueApi.Subset[] {
        const parsedSubset: BrandVueApi.Subset[] = [];

        if (!rawSubsetSpecification) {
            return parsedSubset;
        }

        if (!rawSubsetSpecification.split
            && (rawSubsetSpecification.length || rawSubsetSpecification.length === 0)) {
            //  We've already been given an array of subsets.
            return rawSubsetSpecification;
        }

        const splits = rawSubsetSpecification.split("|");
        if (!splits || splits.length === 0) {
            return parsedSubset;
        }

        splits.forEach(split => {
            const match = DataSubsetManager.getById(split);
            if (match) {
                parsedSubset.push(match);
            }
        });

        return parsedSubset;
    }

    public static supportsDataSubset(
        subset: BrandVueApi.Subset,
        supportedSubsets: BrandVueApi.Subset[]
    ): boolean {
        if (!supportedSubsets || supportedSubsets.length === 0) {
            return true;
        }

        var lowercaseId = subset.id.toLowerCase();
        return !!supportedSubsets.find(supported => supported.id.toLowerCase() === lowercaseId);
    }

    public static filterMetricByCurrentSubset(metrics: Metric[]): Metric[] {
        return metrics.filter(metric =>
            metric && !metric.disableMeasure && DataSubsetManager.supportsDataSubset(DataSubsetManager.selectedSubset,metric.subset));
    }
}