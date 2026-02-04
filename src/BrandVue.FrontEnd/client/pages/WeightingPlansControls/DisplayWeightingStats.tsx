import Highcharts, { AxisLabelsFormatterContextObject, PointOptionsObject} from "highcharts";
import HighchartsReact from "highcharts-react-official";
import React from "react";
import { useResizeDetector } from "react-resize-detector";
import { Modal, ModalBody } from "reactstrap";
import { RimWeightingCalculationResult, WeightsDistribution } from "../../BrandVueApi";
import { SlateDark } from "../../components/helpers/ChromaHelper";
import { toPercentage } from "../../helpers/MathHelper";

interface IProps {
    isOpen: boolean;
    nodeName: string|undefined;
    stats: RimWeightingCalculationResult | null;
    closeModal: () => void;
    maxWeightClass: string;
    minWeightClass: string;
    efficiencyClass: string;
}

const DisplayWeightingStats = (props: IProps) => {


    function pointValue(xValue: number,yValue: number): PointOptionsObject {
        return {
            id: xValue.toFixed(1),
            name: xValue.toFixed(1),
            y: yValue,
            dataLabels: {
                color: SlateDark,
            },
        };
    }


    function getGraphSeries(weightsDistribution: WeightsDistribution) {

        if (weightsDistribution && weightsDistribution.buckets) {
            let myBuckets = Object.assign([], weightsDistribution.buckets);
            const maxKey = (myBuckets.length - 1) * weightsDistribution.bucketFactor;
            let largestValidElement = myBuckets.length;
            for (let i = myBuckets.length - 1; (i >= 0) && (myBuckets[i]==0); i--) {
                largestValidElement = i;
            }

            myBuckets = myBuckets.splice(0, largestValidElement);
            const data = myBuckets.map((r, i) => pointValue((i * weightsDistribution.bucketFactor), r));
            const yMax = Math.max(...myBuckets);
            const yMin = Math.min(...myBuckets);


            return {
                chart: {
                    height: height,
                },
                tooltip: {
                    enabled: true,
                    formatter: function (this: Highcharts.TooltipFormatterContextObject): string {
                        const value = typeof this.key === "number" ? this.key : Number(this.key);
                        if (value >= maxKey) {
                            return `${this.y} respondents have a weight of >= ${this.key}`
                        }
                        return `${this.y} respondents have a weight of ${this.key} to ${this.key}999`
                    }
                },
                xAxis: {
                    categories: ['Weightings'],
                    labels: {
                        formatter: function (this: AxisLabelsFormatterContextObject): string {
                            if (typeof this.value === 'number') {
                                return (this.value / 10).toFixed(1)
                            }
                            return this.value;
                        }
                    }
                },
                yAxis: {
                    min: yMin,
                    max: yMax,
                    tickmarkPlacement: "on",
                    gridLineDashStyle: "Dot",
                    gridLineColor: "#a7a9ac",
                    title:
                        { text:'Number of respondents'}
                },
                series: [{
                    type: "bar",
                    data: data,
                    color: "#000000",
                    lineWidth: 1,
                    animation: true,
                    showInLegend: false,
                }]
            };
        }
    }

    const { width, height, ref } = useResizeDetector<HTMLDivElement>({ refreshMode: 'throttle', refreshRate: 100 });
    const chartOptions = getGraphSeries(props.stats?.weightsDistribution!);
    let options = chartOptions;
    if (options) {
        options = {
            ...options,
            chart: {
                ...options?.chart,
                height: height,
            }
        };
    };
    return (
        <Modal isOpen={props.isOpen} centered={true} className="stats-modal" autoFocus={false}>
            <h3>Weighting statistics</h3>
            <h4>{props.nodeName}</h4>
            <ModalBody>
                <p className={`text${props.minWeightClass ? ' '+props.minWeightClass: ''}`}>Min Weight: {props.stats?.minWeight.toFixed(5)}</p>
                <p className={`text${props.maxWeightClass ? ' ' + props.maxWeightClass : ''}`}>Max Weight: {props.stats?.maxWeight.toFixed(5)}</p>
                <p className={`text${props.efficiencyClass ? ' ' + props.efficiencyClass: ''}`}>Efficiency: {props.stats ? toPercentage(props.stats.efficiencyScore, 1)+'%' : '--'}</p>
                <p className="text ">Iterations: {props.stats?.iterationsRequired}</p>
                <p>
                    <div ref={ref} className="weightingStatsChart">
                        <HighchartsReact highcharts={Highcharts}
                            options={options}
                        />
                    </div>
                </p>
                <div className="button-container">
                    <button onClick={() => { props.closeModal(); }} className={`primary-button }`}>Close</button>
                </div>
            </ModalBody>
        </Modal>
    );
}
export default DisplayWeightingStats;