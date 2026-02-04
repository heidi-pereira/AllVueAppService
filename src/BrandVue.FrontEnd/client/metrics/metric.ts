import {NumberFormattingHelper as dashFormat} from "../helpers/NumberFormattingHelper";
import {FilterValueMapping as MetricFilter} from "./metricSet";
import * as BrandVueApi from "../BrandVueApi";
import {AutoGenerationType, Measure, AllowedValues} from "../BrandVueApi";
import {DataSubsetManager} from "../DataSubsetManager";
import CalculationType = BrandVueApi.CalculationType;
import ResponseEntityType = BrandVueApi.IEntityType;
import ResponseFieldDescriptor = BrandVueApi.ResponseFieldDescriptor;
import FieldOperation = BrandVueApi.FieldOperation;
import { PrimaryFieldDependency } from "./PrimaryFieldDependency";

export class Metric {
    public parent: any;
    public name: string;
    public urlSafeName: string;
    public primaryFieldDependencies: PrimaryFieldDependency[];
    public fieldOperation: FieldOperation;
    public baseField: ResponseFieldDescriptor;
    public calcType: CalculationType;
    public legacyPrimaryTrueValues: AllowedValues;
    public keyImage: string;
    public measure: string;
    public helpText: string;
    public numFormat: string;
    public min: number;
    public max: number;
    public format: string;
    public startDate: Date;
    public filterValueMapping: MetricFilter[];
    public filterGroup: string;
    public filterMulti: boolean;
    public downIsGood: boolean;
    public subset: BrandVueApi.Subset[];
    public disableMeasure: boolean;
    public disableFilter: boolean;
    public disabled: boolean;
    public eligibleForMetricComparison: boolean;
    public eligibleForCrosstabOrAllVue: boolean;
    public entityCombination: ResponseEntityType[];
    public primaryFieldEntityCombination: ResponseEntityType[];
    public defaultSplitByEntityTypeName: string;

    public fmt: (val: any) => string; //format function
    public longFmt: (val: any) => string; //Slightly longer format function for displaying y axis on graph labels for example
    public extraLongFmt: (val: any) => string; //Even longer to display on graphs when have small regions....
    public axisFmt: (val: any) => string; //AxisFormatting....
    public axisLongFmt: (val: any) => string; //AxisFormatting....
    public axisExtraLongFmt: (val: any) => string; //AxisFormatting....
    public deltaFmt: (val: any) => string; //format for displaying deltas
    public graphAxisTitle: string;
    public averageDescription: string | undefined;
    public isBasedOnCustomVariable: boolean;
    public hasCustomFieldExpression: boolean;
    public isNumericVariable: boolean;
    public numericVariableField: ResponseFieldDescriptor | undefined;
    public isWaveMeasure: boolean;
    public isSurveyIdMeasure: boolean;
    public baseDescription: string;
    public hasCustomBase: boolean;
    public displayName: string;
    public varCode: string;
    public variableConfigurationId: number | undefined;
    public baseVariableConfigurationId: number | undefined;
    public originalMetricName: string | undefined;
    public generationType: AutoGenerationType;
    public questionShownInSurvey: boolean;
    public entityInstanceIdMeanCalculationValueMapping?: BrandVueApi.EntityMeanMap;
    public primaryVariableIdentifier: string;
    public hasData: boolean;

    constructor(ms: any, overrides?:Partial<Metric>) {
        this.parent = ms;
        if(overrides) {
            Object.assign(this, overrides)
        }
    }

