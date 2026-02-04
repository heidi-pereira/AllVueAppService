import Highcharts from "highcharts";
import HighchartsExporting from "highcharts/modules/exporting";
HighchartsExporting(Highcharts);

export class highchartsGlobalConfiguration {
    public configure(): void {
        const options: Highcharts.Options = {
            lang: {
                loading: "",
            },
            credits: {
                enabled: false
            },
            title: {
                text: ''
            },
            loading: {
                labelStyle: { top: "0" },
                style: { opacity: 1, backgroundColor: "rgba(255,255,255,1)" }
            },
            chart: {
                style: {
                    fontFamily: "Roboto, Segoe UI, sans-serif",
                    fontSize: "14px"
                },
                events: {
                    /*
                     * For non-split-metric line charts only. This dynamically sets the margin bottom of the charts. 
                     * This is needed to fix an issue with some charts where the legend was overlapping
                     * the xAxis labels (https://app.shortcut.com/mig-global/story/89946/)
                     */
                    render: function (this: Highcharts.Chart) {
                        const MARGIN_BUFFER = 40;

                        const chart = this;
                        const xAxis = chart.xAxis[0];
                        const isLineChart = chart.series[0]?.type === "line";
                        const isSplitMetricChart = chart.series.length > 2;

                        if (isLineChart && !isSplitMetricChart && xAxis) {
                            const maxLabelHeight = Math.max(...Object.values(xAxis.ticks).map(tick => tick.label ? tick.label.getBBox().height : 0));
                            const legendHeight = chart.legend?.box?.getBBox().height || 0;
                            const newMarginBottom = maxLabelHeight + legendHeight + MARGIN_BUFFER;

                            if (chart.options.chart?.marginBottom !== newMarginBottom) {
                                chart.update({
                                    chart: {
                                        marginBottom: newMarginBottom
                                    }
                                } as Highcharts.Options, false);
                            }
                        }
                    }
                }
            },
            tooltip: {
                backgroundColor: "#1A1A1A",
                borderColor: "#1A1A1A",
                borderWidth: 1,
                borderRadius: 10,
                padding: 15,
                style: {
                    color: "#CCC",
                    fontSize: "14px"
                },
                className: "custom-tooltip-container",
                shadow: false,
                useHTML: true,
                // Force the tooltip into a container with high z-index - to stop rendering beneath xAxis labels 
                outside: true,
            },
            legend: {
                itemStyle: {
                    fontSize: "14px"
                }
            },
            exporting: {
                enabled: false,
            },
            xAxis: {
                labels: {
                    style: {
                        textOverflow: 'none'
                    },
                    autoRotation: [-40, -80],
                    useHTML: true
                },
            },
            yAxis: {
                title: {
                    margin: 25
                }
            }
        };

        Highcharts.setOptions(options);
    }
}
