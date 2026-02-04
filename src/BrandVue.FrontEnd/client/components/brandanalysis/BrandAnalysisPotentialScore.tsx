import React from "react";
import { AnalysisDataState, AnalysisScorecardTitle, AnalysisSubPageComponent, getDataWrapper, getTitleDescription, subPageComponentBackground, subPageButtonLinkFromMetric, subPageButtonMetric } from "../helpers/AnalysisHelper";
import { IBrandAnalysisRequestProps } from "../helpers/BrandAnalysisRequestHelper";
import { Metric } from "../../metrics/metric";
import BrandAnalysisPercentage from "../visualisations/brandanalysis/BrandAnalysisPercentage";
import { CompetitionResults, Factory } from "../../BrandVueApi";
import "../helpers/ArrayHelper"
import {ViewHelper} from "../visualisations/ViewHelper";
import {EntityInstance} from "../../entity/EntityInstance";
import { diffDescription } from "../helpers/AnalysisHelper";
import { NoDataError } from "../../NoDataError";
import Tooltip from "../Tooltip";
import style from "./BrandAnalysis.module.less";
import { ProductConfigurationContext } from "../../ProductConfigurationContext";
import { ViewTypeEnum } from "../helpers/ViewTypeHelper";
import {ArrayHelper} from "../helpers/ArrayHelper";
import {useMetricStateContext} from "../../metrics/MetricStateContext";
import {useLocation} from "react-router-dom";
import { useReadVueQueryParams } from "../helpers/UrlHelper";
import { selectSubsetId } from "client/state/subsetSlice";
import { useAppSelector } from "client/state/store";
import { selectTimeSelection } from "client/state/timeSelectionStateSelectors";

interface IBrandAnalysisPotentialScoreProps extends IBrandAnalysisRequestProps {
    title: AnalysisScorecardTitle
}

const pointText = (points: number) => {
    return `${points} ${points == 1 ? "point" : "points"}`;
}

const sectorTalkingPoint = (sectorEntitySetName: string, brandName: string, brandScore: number, bestOtherSectorBrandName: string, bestOtherSectorBrandScore: number) => {
    if (brandScore < 0 || bestOtherSectorBrandScore < 0) {
        return
    }

    const formattedBrandScore = Math.round(brandScore * 100);
    const formattedBestOtherSectorBrandScore = Math.round(bestOtherSectorBrandScore * 100);

    if (formattedBrandScore == formattedBestOtherSectorBrandScore) {
        return <li key="sector">{brandName} is the <strong>joint best</strong> brand in the {sectorEntitySetName} sector, along with <strong>{bestOtherSectorBrandName}</strong>.</li>
    }

    const diff = Math.abs(formattedBrandScore - formattedBestOtherSectorBrandScore);
    if (formattedBrandScore > formattedBestOtherSectorBrandScore) {
        return <li key="sector">{brandName} is the <strong>best</strong> brand in the {sectorEntitySetName} sector by <strong>{pointText(diff)}</strong>.</li>
    }

    const diffText = `${pointText(diff)} ${diffDescription(formattedBrandScore, formattedBestOtherSectorBrandScore)}`;
    return <li key="sector">{brandName} is <strong>{diffText}</strong> the best brand in the {sectorEntitySetName} sector, <strong>{bestOtherSectorBrandName}</strong>.</li>
}

const pointsDiffVsCompetitorSetAverageTalkingPoint = (brandName: string, brandScore: number, competitorSetAverageScore: number) => {
    if (brandScore < 0 || competitorSetAverageScore < 0) {
        return
    }

    const formattedBrandScore = Math.round(brandScore * 100);
    const formattedCompetitorSetAverageScore = Math.round(competitorSetAverageScore * 100);

    const diff = Math.abs(formattedBrandScore - formattedCompetitorSetAverageScore);
    const suffix = <> by<strong> {pointText(diff)}</strong></>;

    return <li key="pointsDiff">{brandName} is <strong>{diffDescription(formattedBrandScore, formattedCompetitorSetAverageScore)}</strong> the competitor average{diff != 0 ? suffix : ""}.</li>;
}

const bestCompetitorBrandVsSelectedBrandTalkingPoint = (brandScore: number, bestOtherCompetitorBrandName: string, bestOtherCompetitorBrandScore: number) => {
    if (brandScore < 0 || bestOtherCompetitorBrandScore < 0) {
        return
    }

    const formattedBrandScore = Math.round(brandScore * 100);
    const formattedBestOtherCompetitorBrandScore = Math.round(bestOtherCompetitorBrandScore * 100);

    const diff = formattedBrandScore - formattedBestOtherCompetitorBrandScore;
    const diffDesc = diffDescription(formattedBestOtherCompetitorBrandScore, formattedBrandScore);
    const suffix = <>, {diffDesc.replace(" of", "")} by <strong>{pointText(Math.abs(diff))}</strong></>;

    return <li key="bestVs">The {diff > 0 ? "next " : ""}{diff == 0 ? "joint " : ""}best brand in the competitor set is <strong>{bestOtherCompetitorBrandName}</strong>{diff != 0 ? suffix : ""}.</li>;
}

