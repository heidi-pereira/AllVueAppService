import React from 'react';
import { Link } from 'react-router-dom';
import MetricDelta from "../../MetricDelta";
import RankDelta from "../../RankDelta";
import { NumberFormattingHelper } from '../../../../helpers/NumberFormattingHelper';
import Tooltip from "../../../Tooltip";

interface IProps {
    metricName: string;
    metricScore: string;
    metricScoreChange: number;
    rankScore: number;
    isJointRankScore: boolean;
    rankScoreChange: number;
    rankScoreTotal: number;
    metricUrl: string;
    metricDownIsGood: boolean;
    periodName: string;
    tooltipHtml: JSX.Element | undefined;
    deltaFormatter: (value: number) => string;
}

interface IScoreChangeParams {
    cssClass: string,
    phrase: string,
}

const getScoreChangeParams = (metricScoreChange: number, downIsGood: boolean, deltaFormatter: (value: number) => string) : IScoreChangeParams => {

    if (deltaFormatter(Math.abs(metricScoreChange)) === deltaFormatter(0)) {
        return {
            cssClass: "change-none",
            phrase: "not changed",
        };
    }

    if (metricScoreChange < 0) {
        return  {
            cssClass: downIsGood ? "change-pos" : "change-neg",
            phrase: "gone down",
        };
    }

    return {
        cssClass: downIsGood ? "change-neg" : "change-pos",
        phrase: "gone up",
    };
}

const MetricChangeOnPeriod = ({ metricName, metricScore, metricScoreChange, rankScore, rankScoreChange, isJointRankScore, rankScoreTotal, metricUrl, metricDownIsGood, periodName, tooltipHtml, deltaFormatter }: IProps) => {
    const scoreChangeParams = getScoreChangeParams(metricScoreChange, metricDownIsGood, deltaFormatter);

    const cardContent = <Link to={metricUrl} className="metric-change">
        <div className="content">
            <span className={scoreChangeParams.cssClass}>{metricName}</span> has {scoreChangeParams.phrase} this {periodName.toLowerCase()}
        </div>
        <div className="data">
            <div className="score-container">
                <div className="score-value">{metricScore}</div>
                <MetricDelta delta={metricScoreChange} formatter={deltaFormatter} downIsGood={metricDownIsGood} />
            </div>
            { rankScoreTotal > 1 && <div className="rank-container">
                <span className="rank-subtext">Rank </span>
                {isJointRankScore ? "=" : "" }
                {NumberFormattingHelper.getOrdinalName(rankScore)}
                <span className="rank-subtext"> / {rankScoreTotal}</span>
                <RankDelta delta={rankScoreChange} downIsGood={metricDownIsGood} />
            </div> }
        </div>
    </Link>;

    if (!tooltipHtml) {
        return cardContent;
    }

    return (
        <Tooltip placement="top" title={tooltipHtml} >
            {cardContent}
        </Tooltip>
    );
};

export default MetricChangeOnPeriod;