import { render } from "@testing-library/react";
import AllVueDescriptionFooter, { IAllVueDescriptionFooterProps } from "./AllVueDescriptionFooter";
import { AverageType, CrosstabAverageResults, CrosstabBreakAverageResults, WeightedDailyResult } from "../../BrandVueApi";
import { Metric } from '../../metrics/metric';
import { MetricSet } from '../../metrics/metricSet';


const getFooterPropsWithCrosstabAverageResults = (metric: Metric, footerAverages: CrosstabAverageResults[], decimalPlaces: number) => {
    const defaultProps: IAllVueDescriptionFooterProps = {
        metric: metric,
        isSurveyVue: true,
        footerAverages: footerAverages,
        decimalPlaces: decimalPlaces
    }

    return defaultProps;
}

const singleChoiceMetricName = "singleChoiceMetric";
const multiChoiceMetricName = "multiChoiceMetric";

const getSingleChoiceMetric = () => {
    const ms = new MetricSet();
    const singleChoiceMetric = new Metric(ms);
    singleChoiceMetric.name = singleChoiceMetricName;
    singleChoiceMetric.varCode = singleChoiceMetricName;
    return singleChoiceMetric;
}

const getMultiChoiceMetric = () => {
    const ms = new MetricSet();
    const multiChoiceMetric = new Metric(ms);
    multiChoiceMetric.name = multiChoiceMetricName;
    multiChoiceMetric.varCode = multiChoiceMetricName;
    return multiChoiceMetric;
}

