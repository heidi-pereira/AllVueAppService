import { CrosstabHeader } from "./CrosstabHeader";
import { Metric } from "../../../metrics/metric";
import CrosstabCell from "./CrosstabCell";

interface IProps {
    key: number;
    cellSuffix: string;
    index: number;
    crosstabHeader: CrosstabHeader
    dataColumns: CrosstabHeader[];
    metric: Metric;
    decimalPlaces: number;
    setHoverColumnIndex(number: number | undefined): void;
    roundCountNumber(value: number): string;
}

const CrosstabHeaderCell = (props: IProps) => {
    return (
        <CrosstabCell
            key={props.key}
            crosstabHeader={props.crosstabHeader}
            index={props.key}
            withToolTip={false}
            cellSuffix={props.cellSuffix}
            data={undefined}
            showSampleSize={false}
            includeCounts={false}
            dataColumns={props.dataColumns}
            metric={props.metric}
            decimalPlaces={props.decimalPlaces}
            setHoverColumnIndex={props.setHoverColumnIndex}
            roundCountNumber={props.roundCountNumber}
            showSignificance={false}
        />
    );
};

export default CrosstabHeaderCell;
