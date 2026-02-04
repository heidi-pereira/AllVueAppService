import React from 'react';
import { useEffect, useState } from 'react';
import MetricChangeOnPeriod from './MetricChangeOnPeriod';
import { Factory, RankingTableResult } from '../../../../BrandVueApi';
import { ViewHelper } from '../../ViewHelper';
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { NoDataError } from '../../../../NoDataError';
import { Metric } from "../../../../metrics/metric";
import { EntityInstance } from "../../../../entity/EntityInstance";
import { EntitySet } from '../../../../entity/EntitySet';
import { NumberFormattingHelper } from '../../../../helpers/NumberFormattingHelper';
import { getDescriptiveNameForAveragePeriod } from '../../../helpers/PeriodHelper';
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from 'client/state/subsetSlice';

import {selectTimeSelection} from "../../../../state/timeSelectionStateSelectors";

interface IProps {
    metric: Metric;
    nextPageUrl: string;
    entitySet: EntitySet | undefined;
    curatedFilters: CuratedFilters;
}

const MetricChangeOnPeriodContainer = ({ metric, nextPageUrl, entitySet, curatedFilters }: IProps) => {

    const [metricScore, updateMetricScore] = useState(0);
    const [metricScoreChange, updateMetricScoreChange] = useState(0);
    const [rankScore, updateRankScore] = useState(0);
    const [isJointRankScore, updateIsJointRankScore] = useState(false);
    const [rankScoreChange, updateRankScoreChange] = useState(0);
    const [rankScoreTotal, updateRankScoreTotal] = useState(0);
    const [rankingResults, updateRankingResults] = useState<RankingTableResult[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [instance, updateInstance] = useState<EntityInstance | undefined>(entitySet?.mainInstance);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    if (metric.entityCombination.length > 1) {
        throw new Error("MetricChangeOnPeriod does not support multi-entity metrics");
    }

    const getEntityInstanceIds = (): number[] => {
        if (metric.entityCombination.length === 0 || !entitySet) {
            return [];
        }
        return entitySet.getInstances().getAll().map(entityInstance => entityInstance.id);
    }

    const getTooltip = (metric: Metric, rankings: RankingTableResult[], entityInstance: EntityInstance | undefined): JSX.Element | undefined => {
        if (!entityInstance) {
            return undefined;
        }

        const rankIndex = rankings.findIndex(r => r.entityInstance.id === entityInstance.id);
        const displayedEntities: RankingTableResult[] = [];
        const indices: number[] = [0, rankIndex, rankings.length - 1]
            .filter((value, index, array) => array.indexOf(value) === index);

        indices.forEach(index => displayedEntities.push(rankings[index]));

        return (
            <div className="brandvue-tooltip">
                <div className="tooltip-header">{metric.displayName}</div>
                <div className="tooltip-header light margin-bottom">You rank {rankings[rankIndex].multipleWithCurrentRank ? "joint" : ""} {NumberFormattingHelper.getOrdinalName(rankings[rankIndex].currentRank)} out of {rankings.length}</div>
                {displayedEntities.map(rankingResult => getTooltipRow(rankingResult, entityInstance.id))}
            </div>
        );
    }

    const getTooltipRow = (rankingResult: RankingTableResult, focusEntityInstanceId: number): JSX.Element => {
        const additionalClass = rankingResult.entityInstance.id === focusEntityInstanceId ? " bold" : "";
        const entityName = rankingResult.entityInstance.id === focusEntityInstanceId ? "You" : rankingResult.entityInstance.name;
        return (
            <React.Fragment key={rankingResult.entityInstance.id}>
                <div className={`tooltip-label${additionalClass}`}>{entityName}</div>
                <div className={`tooltip-value${additionalClass}`}>{rankingResult.multipleWithCurrentRank ? "=" : ""}{NumberFormattingHelper.getOrdinalName(rankingResult.currentRank)}</div>
            </React.Fragment>
        );
    }

    useEffect(() => {
        setIsLoading(true);

        const instanceId = entitySet?.mainInstance ? entitySet.mainInstance.id : 0;
        const rankingRequest = ViewHelper.createCuratedRequestModel(getEntityInstanceIds(),
            [metric],
            curatedFilters,
            instanceId,
            {},
            subsetId,
            timeSelection
        );
            
        Factory.DataClient(throwErr => throwErr())
            .getRankedBrands(rankingRequest)
            .then(resp => {
                const result = entitySet?.mainInstance ? resp.results.find(r => r.entityInstance.id === instanceId) : resp.results[0];

                if (!result) {
                    throw new NoDataError("Insufficient data to display this metric's progress.");
                }

                const scoreChange = result.previousWeightedDailyResult.weightedResult ? result.currentWeightedDailyResult.weightedResult - result.previousWeightedDailyResult.weightedResult : 0;
                const rankChange = result.previousRank ? result.currentRank - result.previousRank : 0;

                updateMetricScore(result.currentWeightedDailyResult.weightedResult);
                updateMetricScoreChange(scoreChange);
                updateRankScore(result.currentRank);
                updateIsJointRankScore(result.multipleWithCurrentRank);
                updateRankScoreChange(rankChange);
                updateRankScoreTotal(resp.results.length);
                updateRankingResults(resp.results);
                updateInstance(entitySet?.mainInstance);
                setIsLoading(false);
            });

    }, [metric, JSON.stringify(entitySet)]);

    if (isLoading) {
        return (
            <div className="metric-change-placeholder">
                <div className="content">
                    <div className="metric-name"/>
                    <div className="change-phrase"/>
                </div>
                <div className="data">
                    <div className="score-value"/>
                    <div className="metric-delta"/>
                </div>
            </div>
        );
    }

    return <MetricChangeOnPeriod
               metricName={metric.varCode}
               metricScore={metric.fmt(metricScore)}
               metricScoreChange={metricScoreChange}
               rankScore={rankScore}
               isJointRankScore={isJointRankScore}
               rankScoreChange={rankScoreChange}
               rankScoreTotal={rankScoreTotal}
               metricUrl={nextPageUrl}
               metricDownIsGood={metric.downIsGood}
               periodName={getDescriptiveNameForAveragePeriod(curatedFilters.average)}
               deltaFormatter={metric.fmt}
               tooltipHtml={getTooltip(metric, rankingResults, instance)}/>;
};

export default MetricChangeOnPeriodContainer;