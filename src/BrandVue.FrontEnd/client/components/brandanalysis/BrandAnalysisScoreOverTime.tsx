import React from "react";
import {
    AnalysisDataState,
    AnalysisScorecardTitle,
    AnalysisSubPageComponent,
    diffDescription,
    getDataWrapper,
    getTitleDescription,
    IButtonOptions,
    subPageButtonLinkFromMetric,
    subPageButtonMetric,
    subPageComponentBackground
} from "../helpers/AnalysisHelper";
import {EntitySet} from "../../entity/EntitySet";
import DashBox, {legendPosition} from "../visualisations/DashBox";
import {IBrandAnalysisRequestProps} from "../helpers/BrandAnalysisRequestHelper";
import {NumberFormattingHelper} from "../../helpers/NumberFormattingHelper";
import {MetricResultsSummary} from "../helpers/MetricInsightsHelper";
import * as moment from "moment";
import { IGoogleTagManager } from "../../googleTagManager";
import style from "./BrandAnalysis.module.less";
import {ProductConfigurationContext} from "../../ProductConfigurationContext";
import {CuratedFilters} from "../../filter/CuratedFilters";
import {filterSet} from "../../filter/filterSet";
import {ViewTypeEnum} from "../helpers/ViewTypeHelper";
import {EntityInstance} from "../../entity/EntityInstance";
import {IEntityConfiguration} from "../../entity/EntityConfiguration";
import {useMetricStateContext} from "../../metrics/MetricStateContext";
import {useLocation} from "react-router-dom";
import { useReadVueQueryParams } from "../helpers/UrlHelper";

interface IBrandAnalysisScoreOverTimeProps extends IBrandAnalysisRequestProps {
    googleTagManager: IGoogleTagManager,
    title: AnalysisScorecardTitle,
    updateMetricResultsSummary,
    updateAverageRequests,
    availableEntitySets: EntitySet[],
    partId: number,
}

const getSixtyDaysComparisonTalkingPoint = (brandName: string, brandScore: number, previousBrandScore: number) => {
    if (brandScore < 0 || previousBrandScore < 0) {
        return;
    }
    return <li key="sixtyDayComparison">{brandName} is <strong>{diffDescription(brandScore, previousBrandScore)}</strong> where it was <strong>60 days ago</strong>.</li>
};

const getLastYearComparisonTalkingPoint = (brandName: string, brandScore: number, previousBrandScore: number) => {
    if (brandScore < 0 || previousBrandScore < 0) {
        return;
    }
    return <li key="lastYearComparison">{brandName} is <strong>{diffDescription(brandScore, previousBrandScore)}</strong> where it was <strong>this time last year</strong>.</li>
};

const getBestScoreTalkingPoint = (brandName: string, brandScore: number, bestBrandScore: { score: number, period: string }) => {
    if (brandScore < 0 || bestBrandScore.score < 0) {
        return;
    }
    return <li key="bestScoreComparison"><strong>{brandName}{brandName.slice(-1) == "s" ? "" : "'s"} best score</strong> was in <strong>{bestBrandScore.period}</strong> where it was <strong>{NumberFormattingHelper.formatPercentage0Dp(bestBrandScore.score)}</strong>.</li>
};

const getStartDateFromEndDate = (endDate: Date): Date => new Date(endDate.getFullYear(), endDate.getMonth() - 11, 0);

const getFixedDateFilter = (curatedFilters: CuratedFilters, entityConfiguration: IEntityConfiguration): CuratedFilters => {
    const filters = new filterSet();
    filters.filters = [];
    const fixedCuratedFilter = new CuratedFilters(filters, entityConfiguration, [], curatedFilters); 
    fixedCuratedFilter.setDates(getStartDateFromEndDate(curatedFilters.endDate), curatedFilters.endDate);
    return fixedCuratedFilter;
};

const getFixedEntitySet = (entitySet: EntitySet, mainBrand: EntityInstance): EntitySet => {
    const fixedEntitySet = entitySet.cloneSet(entitySet.getInstances().clone());
    fixedEntitySet.getInstances().addInstance(mainBrand);
    fixedEntitySet.mainInstance = mainBrand;
    return fixedEntitySet;
};

