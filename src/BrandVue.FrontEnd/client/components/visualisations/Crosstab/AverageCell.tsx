import { AverageType, CrosstabBreakAverageResults } from "../../../BrandVueApi";
import { CrosstabHeader } from "./CrosstabHeader";
import { getFormattedValueText } from "../../helpers/SurveyVueUtils";
import { Metric } from "../../../metrics/metric";
import { StatisticType } from "client/components/enums/StatisticType";

interface IProps {
    cellSuffix: string;
    averageType: AverageType;
    data: CrosstabBreakAverageResults;
    index: number;
    crosstabHeader: CrosstabHeader
    dataColumns: CrosstabHeader[];
    metric: Metric;
    decimalPlaces: number;
    setHoverColumnIndex(number: number | undefined): void
    statisticType?: StatisticType;
}

const AverageCell = (props: IProps) => {
    const getValue = () => {
        if(props.statisticType === StatisticType.StandardDeviation) {
            return props.data.weightedDailyResult.standardDeviation?.toFixed(3);
        }

        if(props.statisticType === StatisticType.Variance) {
            return props.data.weightedDailyResult.variance?.toFixed(3);
        }

        if (props.data.weightedDailyResult.weightedResult == 0 && props.data.weightedDailyResult.unweightedSampleSize == 0) {
            return "-";
        }
        if (props.averageType === AverageType.Mentions || props.metric.isNumericVariable ||
            (props.averageType === AverageType.EntityIdMean)) {
            return props.data.weightedDailyResult.weightedResult.toFixed(2);
        }

        if(props.averageType == AverageType.Median){
            return props.data.weightedDailyResult.weightedResult.toFixed(0);
        }

        return getFormattedValueText(props.data.weightedDailyResult.weightedResult, props.metric, props.decimalPlaces);
    }

    return (
        <td className={`data-cell${props.cellSuffix}`}
            onMouseEnter={() => props.setHoverColumnIndex(props.index)}
            onMouseLeave={() => props.setHoverColumnIndex(undefined)}>
            <div className="value-container">
                <span>{getValue()}</span>
            </div>
        </td>
    );
};

export default AverageCell;