const link = (title: AnalysisScorecardTitle, metric: Metric, productName: string) => {
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();
    const buttonText = `Compare competitors ${(getTitleDescription(title, productName))} scores`;
    return subPageButtonLinkFromMetric(buttonText, metric, ViewTypeEnum.Competition, location, readVueQueryParams);
}

const BrandAnalysisPotentialScore: React.FunctionComponent<IBrandAnalysisPotentialScoreProps> = (props) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const metricContext = useMetricStateContext();
    const [competitorState, setCompetitorState] = React.useState<{ leadingBrand: EntityInstance | null, value:number | null, average: number } | null>();
    const [sectorState, setSectorState] = React.useState<{ leadingBrand: EntityInstance | null, value:number | null, average: number } | null>();
    const [overallPercentState, setOverallPercentState] = React.useState<number>(-1);
    const [competitorDataState, setCompetitorDataState] = React.useState(AnalysisDataState.Loading);
    const [sectorDataState, setSectorDataState] = React.useState(AnalysisDataState.Loading);
    const [sectorEntitySetName, setSectorEntitySetName] = React.useState("");
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const averageColor = "#696969";

    function resultsToBarProps(r: CompetitionResults) {
        const nonPrimary = r.periodResults[0].resultsPerEntity
            .filter(e => e.entityInstance.id != props.brand.id);
        const winner = nonPrimary.length > 0 ? ArrayHelper.maxBy(nonPrimary, e => e.weightedDailyResults[0].weightedResult) : null;
        // This is a copy of code in IntermediateWeightedResultsExtensions https://github.com/Savanta-Tech/Vue/blob/86b32576c5b8cb7f4464ebf90685b0a46cf6289d/src/BrandVue.SourceData/CalculationPipeline/IntermediateWeightedResultsExtensions.cs?plain=1#L261C44-L261C66
        // In future iteration, please aim to make an API call to calculate averages instead of reimplementing in the front end like this
        const resultsWithSample = r.periodResults[0].resultsPerEntity.map(e => e.weightedDailyResults[0]).filter(r => r.unweightedSampleSize > 0);
        const average = ArrayHelper.sumBy(resultsWithSample, e => e.weightedResult) / resultsWithSample.length;
        return {
            average: average,
            value: winner ? winner.weightedDailyResults[0].weightedResult : null,
            leadingBrand: winner ? EntityInstance.convertInstanceFromApi(winner.entityInstance) : null
        };
    }

    const handleResults = (r : CompetitionResults) => {
        setOverallPercentState(r.periodResults[0].resultsPerEntity
            .filter(e=>e.entityInstance.id == props.brand.id)[0].weightedDailyResults[0].weightedResult);
        setCompetitorState(resultsToBarProps(r));
        setCompetitorDataState(AnalysisDataState.Show);
    }

    const handleSectorResults = (r : CompetitionResults) => {
        setSectorState(resultsToBarProps(r));
        setSectorDataState(AnalysisDataState.Show);
    }

    const buttonMetricPageLink = subPageButtonMetric(metricContext.enabledMetricSet, props.pageMetricConfiguration.buttonLink);

    React.useEffect(() => {
        setCompetitorDataState(AnalysisDataState.Loading);
        setSectorDataState(AnalysisDataState.Loading);

        const requestModel = ViewHelper.createMultiEntityRequestModel({
            curatedFilters: props.curatedFilters,
            metric: props.primaryMetric,
            splitBySet: props.activeEntitySet,
            filterInstances: [],
            continuousPeriod: false,
            focusEntityId: props.brand.id,
            subsetId: subsetId
        }, timeSelection);
        if (requestModel) {
            Factory.DataClientWithNoHandler(err => err()).getCompetitionResults(requestModel)
                .then(r => handleResults(r))
                .catch((e: any) => {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        setCompetitorDataState(AnalysisDataState.NoData);
                    } else {
                        setCompetitorDataState(AnalysisDataState.Error);
                        throw e;
                    }
                })
        }

        const sectorEntitySet = ArrayHelper.maxBy(props.entityConfiguration.getSetsFor(props.activeEntitySet.type).filter(x => x.isSectorSet), e => e.getInstances().getAll().length);
        setSectorEntitySetName(sectorEntitySet.name);
        //we may change this later, but for now we use the closest to a "whole market" sector set.
        const sectorRequestModel = ViewHelper.createMultiEntityRequestModel({
            curatedFilters: props.curatedFilters,
            metric: props.primaryMetric,
            splitBySet: sectorEntitySet,
            filterInstances: [],
            continuousPeriod: false,
            focusEntityId: props.brand.id,
            subsetId: subsetId
        }, timeSelection);

        if (sectorRequestModel) {
            Factory.DataClientWithNoHandler(throwError => throwError()).getCompetitionResults(sectorRequestModel)
                .then(r => handleSectorResults(r))
                .catch((e: any) => {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        setSectorDataState(AnalysisDataState.NoData);
                    } else {
                        setSectorDataState(AnalysisDataState.Error);
                        throw e;
                    }
                })
        }
    }, [
        props.activeEntitySet,
        JSON.stringify(props.curatedFilters),
        props.primaryMetric
    ]);
        
    const chartContent = (brand: EntityInstance) => {
        return (
            <div>
                <div>
                    <h4>Sector & competitor benchmarks</h4>
                </div>
                {getDataWrapper(competitorDataState, () => <BrandAnalysisPercentage group={{
                    rows: [
                        { name: props.brand.name, value: overallPercentState, color: props.activeEntitySet.getInstanceColor(props.brand) }
                    ]
                }} />) }
                {getDataWrapper(sectorDataState, () => <BrandAnalysisPercentage group={{
                    title: "Sector",
                    rows: [
                        { name: sectorState?.leadingBrand?.name ?? "", value: sectorState?.value ?? null, color: props.activeEntitySet.getInstanceColor(sectorState?.leadingBrand ?? new EntityInstance()) },
                        { name: sectorState != null ? "Average" : "", value: sectorState?.average ?? null, color: averageColor }
                    ],
                }} />) }
                {getDataWrapper(competitorDataState, () => <BrandAnalysisPercentage group={{
                    title: "Competitors",
                    rows: [
                        { name: competitorState?.leadingBrand?.name ?? "", value: competitorState?.value ?? null, color: props.activeEntitySet.getInstanceColor(competitorState?.leadingBrand ?? new EntityInstance()) },
                        { name: competitorState != null ? "Average" : "", value: competitorState?.average ?? null, color: averageColor }
                    ],
                }} />)}
                <div className={`${style.toolTip} not-exported`}>
                    <Tooltip placement="top-start" title={`Sector "${sectorEntitySetName}" is the largest sector containing ${brand.name}`}>
                        <div className={style.toolTipContent}>
                            <i className="material-symbols-outlined">help</i>
                            What is {brand.name} sector set?
                        </div>
                    </Tooltip>
                </div>
            </div>
        );
    }

    const talkingPoints = (
        <div>
            <h4>Talking points</h4>
            <ul>
                {getDataWrapper(sectorDataState, () => sectorTalkingPoint(sectorEntitySetName, props.brand.name, overallPercentState, sectorState?.leadingBrand?.name ?? "", sectorState?.value ?? -1))}
                {getDataWrapper(competitorDataState, () => pointsDiffVsCompetitorSetAverageTalkingPoint(props.brand.name, overallPercentState, competitorState?.average ?? -1))}
                {getDataWrapper(competitorDataState, () => bestCompetitorBrandVsSelectedBrandTalkingPoint(overallPercentState, competitorState?.leadingBrand?.name ?? "", competitorState?.value ?? -1))}
            </ul>
        </div>
    );

    return <div className={`tile ${style.brandAnalysisSubPage}`} style={subPageComponentBackground(AnalysisSubPageComponent.PotentialScore, productConfiguration.cdnAssetsEndpoint)}>
        <div className={style.cardContent}>
            <div className={style.subPageCardTitle}>
                <div className={style.titleIcon}>
                    <i className="material-icons">emoji_events</i>
                </div>
                <div className={style.titleText}>What should {props.brand.name} {getTitleDescription(props.title, productConfiguration.productName)} score be?</div>
            </div>
            <div className={style.subPageFlexRow}>
                <div className={`${style.subPageTextContainer} ${style.equal}`}>
                    {talkingPoints}
                    {buttonMetricPageLink && link(props.title, buttonMetricPageLink, productConfiguration.productName)}
                </div>
                <div className={`${style.subPageChartContainer} ${style.equal}`}>
                    {chartContent(props.brand)}
                </div>
            </div>
        </div>
    </div>;
}

export default BrandAnalysisPotentialScore;