const BrandAnalysisScoreOverTime: React.FunctionComponent<IBrandAnalysisScoreOverTimeProps> = (props) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const metricContext = useMetricStateContext();
    const [chartDataState, setChartDataState] = React.useState(AnalysisDataState.Loading);
    const [talkingPointState, setTalkingPointState] = React.useState(AnalysisDataState.Loading);
    const [brandCurrentScore, setBrandCurrentScore] = React.useState<number>(-1);
    const [sixtyDayPreviousBrandScore, setSixtyDayPreviousBrandScore] = React.useState<number>(-1);
    const [lastYearPreviousBrandScore, setLastYearPreviousBrandScore] = React.useState<number>(-1);
    const [brandBestScore, setBrandBestScore] = React.useState<{ score: number, period: string }>({score: -1, period: ""});

    const talkingPoints = (
        <div>
            <div>
                <h4>Talking points</h4>
                <ul>
                    {getDataWrapper(talkingPointState, () => getSixtyDaysComparisonTalkingPoint(props.brand.name, brandCurrentScore, sixtyDayPreviousBrandScore))}
                    {getDataWrapper(talkingPointState, () => getLastYearComparisonTalkingPoint(props.brand.name, brandCurrentScore, lastYearPreviousBrandScore))}
                    {getDataWrapper(talkingPointState, () => getBestScoreTalkingPoint(props.brand.name, brandCurrentScore, brandBestScore))}
                </ul>
            </div>
        </div>
    );

    const updateBrandAnalysisResultsSummary = (metricResultsSummary: MetricResultsSummary) => {
        if (!metricResultsSummary || !metricResultsSummary.results[0]) {
            return;
        }
        const weightedResults = metricResultsSummary?.results[0]?.weightedDailyResults;
        const currentMonthIndex = weightedResults.length - 1;
        if (weightedResults[currentMonthIndex]) {
            setBrandCurrentScore(weightedResults[currentMonthIndex].weightedResult)
        }
        if (weightedResults[currentMonthIndex - 2]) {
            setSixtyDayPreviousBrandScore(weightedResults[currentMonthIndex - 2]?.weightedResult)
        }
        if (weightedResults[currentMonthIndex - 12]) {
            setLastYearPreviousBrandScore(weightedResults[currentMonthIndex - 12]?.weightedResult)
        }
        const maxScore = weightedResults.reduce((a, b) => a.weightedResult > b.weightedResult ? a : b);
        if (maxScore) {
            setBrandBestScore({ score: maxScore.weightedResult, period: moment.default(maxScore.date).format("MMM yyyy") })
        }
        props.updateMetricResultsSummary(metricResultsSummary);
    }

    const buttonMetricPageLink = subPageButtonMetric(metricContext.enabledMetricSet, props.pageMetricConfiguration.buttonLink);

    const getChartContent = () => {
        return (<div className={"analysis-chart-content"}>
            <div>
                <h4>Trend over the last year</h4>
            </div>
            <div>
                <DashBox
                    googleTagManager={props.googleTagManager}
                    activeBrand={props.brand}
                    height={200}
                    metrics={[props.primaryMetric]}
                    partId={props.partId}
                    curatedFilters={getFixedDateFilter(props.curatedFilters, props.entityConfiguration)}
                    legendPosition={legendPosition.Bottom}
                    entitySet={getFixedEntitySet(props.activeEntitySet, props.brand)}
                    availableEntitySets={props.availableEntitySets}
                    updateMetricResultsSummary={updateBrandAnalysisResultsSummary}
                    updateAverageRequests={props.updateAverageRequests}
                    hideFooter={true}
                    showArea={true}
                    showFocusInstanceOnly={true}
                    onSuccess={() => setTalkingPointState(AnalysisDataState.Show)}
                    onFailure={() => setTalkingPointState(AnalysisDataState.Error)}
                    onNoData={() => setTalkingPointState(AnalysisDataState.NoData)}
                />
            </div>
        </div>)
    };

    const link = (title: AnalysisScorecardTitle) => {
        const location = useLocation();
        const readVueQueryParams = useReadVueQueryParams();
        const buttonText = `Explore ${(getTitleDescription(title, productConfiguration.productName))} trends over time`;
        const buttonOptions: IButtonOptions = {
            focusInstance: props.brand,
            entitySetInstances: props.activeEntitySet.getInstances().clone().getAll()
        };
        return buttonMetricPageLink && 
            subPageButtonLinkFromMetric(buttonText, buttonMetricPageLink, ViewTypeEnum.OverTime, location, readVueQueryParams, buttonOptions);
    }

    React.useEffect(() => {
        setChartDataState(AnalysisDataState.Loading);
    }, [
        props.activeEntitySet,
        JSON.stringify(props.curatedFilters),
        props.primaryMetric
    ]);

    return <div className={`tile ${style.brandAnalysisSubPage}`} style={subPageComponentBackground(AnalysisSubPageComponent.ScoreOverTime, productConfiguration.cdnAssetsEndpoint)}>
        <div className={style.cardContent}>
            <div className={style.subPageCardTitle}>
                <div className={style.titleIcon}>
                    <i className="material-symbols-outlined">trending_up</i>
                </div>
                <div className={style.titleText}>How is {props.brand.name} {getTitleDescription(props.title, productConfiguration.productName)} score changing over time?</div>
            </div>
            <div className={style.subPageFlexRow}>
                <div className={`${style.subPageTextContainer} ${style.equal}`}>
                    {talkingPoints}
                    {link(props.title)}
                </div>
                <div className={`${style.subPageChartContainer} ${style.equal}`}>
                    {getDataWrapper(chartDataState, () => getChartContent(), { showThrobber: false })}
                </div>
            </div>
        </div>
    </div>;
}

export default BrandAnalysisScoreOverTime;