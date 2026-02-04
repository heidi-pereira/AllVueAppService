import React from 'react'
import * as BrandVueApi from "../../../BrandVueApi";
import {Metric} from '../../../metrics/metric';
import {CuratedFilters} from "../../../filter/CuratedFilters";
import {ViewHelper} from '../ViewHelper';
import {getEndOfLastMonthWithData} from '../../helpers/DateHelper';
import * as moment from "moment";
import {EntityInstance} from "../../../entity/EntityInstance";
import TileTemplate from "../shared/TileTemplate";
import {getMetricOrThrow} from "../../../metrics/metricHelper";
import { useAppSelector } from '../../../state/store';
import { selectSubsetId } from '../../../state/subsetSlice';

import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface IProps {
    metrics: Metric[];
    descriptionText: string;
    entityInstance: EntityInstance | undefined
    curatedFilters: CuratedFilters;
    nextPageUrl: string;
}

const FunnelCard = (props: IProps) => {
    const [scorecardResults, setScorecardResults] = React.useState(new BrandVueApi.ScorecardPerformanceResults());
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
        setDataLoaded(false);
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

    }, [props.entityInstance, JSON.stringify(props.curatedFilters), subsetId, timeSelection]);

    const getFormattedResultForDate = (date: Date, results: BrandVueApi.WeightedDailyResult[]): BrandVueApi.WeightedDailyResult | undefined => {
        return results.find(r => moment.utc(r.date).startOf('D').toString() === moment.utc(date).startOf('D').toString());
    }

    const getValue = (currentDate: Date, result: BrandVueApi.ScorecardPerformanceMetricResult) => {
        let metric = getMetricOrThrow(props.metrics, result.metricName);
        let currentValue = getFormattedResultForDate(currentDate, result.periodResults)?.weightedResult;
        let value = metric.fmt(currentValue);
        return value;
    }

    const getTableContent = () => {
        if (!dataLoaded) {
            return props.metrics.map(m => {
                return (
                    <div key={m.name}>
                        <div className="table-row-placeholder"></div>
                    </div>
                );
            });
        }

        return scorecardResults.metricResults?.map((result) => {
            let currentValue = getValue(getEndOfLastMonthWithData(props.curatedFilters.endDate), result);
            return (
                <div className="single-bar">
                    <div key={result.metricName}>
                        <div className="metric-name">{result.metricName}</div>
                        <div className="data">{currentValue}</div>
                    </div>
                    <div className="bar-bg">
                        <div className="bar-fg" style={{
                            width: currentValue
                        }}></div>
                    </div>
                </div>
            )
        });
    }

    //comparing values of each metric in the list to the previous and get relative difference
    const getBiggestDropOffDescription = () => {
        const instancePlaceholder = "{{instance}}";
        const metricPlaceholder = "{{metric}}";
        let biggestDropOff: number = 0;
        let dropOffMetricName: string = "";
        let previousMetricValue : number | undefined = 1;

        scorecardResults.metricResults.forEach((r, i) => {
            let currentMetricValue = getFormattedResultForDate(getEndOfLastMonthWithData(props.curatedFilters.endDate), r.periodResults)?.weightedResult;
            if (currentMetricValue && previousMetricValue) {
                let relativeDropOff = 100 - (currentMetricValue / previousMetricValue) * 100;
                if (relativeDropOff > biggestDropOff) {
                    biggestDropOff = relativeDropOff;
                    dropOffMetricName = r.metricName;
                }
            }
            previousMetricValue = currentMetricValue;
        })

        let description = props.entityInstance ? props.descriptionText.replace(instancePlaceholder, props.entityInstance.name) : props.descriptionText;
        description = description.replace(metricPlaceholder, dropOffMetricName);

        return description;
    }

    return (
        <TileTemplate
            description={getBiggestDropOffDescription()}
            linkText="Compare usage vs competitors"
            nextPageUrl={props.nextPageUrl}
        >
            <div className="all-bars">
                {getTableContent()}
            </div>
        </TileTemplate>
    );
};

export default FunnelCard;