import { AverageType, CrosstabAverageResults, MainQuestionType, OverTimeAverageResults, VariableConfigurationModel } from "../../BrandVueApi"
import { Metric } from "../../metrics/metric";
import { GetUnderlyingMetric } from "./Variables/VariableModal/Utils/VariableComponentHelpers";

export const getAverageDisplayText = (averageType: AverageType) => {
    return `Average (${isTypeOfMean(averageType) ? "mean" : averageType.toString().toLowerCase()})`
}

export const OrderedAverageTypes = [AverageType.Median, AverageType.Mean, AverageType.Mentions];

export function SortAverages(a: AverageType, b: AverageType): number {
    return OrderedAverageTypes.indexOf(a) - OrderedAverageTypes.indexOf(b);
}

export const getVerifiedAverageType = (
    average: AverageType, 
    metric: Metric, 
    questionTypeLookup: { [key: string]: MainQuestionType; },
    isAllVue: boolean,
    allMetrics: Metric[],
    variables: VariableConfigurationModel[]) => {

    let verifiedAverageType = average;
    const metricToCheck = GetUnderlyingMetric(metric, allMetrics, variables) ?? metric;

    if (isTypeOfMean(average) && isAllVue) {
        verifiedAverageType = ensureCorrectMeanType(metricToCheck, questionTypeLookup);
    }
    return verifiedAverageType;
}

const ensureCorrectMeanType = (metric: Metric, questionTypeLookup: { [key: string]: MainQuestionType; }) => {
        const questionType = questionTypeLookup[metric.name];
        return questionType == MainQuestionType.SingleChoice ? AverageType.EntityIdMean : AverageType.ResultMean;
}

export const isTypeOfMean = (average: AverageType) => {
    return average == AverageType.ResultMean 
        || average == AverageType.EntityIdMean
        || average == AverageType.Mean
}

export const splitCrosstabAverageResults = (averages: CrosstabAverageResults[], metric: Metric | undefined):
    { averagesToChart: CrosstabAverageResults[], averagesForFooter: CrosstabAverageResults[]} =>
{
    if (metric?.isNumericVariable) {
        return {
            averagesToChart: [],
            averagesForFooter: averages
        }
    }
    return {
        averagesToChart: averages.filter(d => d.averageType != AverageType.Mentions),
        averagesForFooter: averages.filter(d => d.averageType == AverageType.Mentions)
    }
}

export const splitOvertimeAverageResults = (averages: OverTimeAverageResults[][], metric: Metric | undefined):
    { averagesToChart: OverTimeAverageResults[][], averagesForFooter: OverTimeAverageResults[][]} =>
{
    if (metric?.isNumericVariable) {
        return {
            averagesToChart: [],
            averagesForFooter: averages
        }
    }
    return {
        averagesToChart: averages.filter(d => d[0].averageType != AverageType.Mentions),
        averagesForFooter: averages.filter(d => d[0].averageType == AverageType.Mentions)
    }
}