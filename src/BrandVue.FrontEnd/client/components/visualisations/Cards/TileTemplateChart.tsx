import React from "react";
import { PropsWithChildren } from "react";
import { useResizeDetector } from 'react-resize-detector';
import Highcharts from 'highcharts';
import HighchartsReact from 'highcharts-react-official';
import TileTemplate, { ITileTemplateProps } from "../shared/TileTemplate";

interface IChartTileTemplateProps extends ITileTemplateProps {
    getChartOptions: (width: number, height: number) => Highcharts.Options | undefined;
    refreshMode?: 'throttle' | 'debounce';
    callback?: Highcharts.ChartCallbackFunction;
    resizeElementClass?: string;
    className?: string;
    handleWidth?: boolean;
    handleHeight?: boolean;
}

const TileTemplateChart: React.FunctionComponent<React.PropsWithChildren & IChartTileTemplateProps> = (props: PropsWithChildren<IChartTileTemplateProps>) => {
    const { width, height, ref } = useResizeDetector<HTMLDivElement>({ refreshMode: props.refreshMode, refreshRate: 100 });
    const [chart, setChart] = React.useState<Highcharts.Chart | undefined>(undefined);
    const chartOptions = props.getChartOptions(width ?? 0, height ?? 0);

    React.useEffect(() => {
        if (chart) {
            chart.reflow();
        }
    }, [width, height]);

    const callback = (chart: Highcharts.Chart) => {
        setChart(chart);
        if (props.callback) {
            props.callback(chart);
        }
    }

    const getOptions = () => {
        let options = chartOptions;

        if (props.handleWidth) {
            options = {
                ...options,
                chart: {
                    ...options?.chart,
                    width: width,
                }
            };
        }

        if (props.handleHeight) {
            options = {
                ...options,
                chart: {
                    ...options?.chart,
                    height: height,
                }
            };
        }
        return options;
    }

    const divClass = props.resizeElementClass ?? props.className;
    return <TileTemplate {...props} >
        <div ref={ref} className={divClass}>
            <HighchartsReact highcharts={Highcharts}
                options={getOptions()}
                callback={callback} />
        </div>
        {props.children}
    </TileTemplate>;
};

export default TileTemplateChart;