    public static createFromApi(measure: Measure): Metric {
        const metric = new Metric(undefined);
        metric.name = measure.name;
        metric.urlSafeName = measure.urlSafeName;
        metric.primaryFieldDependencies = measure.primaryFieldDependencies.map(p => new PrimaryFieldDependency({name: p.name, itemNumber: p.itemNumber}));
        metric.fieldOperation = measure.fieldOperation;
        metric.calcType = measure.calculationType;
        metric.baseField = measure.baseField;
        metric.keyImage = "";
        metric.measure = measure.description;
        metric.helpText = measure.helpText;
        metric.entityCombination = measure.entityCombination;
        metric.primaryFieldEntityCombination = measure.primaryFieldEntityCombination;
        metric.defaultSplitByEntityTypeName = measure.defaultSplitByEntityTypeName;
        metric.setNumFormat(measure.numberFormat);
        metric.downIsGood = measure.downIsGood;
        metric.primaryVariableIdentifier = measure.primaryVariableIdentifier;

        if (measure.min != null)
            metric.min = measure.min;
        if (measure.max != null)
            metric.max = measure.max;
        if (measure.startDate) {
            metric.startDate = measure.startDate;
        }

        metric.filterGroup = measure.filterGroup;
        metric.filterMulti = measure.filterMulti;
        metric.subset = DataSubsetManager.parseSupportedSubsets(measure.subset);
        metric.disableMeasure = measure.disableMeasure;
        metric.disableFilter = measure.disableFilter;
        metric.disabled = measure.disabled;
        metric.eligibleForMetricComparison = measure.eligibleForMetricComparison;
        metric.eligibleForCrosstabOrAllVue = measure.eligibleForCrosstabOrAllVue;
        metric.isBasedOnCustomVariable = measure.isBasedOnCustomVariable;
        metric.hasCustomFieldExpression = measure.hasCustomFieldExpression;
        metric.isNumericVariable = measure.isNumericVariable;
        metric.numericVariableField = measure.numericVariableField;
        metric.isWaveMeasure = measure.isWaveMeasure;
        metric.isSurveyIdMeasure = measure.isSurveyIdMeasure;
        metric.baseDescription = measure.subsetSpecificBaseDescription;
        metric.hasCustomBase = measure.hasCustomBase;
        metric.varCode = measure.varCode;
        metric.displayName = measure.displayName;
        metric.variableConfigurationId = measure.variableConfigurationId;
        metric.baseVariableConfigurationId = measure.baseVariableConfigurationId;
        metric.originalMetricName = measure.originalMetricName;
        metric.generationType = measure.generationType;
        metric.legacyPrimaryTrueValues = measure.legacyPrimaryTrueValues;
        metric.questionShownInSurvey = measure.questionShownInSurvey;
        metric.hasData = measure.hasData;
        metric.entityInstanceIdMeanCalculationValueMapping = measure.entityInstanceIdMeanCalculationValueMapping;

        return metric;
    }

    public isAutoGeneratedNumeric(): boolean {
        return this.generationType == AutoGenerationType.CreatedFromNumeric
    }

    public getSignificanceValue(): string {
        return this.calcType === CalculationType.NetPromoterScore ? "shift of 1" : "95% confidence";
    }

    public isPercentage(): boolean {
        return this.numFormat === "0%";
    }

    public getNumberFormatForAxis(maxNumber: number) {
        if (this.isPercentage()) {
            maxNumber *= 100;
        }

        if (maxNumber < 3) {
            return this.axisExtraLongFmt;
        }
        if (maxNumber < 30) {
            return this.axisLongFmt;
        }
        return this.axisFmt;
    }

    public yAxisTitle(qualifier?: string): string {
        const axisQualifier = this.graphAxisTitle ? this.graphAxisTitle : qualifier;
        if (axisQualifier) {
            return `${(this.varCode)} (${axisQualifier})`;
        }
        return this.varCode;
    }

    public isProfileMetric(): boolean {
        return this.entityCombination.length == 0;
    }

    public isBrandMetric(): boolean {
        return this.entityCombination.length == 1 && this.entityCombination[0].isBrand;
    }

    public isMetricFilterable(): boolean {
        return !(this.calcType === CalculationType.EoTotalSpendPerTimeOfDay || this.calcType === CalculationType.EoTotalSpendPerLocation );
    }

