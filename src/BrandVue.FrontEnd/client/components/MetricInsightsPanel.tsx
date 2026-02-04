import React from "react";
import { Metric } from "../metrics/metric";
import { EntitySet } from "../entity/EntitySet";
import RankDelta from "./visualisations/RankDelta";
import { MetricResultsSummary } from "./helpers/MetricInsightsHelper";
import { ViewType, ViewTypeEnum } from "./helpers/ViewTypeHelper";
import style from "./MetricInsights.module.less";

interface IMetricInsightsPanelProps {
    metric: Metric;
    activeEntitySet: EntitySet;
    metricResultsSummary: MetricResultsSummary | undefined;
    viewType: ViewType;
}

type MetricInsight = {
    icon: JSX.Element,
    text: JSX.Element,
    key: string,
}

type MetricInsightsCardContent = {
    title: string,
    key: string,
    insights: MetricInsight[],
}

const MetricInsightsPanel: React.FunctionComponent<IMetricInsightsPanelProps> = (props) => {

    const getHowAreWeDoingContent = (metric: Metric, activeEntitySet: EntitySet, metricResult: MetricResultsSummary): MetricInsightsCardContent => {
        const maxDatePeriod = metricResult.results.map(r => r.weightedDailyResults[0].date).sort((a,b) => b.getTime() - a.getTime())[0];
        const currentPeriodResults = metricResult.results.filter(r => r.weightedDailyResults[0].date.valueOf() === maxDatePeriod.valueOf());
        
        const mainEntityInstance = activeEntitySet.mainInstance;
        const resultValues = currentPeriodResults.map(r => r.weightedDailyResults[0].weightedResult);
        const average = resultValues.reduce((accumulator, value) => accumulator + value, 0)/resultValues.length;

        const mainInstanceResults = mainEntityInstance && currentPeriodResults.find(r => r.entityInstance.id === mainEntityInstance.id);
        const mainInstanceValue = mainInstanceResults!.weightedDailyResults[0].weightedResult;
        const performanceVsAverage =
            `${mainInstanceValue < average ? "below " : mainInstanceValue > average && "above "}average`;

        const sortedResults = currentPeriodResults.sort((a, b) => b.weightedDailyResults[0].weightedResult - a.weightedDailyResults[0].weightedResult);

        const topPerformer = sortedResults[0];
        const topPerformerResult = metric.fmt(topPerformer.weightedDailyResults[0].weightedResult);

        return {
            title: "How are we doing?",
            insights: [
                {
                    icon: <RankDelta delta={average - mainInstanceValue} downIsGood={metric.downIsGood} />,
                    text:
                        <p>{mainEntityInstance?.name} {metric.name} is <strong>{performanceVsAverage}</strong> for the group of competitors.</p>,
                    key:  "rank"
                },
                {
                    icon: <span><i className={`material-icons-round ${style.gold}`}>emoji_events</i></span>,
                    text:
                        <p>The <strong>top performer</strong> in this group is <strong>{topPerformer.entityInstance.name}</strong> who have <strong>{topPerformerResult}</strong>.</p>,
                    key:"events"
                }
            ],
            key: "howAreWeDoing"
        };
    }

    const getInsight = (insight: MetricInsight) => {
        return (
            <div key={insight.key}  className={style.metricInsight}>
                <div className={style.metricInsightIcon}>
                    {insight.icon}
                </div>
                <div className={style.metricInsightText}>
                    <div>{insight.text}</div>
                </div>
            </div>
        );
    }

    const metricInsightsCard = (content: MetricInsightsCardContent) => {
        return (
            <div key={content.key} className={style.metricInsightsCard}>
                <div className={style.metricInsightsCardTitle}>
                    <h5>{content.title}</h5>
                </div>
                <div className={style.metricInsightsCardInsightList}>
                    {content.insights.map(getInsight)}
                </div>
            </div>
        );
    }

    const getMetricInsightsCards = (metric: Metric, activeEntitySet: EntitySet, metricResult: MetricResultsSummary, viewType: ViewType) => {
        let cardsToBuild: MetricInsightsCardContent[] = [];

        switch(viewType.id) {
            case ViewTypeEnum.Competition:
            case ViewTypeEnum.Ranking:
            case ViewTypeEnum.OverTime:
                if (activeEntitySet.mainInstance) {
                    cardsToBuild = [getHowAreWeDoingContent(metric, activeEntitySet, metricResult)];
                }
                break;
        }

        return cardsToBuild.map(metricInsightsCard);
    };

    return (
            <div className={style.metricInsightsCardList}>
            {props.metricResultsSummary && getMetricInsightsCards(props.metric, props.activeEntitySet, props.metricResultsSummary, props.viewType)}
            </div>
    );
}

export default MetricInsightsPanel;