describe(AllVueDescriptionFooter, () => {
    
    it("single choice question with mean should format correctly ignoring dp", () => {
        const singleChoiceMetric = getSingleChoiceMetric();
        const singleChoiceAverageResults = new CrosstabAverageResults({
            averageType: AverageType.EntityIdMean,
            dailyResultPerBreak: [],
            overallDailyResult: new CrosstabBreakAverageResults({
                breakName: "test",
                weightedDailyResult: new WeightedDailyResult({
                    weightedResult: 2.0958679,
                    unweightedSampleSize: 658,
                    weightedValueTotal: 0,
                    unweightedValueTotal: 6,
                    text: "",
                    responseIdsForDay: [],
                    weightedSampleSize: 1658,
                    childResults: [],
                    date: new Date()
                })
            })
        })
    
        const footerProps = getFooterPropsWithCrosstabAverageResults(singleChoiceMetric, [singleChoiceAverageResults], 0);
        const { container } = render(
            <AllVueDescriptionFooter {...footerProps} />
        )

        const element = container.getElementsByClassName("footer-element");
        const averageElement = Array.from(element).filter(e => e.textContent?.includes("Average"))[0];
        expect(averageElement.textContent).toBe(
            "Average (mean) = 2.1"
        );
    })

    it("multi choice question with mean with 0dp should format correctly", () => {
        const multiChoiceMetric = getMultiChoiceMetric();
        const multiChoiceAverageResults = new CrosstabAverageResults({
            averageType: AverageType.ResultMean,
            dailyResultPerBreak: [],
            overallDailyResult: new CrosstabBreakAverageResults({
                breakName: "test",
                weightedDailyResult: new WeightedDailyResult({
                    weightedResult: 0.16403785,
                    unweightedSampleSize: 10461,
                    weightedValueTotal:0,
                    unweightedValueTotal: 11,
                    text: "",
                    responseIdsForDay: [],
                    weightedSampleSize: 10461,
                    childResults: [],
                    date: new Date()
                })
            })
        })
    
        const footerProps = getFooterPropsWithCrosstabAverageResults(multiChoiceMetric, [multiChoiceAverageResults], 0);
        const { container } = render(
            <AllVueDescriptionFooter {...footerProps} />
        )

        const element = container.getElementsByClassName("footer-element");
        const averageElement = Array.from(element).filter(e => e.textContent?.includes("Average"))[0];
        expect(averageElement.textContent).toBe(
            "Average (mean) = 16%"
        );
    })

    it("multi choice question with mean with 1dp should format correctly", () => {
        const multiChoiceMetric = getMultiChoiceMetric();
        const multiChoiceAverageResults = new CrosstabAverageResults({
            averageType: AverageType.ResultMean,
            dailyResultPerBreak: [],
            overallDailyResult: new CrosstabBreakAverageResults({
                breakName: "test",
                weightedDailyResult: new WeightedDailyResult({
                    weightedResult: 0.16403785,
                    unweightedSampleSize: 10461,
                    weightedValueTotal:0,
                    unweightedValueTotal: 11,
                    text: "",
                    responseIdsForDay: [],
                    weightedSampleSize: 10461,
                    childResults: [],
                    date: new Date()
                })
            })
        })
    
        const footerProps = getFooterPropsWithCrosstabAverageResults(multiChoiceMetric, [multiChoiceAverageResults], 1);
        const { container } = render(
            <AllVueDescriptionFooter {...footerProps} />
        )

        const element = container.getElementsByClassName("footer-element");
        const averageElement = Array.from(element).filter(e => e.textContent?.includes("Average"))[0];
        expect(averageElement.textContent).toBe(
            "Average (mean) = 16.4%"
        );
    })

    it("single choice question with median with 0dp should format correctly", () => {
        const singleChoiceMetric = getSingleChoiceMetric();
        const singleChoiceAverageResults = new CrosstabAverageResults({
            averageType: AverageType.Median,
            dailyResultPerBreak: [],
            overallDailyResult: new CrosstabBreakAverageResults({
                breakName: "test",
                weightedDailyResult: new WeightedDailyResult({
                    weightedResult: 1,
                    unweightedSampleSize: 3948,
                    weightedValueTotal: 0,
                    unweightedValueTotal: 2,
                    text: "",
                    responseIdsForDay: [],
                    weightedSampleSize: 1316,
                    childResults: [],
                    date: new Date()
                })
            })
        })
    
        const footerProps = getFooterPropsWithCrosstabAverageResults(singleChoiceMetric, [singleChoiceAverageResults], 0);
        const { container } = render(
            <AllVueDescriptionFooter {...footerProps} />
        )

        const element = container.getElementsByClassName("footer-element");
        const averageElement = Array.from(element).filter(e => e.textContent?.includes("Average"))[0];
        expect(averageElement.textContent).toBe(
            "Average (median) = 1"
        );
    })

    it("single choice question with median with 1dp should format correctly", () => {
        const singleChoiceMetric = getSingleChoiceMetric();
        const singleChoiceAverageResults = new CrosstabAverageResults({
            averageType: AverageType.Median,
            dailyResultPerBreak: [],
            overallDailyResult: new CrosstabBreakAverageResults({
                breakName: "test",
                weightedDailyResult: new WeightedDailyResult({
                    weightedResult: 2,
                    unweightedSampleSize: 3948,
                    weightedValueTotal: 0,
                    unweightedValueTotal: 2,
                    text: "",
                    responseIdsForDay: [],
                    weightedSampleSize: 1316,
                    childResults: [],
                    date: new Date()
                })
            })
        })
    
        const footerProps = getFooterPropsWithCrosstabAverageResults(singleChoiceMetric, [singleChoiceAverageResults], 1);
        const { container } = render(
            <AllVueDescriptionFooter {...footerProps} />
        )

        const element = container.getElementsByClassName("footer-element");
        const averageElement = Array.from(element).filter(e => e.textContent?.includes("Average"))[0];
        expect(averageElement.textContent).toBe(
            "Average (median) = 2"
        );
    })

    it("Average mentions should format correctly, ignoring dp", () => {
        const multiChoiceMetric = getMultiChoiceMetric();
        const multiChoiceAverageResults = new CrosstabAverageResults({
            averageType: AverageType.Mentions,
            dailyResultPerBreak: [],
            overallDailyResult: new CrosstabBreakAverageResults({
                breakName: "test",
                weightedDailyResult: new WeightedDailyResult({
                    weightedResult: 1.8044164,
                    unweightedSampleSize: 951,
                    weightedValueTotal:0,
                    unweightedValueTotal: 0,
                    text: "",
                    responseIdsForDay: [],
                    weightedSampleSize: 951,
                    childResults: [],
                    date: new Date()
                })
            })
        })
    
        const footerProps = getFooterPropsWithCrosstabAverageResults(multiChoiceMetric, [multiChoiceAverageResults], 0);
        const { container } = render(
            <AllVueDescriptionFooter {...footerProps} />
        )

        const element = container.getElementsByClassName("footer-element");
        const averageElement = Array.from(element).filter(e => e.textContent?.includes("Average"))[0];
        expect(averageElement.textContent).toBe(
            "Average (mentions) = 1.8"
        );
    })
})
