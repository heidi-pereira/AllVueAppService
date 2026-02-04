import { ICellResult, Significance } from "../../../BrandVueApi";
import { CrosstabHeader } from "./CrosstabHeader";
import Tooltip from "../../Tooltip";
import { getFormattedValueText } from "../../../components/helpers/SurveyVueUtils";
import { Metric } from "../../../metrics/metric";

interface IProps {
    includeCounts: boolean;
    cellSuffix: string;
    withToolTip: boolean;
    data: ICellResult | undefined;
    index: number;
    crosstabHeader: CrosstabHeader
    showSampleSize: boolean;
    dataColumns: CrosstabHeader[];
    metric: Metric;
    decimalPlaces: number;
    showSignificance: boolean;
    setHoverColumnIndex(number: number | undefined): void;
    roundCountNumber(value: number): string;
}

const CrosstabCell = (props: IProps) => {
    const dataCount = props.data?.count;
    const sampleSize = props.data?.sampleForCount;
    const significance = props.data?.significance ?? Significance.None;

    const countValue = (sampleSize && sampleSize > 0)
        ? dataCount ? props.roundCountNumber(dataCount): "0"
                        : "-";

    const getValue = (data: ICellResult | undefined, metric: Metric, decimalPlaces: number) => {
        if (data === undefined) {
            return "";
        }

        return (data.sampleSizeMetaData.sampleSize && data.sampleSizeMetaData.sampleSize.unweighted > 0)
            ? getFormattedValueText(data.result, metric, decimalPlaces)
            : "-";
    }

    const getSigColumns = (data: ICellResult | undefined, crosstabHeader: CrosstabHeader) => {
        if (data) {
            return data.significantColumns ?? undefined;
        }
        return crosstabHeader.significanceIdentifier ? [crosstabHeader.significanceIdentifier] : undefined;
    }

    const sigColumns = getSigColumns(props.data, props.crosstabHeader);
    const indexScore = props.data?.indexScore?.toLocaleString();

    const getCountValue = () => {
        if (!props.includeCounts) {
            return;
        }
        if (sampleSize && sampleSize > 0) {
            return (
                <div className="count-cell">
                    {countValue + ((props.showSampleSize && sampleSize) ? " of " + props.roundCountNumber(sampleSize) : "")}
                </div>
            )
        }
        return (
            <div className="count-cell">
                {props.showSampleSize ? "0 of 0" : "-" }
            </div>
        )
    }

    const getValueCell = () => {
        return (
            <td className={`data-cell${props.cellSuffix}`}
                onMouseEnter={() => props.setHoverColumnIndex(props.index)}
                onMouseLeave={() => props.setHoverColumnIndex(undefined)}>
                <div className="value-container">
                    <span>{getValue(props.data, props.metric, props.decimalPlaces)}</span>
                    {props.showSignificance && significance == Significance.Up && <i className={`material-symbols-outlined ${props.metric.downIsGood ? "sig-red" : "sig-green"}`}>arrow_upward</i>}
                    {props.showSignificance && significance == Significance.Down && <i className={`material-symbols-outlined ${props.metric.downIsGood ? "sig-green" : "sig-red"}`}>arrow_downward</i>}
                </div>
                {props.includeCounts && getCountValue()}
                {sigColumns &&
                    <div className="significant-columns">
                        {sigColumns.join(', ')}
                    </div>
                }
                {indexScore &&
                    <div className="index-score">
                        {indexScore}
                    </div>
                }
            </td>
        );
    }

    const getColumnDetail = (sigColumn: string, columnIndex: number, dataColumns: CrosstabHeader[])  => {
        let result = "Unknown";
        const fullName = dataColumns[columnIndex].id;
        const name = dataColumns[columnIndex].name ?? "****";
        let groupName = "";

        if (fullName.endsWith(name)) {
            const numberOfLetters = fullName.length - name.length;
            groupName = fullName.substr(0, numberOfLetters);
        }
        dataColumns.forEach((dataColumn) => {
            if (dataColumn.significanceIdentifier === sigColumn && dataColumn.name && dataColumn.id.startsWith(groupName)) {
                result= dataColumn.name;
            }
        });
        return result;
    }

    const getValueCellWithTooltip = () => {
        if (sigColumns && sigColumns.length) {
            let tooltip = "Significant: ";
            sigColumns.forEach((sigColumn) => {
                tooltip += getColumnDetail(sigColumn, props.index,  props.dataColumns) + ", ";
            });
            tooltip = tooltip.substr(0, tooltip.length - 2);
            return (
                <Tooltip placement="top" title={tooltip}>
                    {getValueCell()}
                </Tooltip>
            );
        }
        return getValueCell();
    }

    if (props.withToolTip) {
        return getValueCellWithTooltip();
    }

    return getValueCell();
};

export default CrosstabCell;
