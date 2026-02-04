import { Point, Series } from 'highcharts';
import React from 'react';

interface IHighchartsCustomLegendProps {
    keyToColourMap: Map<string, string>;
    chartReference: React.MutableRefObject<Highcharts.Chart | undefined | null>;
    reverse?: boolean;
    instanceNameToId?: Map<string, number>;
}

const HighchartsCustomLegend = (props: IHighchartsCustomLegendProps) => {
    const setChartState = (instanceName: string, itemArray: Series[] | Point[]) => {
        if (itemArray.some(item => item.name == instanceName || item.custom?.constituentData?.some(c => c.name == instanceName))) {
            itemArray.forEach(item => {
                if (item.name == instanceName || item.custom?.constituentData?.some(c => c.name == instanceName)) {
                    item.setState('normal');
                } else {
                    item.setState('inactive');
                }
            });
        }
    }

    const handleLegendItemMouseEnter = (instanceName: string) => {
        if (props.chartReference?.current) {
            if (props.chartReference.current.series.length > 1) {
                setChartState(instanceName, props.chartReference.current.series);
                props.chartReference.current.series.forEach(s => setChartState(instanceName, s.data));
            } else {
                setChartState(instanceName, props.chartReference.current.series[0].data);
            }
        }
    }

    const clearChartState = (itemArray: Series[] | Point[]) => {
        itemArray.forEach(item => item.setState('normal'));
    }

    const handleLegendItemMouseExit = () => {
        if (props.chartReference?.current) {
            if (props.chartReference.current.series.length > 1) {
                clearChartState(props.chartReference.current.series);
                props.chartReference.current.series.forEach(s => clearChartState(s.data));
            } else {
                clearChartState(props.chartReference.current.series[0].data);
            }
        }
    }

    const getInstanceDisplayName = (instanceName: string) => {
        const id = props.instanceNameToId?.get(instanceName);
        return id != null ? `${instanceName} (${id})` : instanceName;
    }

    const getLegendItems = () => {
        const legendItems: JSX.Element[] = [];
        props.keyToColourMap.forEach((colour, instanceName) => {
            legendItems.push(<div key={instanceName} className="legend-item" onMouseEnter={() => handleLegendItemMouseEnter(instanceName)} onMouseLeave={handleLegendItemMouseExit}>
                <span className="legend-icon" style={{ color: colour, backgroundColor: colour }}></span> {getInstanceDisplayName(instanceName)}
            </div>);
        });
        return props.reverse ? legendItems.reverse() : legendItems;
    }

    return (
        <div className="legend-container">
            {getLegendItems()}
        </div>
    );
}
export default HighchartsCustomLegend;