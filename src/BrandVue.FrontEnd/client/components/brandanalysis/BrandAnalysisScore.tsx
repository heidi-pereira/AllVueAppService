import React from "react";
import {
    AnalysisDataState,
    AnalysisScorecardTitle,
    AnalysisSubPageComponent,
    AnalysisSubPageEntityType,
    AnalysisSubPageRequestType,
    getDataWrapper,
    getTitleDescription,
    subPageComponentBackground,
    updateResult,
    subPageButtonLinkFromMetric,
    subPageButtonMetric
} from "../helpers/AnalysisHelper";
import {EntityInstance} from "../../entity/EntityInstance";
import {
    makeBrandAnalysisCrossbreakRequests,
    IBrandAnalysisRequestProps,
    BrandAnalysisRequestReturnType
} from "../helpers/BrandAnalysisRequestHelper";
import {CrossbreakCompetitionResults, Factory} from "../../BrandVueApi";
import {IKeyResult, keyResultFromCrossbreakResult} from "../visualisations/brandanalysis/IKeyResult";
import BrandAnalysisPercentage from "../visualisations/brandanalysis/BrandAnalysisPercentage";
import {ViewHelper} from "../visualisations/ViewHelper";
import { NoDataError } from "../../NoDataError";
import style from "./BrandAnalysis.module.less";
import { ProductConfigurationContext } from "../../ProductConfigurationContext";
import { ViewTypeEnum } from "../helpers/ViewTypeHelper";
import { useMetricStateContext } from "../../metrics/MetricStateContext";
import {useLocation} from "react-router-dom";
import { useReadVueQueryParams } from "../helpers/UrlHelper";
import { useAppSelector } from 'client/state/store';
import { selectSubsetId } from "client/state/subsetSlice";

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";
import { throwIfNullish } from "../helpers/ThrowHelper";

interface IBrandAnalysisScoreProps extends IBrandAnalysisRequestProps {
    title: AnalysisScorecardTitle,
}

const getHighestAgeScoreTalkingPoint = (title: AnalysisScorecardTitle, brandName: string, highestAgeGroup: string | null, productName: string) => {
    if (highestAgeGroup) {
        return <li key="highestAge"><strong>Highest</strong> {brandName} {getTitleDescription(title, productName).toLowerCase()} scores are given by people aged <strong>{highestAgeGroup}</strong>.</li>;
    }
    return;
}

const getHighestRegionScoreTalkingPoint = (title: AnalysisScorecardTitle, brandName: string, highestRegion: string | null, productName: string) => {
    if (highestRegion) {
        const regions: string[] = ["North", "South", "Midlands"];
        if (regions.includes(highestRegion)) {
            highestRegion = `the ${highestRegion}`;
        }
        return <li key="highestRegion">People from <strong>{highestRegion}</strong> have the <strong>highest {getTitleDescription(title, productName).toLowerCase()}</strong> for {brandName}.</li>;
    }
    return;
}

const getLowestAgeScoreTalkingPoint = (lowestAge: string | null) => {
    if (lowestAge) {
        return <li key="lowestAge"><strong>Lowest scores</strong> are from people aged <strong>{lowestAge}</strong>.</li>;
    }
    return;
    
}

const getMaxResult = (results: IKeyResult[]) => results.length > 0 ? results.reduce((a, b) => a.result?.weightedResult > b.result?.weightedResult || b.result?.weightedResult === 0 ? a : b, {} as IKeyResult).entity : null;
const getMinResult = (results: IKeyResult[]) => results.length > 0 ? results.reduce((a, b) => a.result?.weightedResult < b.result?.weightedResult || b.result?.weightedResult === 0 ? a : b, {} as IKeyResult).entity : null;

