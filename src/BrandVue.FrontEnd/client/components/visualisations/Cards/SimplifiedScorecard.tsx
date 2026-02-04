import React from 'react'
import * as BrandVueApi from "../../../BrandVueApi";
import {Metric} from '../../../metrics/metric';
import {CuratedFilters} from "../../../filter/CuratedFilters";
import {ViewHelper} from '../ViewHelper';
import {getEndOfLastMonthWithData, getEndOfPreviousMonthWithData} from '../../helpers/DateHelper';
import * as moment from "moment";
import {EntityInstance} from "../../../entity/EntityInstance";
import MetricDelta from "../MetricDelta";
import TileTemplate from "../shared/TileTemplate";
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from '../../../state/subsetSlice';

import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface IProps {
    metrics: Metric[];
    descriptionText: string;
    entityInstance: EntityInstance | undefined;
    curatedFilters: CuratedFilters;
    nextPageUrl: string;
}

const instancePlaceholder = "{{instance}}";

const SimplifiedScorecard = (props: IProps) => {
    const [scorecardResults, setScorecardResults] = React.useState<BrandVueApi.ScorecardPerformanceResults>(new BrandVueApi.ScorecardPerformanceResults());
    const [dataLoaded, setDataLoaded] = React.useState<boolean>(false);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const getEntityInstanceIds = () => {
        if (props.metrics.every(m => m.entityCombination.length === 0)) {
            return [];
        }

        return [props.entityInstance?.id ?? 0];
    }

    React.useEffect(() => {
        // Daily averages are not supported
        if (props.curatedFilters.average.makeUpTo === BrandVueApi.MakeUpTo.Day) {
            return;
        }

        const activeBrandModel = ViewHelper.createCuratedRequestModel(
            getEntityInstanceIds(),
            props.metrics,
            props.curatedFilters,
            props.entityInstance?.id ?? 0,
            { useScorecardDates: true },
            subsetId,
            timeSelection
        );
        BrandVueApi.Factory.DataClient(throwErr => throwErr())
            .getScorecardPerformanceResults(
                activeBrandModel
            ).then(r => {
                setScorecardResults(r);
                setDataLoaded(true);
            });
    },[props.entityInstance, props.curatedFilters, subsetId, timeSelection]);

    const getMetricByName = (name: string): Metric => props.metrics.find(m => m.name === name) || props.metrics[0];
    const getFormattedResultForDate = (date: Date, results: BrandVueApi.WeightedDailyResult[]): BrandVueApi.WeightedDailyResult | undefined => {
        const r = results.find(r => moment.utc(r.date).startOf('D').toString() === moment.utc(date).startOf('D').toString());
        if (r) {
            return r;
        }
        return undefined;
    }

    const getValuesAndDelta = (metricName: string, currentDate: Date, previousDate: Date, result: BrandVueApi.ScorecardPerformanceMetricResult) => {
        let metric = getMetricByName(result.metricName);
        let currentValue = getFormattedResultForDate(currentDate,result.periodResults)?.weightedResult;
        let previousValue = getFormattedResultForDate(previousDate,result.periodResults)?.weightedResult;
        let deltaVal = Number.NaN;
        if(currentValue !== undefined && previousValue !== undefined)
            deltaVal = currentValue - previousValue;
        let value = metric.fmt(currentValue);
        let deltaFmt = metric.fmt;
        let downIsGood = metric.downIsGood;
        return {value, deltaVal, deltaFmt, downIsGood};
    }

    function getTableContent() {

        if (!dataLoaded) {
            return props.metrics.map(m => {
                return (
                    <tr key={m.name}>
                        <td colSpan={3}><div className="table-row-placeholder"></div></td>
                    </tr>
                );
            });
        }

        return scorecardResults.metricResults?.map((result) => {
            let currentValues = getValuesAndDelta(result.metricName, getEndOfLastMonthWithData(props.curatedFilters.endDate), getEndOfPreviousMonthWithData(props.curatedFilters.endDate), result);
            return (
                <tr key={result.metricName}>
                    <td className="metric-name">{result.metricName}</td>
                    <td className="data">{currentValues.value}</td>
                    <td className="delta"><MetricDelta delta={currentValues.deltaVal}
                                                       formatter={currentValues.deltaFmt}
                                                       downIsGood={currentValues.downIsGood}/></td>
                </tr>
            )
        });
    }

    const description = props.entityInstance ? props.descriptionText.replace(instancePlaceholder, props.entityInstance.name) : props.descriptionText;

    return (
        <TileTemplate
            description={description}
            linkText="Monitor changes"
            nextPageUrl={props.nextPageUrl}
        >
            <table className="scorecard-tile-table">
                <tbody>
                {getTableContent()}
                </tbody>
            </table>
        </TileTemplate>
    );
};


export default SimplifiedScorecard;