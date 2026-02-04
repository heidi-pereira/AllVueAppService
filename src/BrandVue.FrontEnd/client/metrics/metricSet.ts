import { Metric } from "./metric";
import { DataSubsetManager } from "../DataSubsetManager";
import * as BrandVueApi from "../BrandVueApi";
import { IEntityType } from "../BrandVueApi";
import _ from "lodash";
import { PrimaryFieldDependency } from "./PrimaryFieldDependency";

export class MetricSet {
    constructor(overrides?: Partial<MetricSet>) {
        if(overrides) {
            Object.assign(this, overrides)
        }
    }

    public metrics: Metric[];

    public getMetric(metricString: string): Metric | undefined {
        return this.findMetrics(metricString, m => m.name)[0];
    }
    
    public getMetrics(metricsString: string): Metric[] {
        return this.findMetrics(metricsString, m => m.name);
    }
    
    public getMetricByName(metricsString: string[]): Metric[] {
        return this.findMetricsByString(metricsString, m => m.name);
    }

    public getMetricsByUrlSafeName(urlSafeMetricsString: string): Metric[] {
        return this.findMetrics(urlSafeMetricsString, m => m.urlSafeName);
    }
    
    private findMetricsByString(metricStrings: string[], nameToMatch: (m: Metric) => string) {
        return metricStrings
            .reduce((metrics, metricName) => {
                    // Note: Dont use localeCompare(metricName, undefined, { sensitivity: 'base' }) here due to perf issues when opening the menu
                    const metric = this.metrics.find(m => nameToMatch(m).toLocaleLowerCase() === metricName.toLocaleLowerCase());
                    if (metric) {
                        metrics.push(metric);
                    }
                    return metrics;
                },
                [] as Metric[]);
    }
    
    private findMetrics(metricsString: string, nameToMatch: (m: Metric) => string) {
        if (metricsString == null)
            return [];
        return this.findMetricsByString(metricsString
            .split("|"), nameToMatch);
    }

    public getEntityTypes(): IEntityType[] {
        return this.metrics.map(m => m.entityCombination).reduce((a, b) => _.unionBy(a, b, x => x.identifier));
    }

    public load(selectedSubsetId: string) {
        this.metrics = [];
        let metaDataClient = BrandVueApi.Factory.MetricsClient(throwErr => throwErr());
        return metaDataClient.getMetrics(selectedSubsetId).then(ms => {
                this.metrics = MetricSet.mapMeasuresToMetrics(ms, this);
            }
        );
    }

    public static mapMeasuresToMetrics(measures: BrandVueApi.Measure[], parent?: any): Metric[] {
        const measureByName = new Map(measures.map(m => [m.name, m] as [string, BrandVueApi.Measure]));
        return measures.map(data => {
            const m: Metric = new Metric(parent);

            m.name = data.name;
            m.urlSafeName = data.urlSafeName;
            m.primaryFieldDependencies = data.primaryFieldDependencies.map(p => new PrimaryFieldDependency({name: p.name, itemNumber:p.itemNumber}));
            m.fieldOperation = data.fieldOperation;
            m.calcType = data.calculationType;
            m.baseField = data.baseField;
            m.keyImage = "";
            m.measure = data.description;
            m.helpText = data.helpText;
            m.entityCombination = data.entityCombination;
            m.primaryFieldEntityCombination = data.primaryFieldEntityCombination;
            m.defaultSplitByEntityTypeName = data.defaultSplitByEntityTypeName;
            m.setNumFormat(data.numberFormat);
            m.downIsGood = data.downIsGood;
            const baseMeasure = measureByName.get(data.marketAverageBaseMeasure);
            m.averageDescription = baseMeasure && baseMeasure.description;

            if (data.min != null)
                m.min = data.min;
            if (data.max != null)
                m.max = data.max;
            if (data.startDate) {
                m.startDate = data.startDate;
            }
            m.filterValueMapping = this.parseFilterValueMapping(data.filterValueMapping);
            m.filterGroup = data.filterGroup;
            m.filterMulti = data.filterMulti;
            m.subset = DataSubsetManager.parseSupportedSubsets(data.subset);
            m.disableMeasure = data.disableMeasure;
            m.disableFilter = data.disableFilter;
            m.disabled = data.disabled;
            m.eligibleForMetricComparison = data.eligibleForMetricComparison;
            m.eligibleForCrosstabOrAllVue = data.eligibleForCrosstabOrAllVue;
            m.isBasedOnCustomVariable = data.isBasedOnCustomVariable;
            m.hasCustomFieldExpression = data.hasCustomFieldExpression;
            m.isNumericVariable = data.isNumericVariable;
            m.numericVariableField = data.numericVariableField;
            m.isWaveMeasure = data.isWaveMeasure;
            m.isSurveyIdMeasure = data.isSurveyIdMeasure;
            m.baseDescription = data.subsetSpecificBaseDescription;
            m.hasCustomBase = data.hasCustomBase;
            m.varCode = data.varCode;
            m.displayName = data.displayName;
            m.variableConfigurationId = data.variableConfigurationId;
            m.baseVariableConfigurationId = data.baseVariableConfigurationId;
            m.originalMetricName = data.originalMetricName;
            m.generationType = data.generationType;
            m.legacyPrimaryTrueValues = data.legacyPrimaryTrueValues;
            m.questionShownInSurvey = data.questionShownInSurvey;
            m.primaryVariableIdentifier = data.primaryVariableIdentifier;
            m.hasData = data.hasData;
            m.entityInstanceIdMeanCalculationValueMapping = data.entityInstanceIdMeanCalculationValueMapping;

            return m;
        });
    }

    private static parseFilterValueMapping(vm: string): FilterValueMapping[] {
        const results: FilterValueMapping[] = [];
        vm?.split("|").forEach(p => {
            const s = p.split(":");
            if (s[0] !== "")
            results.push(new FilterValueMapping(s[1], s.slice(1).join(":"), s[0].split(",")));
        });
        return results;
    }
}

export class FilterValueMapping {
    constructor(text: string, fullText: string, values: string[]) {
        this.text = text;
        this.fullText = fullText;
        this.values = values;
    }
    text: string;
    fullText: string;
    values: string[];
}