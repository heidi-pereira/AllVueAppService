import { Metric } from '../../../../metrics/metric';
import React from 'react';
import Highcharts, { Options } from 'highcharts';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import TileTemplate from '../../shared/TileTemplate';
import TileTemplateChart from '../../Cards/TileTemplateChart';
import { IAverageDescriptor, BaseExpressionDefinition, CrosstabAverageResults, MainQuestionType, OverTimeAverageResults, SampleSizeMetadata } from '../../../../BrandVueApi';
import AllVueDescriptionFooter from '../../AllVueDescriptionFooter';
import HighchartsCustomLegend from '../../HighchartsCustomLegend';

interface IReportsPageColumnChartTemplateProps {
    isLoading: boolean;
    chartOptions: Options | undefined;
    chart: React.MutableRefObject<Highcharts.Chart | undefined | null>;
    sampleSizeMeta?: SampleSizeMetadata;
    metric: Metric;
    questionTypeLookup: {[key: string]: MainQuestionType};
    filterInstanceNames?: string[];
    baseExpressionOverride?: BaseExpressionDefinition;
    footerAverages: CrosstabAverageResults[] | OverTimeAverageResults[][] | OverTimeAverageResults[] | undefined;
    decimalPlaces: number;
    legendMap?: Map<string, string>;
    instanceNameToId?: Map<string, number>;
    averageDescriptor?: IAverageDescriptor;
    filterByIndex?: number;
}

const ReportsPageColumnChartTemplate = (props: IReportsPageColumnChartTemplateProps) => {
    if (props.isLoading) {
        return (
            <TileTemplate>
                <PageCardPlaceholder />
            </TileTemplate>
        );
    }

    const getCustomLegend = () => {
        return props.legendMap && <HighchartsCustomLegend keyToColourMap={props.legendMap}
            instanceNameToId={props.instanceNameToId}
            chartReference={props.chart} />
    }

    return <TileTemplateChart
        handleHeight
        handleWidth
        getChartOptions={(width, height) => props.chartOptions}
        callback={c => props.chart.current = c}
        resizeElementClass="tile-chart-container">
        {getCustomLegend() }
        <AllVueDescriptionFooter
            sampleSizeMeta={props.sampleSizeMeta}
            metric={props.metric}
            filterInstanceNames={props.filterInstanceNames}
            baseExpressionOverride={props.baseExpressionOverride}
            isSurveyVue={true}
            decimalPlaces={props.decimalPlaces}
            footerAverages={props.footerAverages}
            averageDescriptor={props.averageDescriptor}
            filterByIndex={props.filterByIndex}/>
    </TileTemplateChart>;
}

export default ReportsPageColumnChartTemplate;