const BrandAnalysisScore: React.FunctionComponent<IBrandAnalysisScoreProps> = (props) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const metricContext = useMetricStateContext();
    const [regionState, setRegionState] = React.useState<IKeyResult[]>([]);
    const [ageState, setAgeState] = React.useState<IKeyResult[]>([]);
    const [overallPercentState, setOverallPercentState] = React.useState<number>(-1);

    const [regionDataState, setRegionDataState] = React.useState(AnalysisDataState.Loading);
    const [ageDataState, setAgeDataState] = React.useState(AnalysisDataState.Loading);
    const [overallPercentDataState, setoverallPercentStateDataState] = React.useState(AnalysisDataState.Loading);
    const {questionTypeLookup} = metricContext;
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);

    const buttonMetricPageLink = subPageButtonMetric(metricContext.enabledMetricSet, props.pageMetricConfiguration.buttonLink);
    const location = useLocation();
    React.useEffect(() => {
        
        const handleCrossbreakResults = (r: { key: AnalysisSubPageEntityType, results: CrossbreakCompetitionResults | null, returnType: BrandAnalysisRequestReturnType }) => {
            switch (r.key) {
                case AnalysisSubPageEntityType.RegionGrouped:
                    updateResult(keyResultFromCrossbreakResult(r.results), r.returnType, s => setRegionState(s), ds => setRegionDataState(ds));
                    break;
                case AnalysisSubPageEntityType.AgeGrouped:
                    updateResult(keyResultFromCrossbreakResult(r.results), r.returnType, s => setAgeState(s), ds => setAgeDataState(ds));
                    break;
            }
        }

        setRegionDataState(AnalysisDataState.Loading);
        setAgeDataState(AnalysisDataState.Loading);
        setoverallPercentStateDataState(AnalysisDataState.Loading);

        const crossbreakMetrics = props.pageMetricConfiguration.metrics.filter(x=>x.requestType == AnalysisSubPageRequestType.Crossbreak).map(x => ({
            key: x.key, metric: throwIfNullish(metricContext.enabledMetricSet.getMetric(x.metricName), `Metric ${x.metricName}`)
        }));

        makeBrandAnalysisCrossbreakRequests(props,
            questionTypeLookup,
            crossbreakMetrics,
            subsetId,
            timeSelection,
            handleCrossbreakResults);

        const requestModel = ViewHelper.createCuratedRequestModel([props.brand.id],
            [props.primaryMetric],
            props.curatedFilters,
            props.brand.id,
            {},
            subsetId,
            timeSelection
            );
        if (requestModel) {
            Factory.DataClient(err => err()).getScorecardPerformanceResults(requestModel)
                .then(r => {
                    setOverallPercentState(r.metricResults[0].periodResults[0].weightedResult);
                    setoverallPercentStateDataState(AnalysisDataState.Show);
                })
                .catch((e: any) => {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        setoverallPercentStateDataState(AnalysisDataState.NoData);
                    } else {
                        setoverallPercentStateDataState(AnalysisDataState.Error);
                        throw e;
                    }
                })
        }
    }, [
        props.activeEntitySet,
        JSON.stringify(props.curatedFilters),
        props.primaryMetric
    ]);

    const talkingPoints = (
        <div>
            <div>
                <h4>Talking points</h4>
                <ul>
                    {getDataWrapper(ageDataState, () => getHighestAgeScoreTalkingPoint(props.title, props.brand.name, getMaxResult(ageState), productConfiguration.productName))}
                    {getDataWrapper(regionDataState, () => getHighestRegionScoreTalkingPoint(props.title, props.brand.name, getMaxResult(regionState), productConfiguration.productName))}
                    {getDataWrapper(ageDataState, () => getLowestAgeScoreTalkingPoint(getMinResult(ageState)))}
                </ul>
            </div>
        </div>
    );

    const ageRowColor = "#0E4B81";
    const chartContent = (
        <div>
            <div>
                <h4>{getTitleDescription(props.title, productConfiguration.productName)} Data</h4>
            </div>
            <div>
                {getDataWrapper(overallPercentDataState, () => <BrandAnalysisPercentage group={{
                    rows: [
                        { name: "Total", value: overallPercentState, color: ageRowColor }
                    ]
                }} />)}
                {getDataWrapper(ageDataState, () => <BrandAnalysisPercentage group={{
                    title: "Age",
                    rows: ageState?.map(r => ({ name: r.entity, value: r.result.weightedResult, color: ageRowColor })) ?? [],
                }} />)}
            </div>
        </div>
    );

    const link = (title: AnalysisScorecardTitle) => {
        const buttonText = `View audience breakdown of ${(getTitleDescription(title, productConfiguration.productName))} scores`;
        const readVueQueryParams = useReadVueQueryParams();
        return buttonMetricPageLink && subPageButtonLinkFromMetric(buttonText, buttonMetricPageLink, ViewTypeEnum.Profile, location, readVueQueryParams );
    }

    return <div className={`tile ${style.brandAnalysisSubPage}`} style={subPageComponentBackground(AnalysisSubPageComponent.Score, productConfiguration.cdnAssetsEndpoint)}>
        <div className={style.cardContent}>
            <div className={style.subPageCardTitle}>
                <div className={style.titleIcon}>
                    <i className="material-symbols-outlined">bar_chart</i>
                </div>
                <div className={style.titleText}>
                    What is {props.brand.name} {getTitleDescription(props.title, productConfiguration.productName)} score?
                </div>
            </div>
            <div className={style.subPageFlexRow}>
                <div className={`${style.subPageTextContainer} ${style.equal}`}>
                    {talkingPoints}
                    <div className={`${style.subPageFlexRow} flexrow-buttons`}>
                        {link(props.title)}
                    </div>
                </div>
                <div className={`${style.subPageChartContainer} ${style.equal}`}>
                    {chartContent}
                </div>
            </div>
        </div>
    </div>;
}

export default BrandAnalysisScore;