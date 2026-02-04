import React from 'react'
import * as BrandVueApi from "../../../BrandVueApi";
import { Metric } from '../../../metrics/metric';
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { ViewHelper } from '../ViewHelper';
import { EntityInstance } from "../../../entity/EntityInstance";
import MetricDelta from "../MetricDelta";
import { FilterInstance } from "../../../entity/FilterInstance";
import { IEntityConfiguration } from '../../../entity/EntityConfiguration';
import TileTemplate from '../shared/TileTemplate';
import { RankingTableResult } from "../../../BrandVueApi";
import { NoDataError } from '../../../NoDataError';
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from '../../../state/subsetSlice';

import {selectTimeSelection} from "../../../state/timeSelectionStateSelectors";

interface IProps {
    metric: Metric;
    descriptionText: string;
    entityInstance: EntityInstance | undefined;
    curatedFilters: CuratedFilters;
    nextPageUrl: string;
    entityConfiguration: IEntityConfiguration;
    numberOfScoresToShow: number | undefined;
}

function sortByDescendingValue(a: RankingTableResult, b: RankingTableResult) {
    return b.currentWeightedDailyResult.weightedResult - a.currentWeightedDailyResult.weightedResult;
}

const instancePlaceholder = "{{instance}}";

const RankingScorecard = (props: IProps) => {
    const [rankingResults, setRankingResults] = React.useState<BrandVueApi.RankingTableResults>(new BrandVueApi.RankingTableResults());
    const [dataLoaded, setDataLoaded] = React.useState<boolean>(false);
    const [errorMessage, setErrorMessage] = React.useState<string>();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const defaultNumberOfScoresToShow = 3;
    const correctedNumberOfScoresToShow = props.numberOfScoresToShow === undefined || props.numberOfScoresToShow < 0 ?
        defaultNumberOfScoresToShow :
        props.numberOfScoresToShow;

    React.useEffect(() => {
        // Daily averages are not supported
        if (props.curatedFilters.average.makeUpTo === BrandVueApi.MakeUpTo.Day) {
            return;
        }

        if (props.metric.entityCombination.length === 0) {
            throw new Error("Can't perform ranking request for profile metrics");
        }

        if (props.metric.entityCombination.length > 1) {
            let entityType = props.metric.entityCombination.find(e => !e.isBrand);
            if (entityType !== undefined) {
                let entitySet = props.entityConfiguration.getDefaultEntitySetFor(entityType);
                const filterInstance = new FilterInstance(props.entityConfiguration.defaultEntityType, props.entityInstance!);

                setDataLoaded(false);
                setErrorMessage(undefined);
                BrandVueApi.Factory.DataClient(throwErr => throwErr())
                    .getRankingTableResults(
                        ViewHelper.createMultiEntityRequestModel({
                            curatedFilters: props.curatedFilters,
                            metric: props.metric,
                            splitBySet: entitySet,
                            filterInstances: [filterInstance],
                            continuousPeriod: false,
                            subsetId: subsetId
                        }, timeSelection)
                    )
                    .then(r => {
                        setRankingResults(r);
                        setDataLoaded(true);
                    }).catch((e: any) => {
                        if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                            setErrorMessage(e.message);
                        }
                        else {
                            throw e;
                        }
                    });
            }
            else {
                throw new Error("Can't do multityentity request because there is no other entity type than Brand");
            }
        } else {
            const entityType = props.metric.entityCombination[0];
            const entitySet = props.entityConfiguration.getDefaultEntitySetFor(entityType);
            const instances = entitySet.getInstances().getAll().map(i => i.id);

            setDataLoaded(false);
            setErrorMessage(undefined);
            BrandVueApi.Factory.DataClient(throwErr => throwErr())
                .getRankedBrands(
                    ViewHelper.createCuratedRequestModel(instances,
                        [props.metric],
                        props.curatedFilters,
                        0,
                        {},
                        subsetId,
                        timeSelection)
                ).then(r => {
                    setRankingResults(r);
                    setDataLoaded(true);
                }).catch((e: any) => {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        setErrorMessage(e.message);
                    }
                    else {
                        throw e;
                    }
                });
        }

    }, [props.entityInstance, props.metric, props.curatedFilters, subsetId, timeSelection]);

    const getValuesAndDelta = (result: BrandVueApi.RankingTableResult) => {
        let currentValue = result.currentWeightedDailyResult.weightedResult;
        let previousValue = result.previousWeightedDailyResult.weightedResult;
        let deltaVal = Number.NaN;
        if (currentValue !== undefined && previousValue !== undefined)
            deltaVal = currentValue - previousValue;
        let value = props.metric.fmt(currentValue);
        let deltaFmt = props.metric.fmt;
        let downIsGood = props.metric.downIsGood;
        return { value, deltaVal, deltaFmt, downIsGood };
    }

    function getTableContent() {

        if (!dataLoaded && !errorMessage) {
            return Array.from(Array(correctedNumberOfScoresToShow).keys()).map(i => {
                return (
                    <tr key={i}>
                        <td colSpan={3}><div className="table-row-placeholder"></div></td>
                    </tr>
                );
            });
        }

        if (errorMessage) {
            return (
                <tr>
                    <td>
                        <div className="subtext no-data">{errorMessage}</div>
                    </td>
                </tr>
            );
        }

        return rankingResults.results?.sort(sortByDescendingValue).slice(0, correctedNumberOfScoresToShow).map((result) => {
            let currentValues = getValuesAndDelta(result);
            return (
                <tr key={result.entityInstance.name}>
                    <td className="metric-name">{result.entityInstance.name}</td>
                    <td className="data">{currentValues.value}</td>
                    <td className="delta"><MetricDelta delta={currentValues.deltaVal}
                        formatter={currentValues.deltaFmt}
                        downIsGood={currentValues.downIsGood} /></td>
                </tr>
            )
        });
    }

    const description = props.entityInstance ? props.descriptionText.replace(instancePlaceholder, props.entityInstance.name) : props.descriptionText;

    return (
        <TileTemplate
            description={description}
            linkText="Monitor changes"
            nextPageUrl={props.nextPageUrl}>
            <table className="scorecard-tile-table">
                <tbody>
                    {getTableContent()}
                </tbody>
            </table>
        </TileTemplate>
    );
};


export default RankingScorecard;