    public setNumFormat(f: string) {
        this.numFormat = f;
        this.graphAxisTitle = "";

        // //handle custom number format overrides
        if (f && f.includes("currencyAffix")) {
            //Format is "currencyAffix:xx" - we want everything after the ":"
            const affix = f.includes(":") ? f.split(":")[1] : "";

            const formatFunction = f.split(":")[0].endsWith("AutoDp") ?
                this.fmt = (val: any) => dashFormat.formatCurrencyWithAffixAutoDp(val, affix) :
                this.fmt = (val: any) => dashFormat.formatCurrencyWithAffix(val, affix);

            this.fmt = this.deltaFmt = this.axisFmt = this.longFmt = this.extraLongFmt = this.axisLongFmt = this.axisExtraLongFmt = formatFunction;
            return;
        }

        switch (f) {
            case "time_minutes":
                this.fmt = dashFormat.format0Dp;
                this.deltaFmt = dashFormat.format0Dp;
                this.longFmt = dashFormat.format1Dp;
                this.extraLongFmt = dashFormat.format2Dp;
                this.axisFmt = this.fmt;
                this.axisLongFmt = dashFormat.formatAxis1Dp;
                this.axisExtraLongFmt = dashFormat.formatAxis2Dp;
                break;
            case "currency":
                this.fmt = dashFormat.formatCurrency;
                this.deltaFmt = dashFormat.formatCurrency;
                this.longFmt = dashFormat.formatCurrencyLong;
                this.extraLongFmt = dashFormat.formatCurrencyLong;
                this.axisFmt = this.fmt 
                this.axisLongFmt = this.longFmt;
                this.axisExtraLongFmt = this.extraLongFmt;
                break;
            case "0%":
                this.fmt = dashFormat.formatPercentage0Dp;
                this.deltaFmt = dashFormat.formatPercentage0DpWithSign;
                this.longFmt = dashFormat.formatPercentage1Dp;
                this.extraLongFmt = dashFormat.formatPercentage2Dp;
                this.axisFmt = dashFormat.formatAxisPercentage1Dp;
                this.axisLongFmt = dashFormat.formatAxisPercentage2Dp;
                this.axisExtraLongFmt = dashFormat.formatAxisPercentage2Dp;
                this.graphAxisTitle = "% of base";
                break;
            case "+0;-0;0":
                this.fmt = dashFormat.formatNps0Dp;
                this.deltaFmt = dashFormat.formatNps0Dp;
                this.longFmt = dashFormat.formatNps1Dp;
                this.extraLongFmt = dashFormat.formatNps2Dp;
                this.axisFmt = this.fmt;
                this.axisLongFmt = dashFormat.formatAxisNps1Dp;
                this.axisExtraLongFmt = dashFormat.formatAxisNps2Dp;

                break;
            case "0;-0;0":
                this.fmt = dashFormat.format0Dp;
                this.deltaFmt = dashFormat.format0Dp;
                this.longFmt = dashFormat.format1Dp;
                this.extraLongFmt = dashFormat.format2Dp;
                this.axisFmt = this.fmt;
                this.axisLongFmt = dashFormat.formatAxis1Dp;
                this.axisExtraLongFmt = dashFormat.formatAxis2Dp;

                break;
            case "+0.0;-0.0;0.0":
                this.fmt = dashFormat.formatNps1Dp;
                this.deltaFmt = dashFormat.formatNps1Dp;
                this.longFmt = dashFormat.formatNps1Dp;
                this.extraLongFmt = dashFormat.formatNps2Dp;
                this.axisFmt = dashFormat.formatAxisNps1Dp;
                this.axisLongFmt = dashFormat.formatAxisNps1Dp;
                this.axisExtraLongFmt = dashFormat.formatAxisNps2Dp;

                break;
            case "0.0;-0.0;0.0":
                this.fmt = dashFormat.format1Dp;
                this.deltaFmt = dashFormat.format1Dp;
                this.longFmt = dashFormat.format2Dp;
                this.extraLongFmt = dashFormat.format2Dp;
                this.axisFmt = dashFormat.formatAxis1Dp;
                this.axisLongFmt = dashFormat.formatAxis2Dp;
                this.axisExtraLongFmt = dashFormat.formatAxis2Dp;
                break;

            case "ukDressSizeHack":
                this.fmt = dashFormat.formatUkDressSize1Dp;
                this.deltaFmt = dashFormat.formatUkDressSize1Dp;
                this.longFmt = dashFormat.formatUkDressSize1Dp;
                this.extraLongFmt = dashFormat.formatUkDressSize1Dp;
                this.axisFmt = this.fmt;
                this.axisLongFmt = this.longFmt;
                this.axisExtraLongFmt = this.extraLongFmt;
                break;

            case "usDressSizeHack":
                this.fmt = dashFormat.formatUsDressSize1Dp;
                this.deltaFmt = dashFormat.formatUsDressSize1Dp;
                this.longFmt = dashFormat.formatUsDressSize1Dp;
                this.extraLongFmt = dashFormat.formatUsDressSize1Dp;
                this.axisFmt = this.fmt;
                this.axisLongFmt = this.longFmt;
                this.axisExtraLongFmt = this.extraLongFmt;
                break;

            default:
                this.numFormat = "0%";
                this.fmt = dashFormat.formatPercentage0Dp;
                this.deltaFmt = dashFormat.formatPercentage0DpWithSign;
                this.longFmt = dashFormat.formatPercentage1Dp;
                this.extraLongFmt = dashFormat.formatPercentage2Dp;
                this.axisFmt = this.fmt;
                this.axisLongFmt = dashFormat.formatAxisPercentage1Dp;
                this.axisExtraLongFmt = dashFormat.formatAxisPercentage2Dp;
                break;
        }
    }
}