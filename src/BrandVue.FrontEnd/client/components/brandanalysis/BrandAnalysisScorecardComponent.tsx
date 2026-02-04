import React from "react";
import {
    Factory,
    ScorecardPerformanceCompetitorResults,
    ScorecardPerformanceResults
} from "../../BrandVueApi";
import { EntityInstance } from "../../entity/EntityInstance";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { NumberFormattingHelper } from "../../helpers/NumberFormattingHelper";
import { Metric } from "../../metrics/metric";
import { NoDataError } from "../../NoDataError";
import {
    analyse,
    AnalysisDataState,
    AnalysisScorecardTitle,
    getDataWrapper,
    getTitleDescription,
    getTitle
} from "../helpers/AnalysisHelper";
import { ViewHelper } from "../visualisations/ViewHelper";
import style from "./BrandAnalysis.module.less";
import CardRoundelComponent from "../CardRoundelComponent";
import { EntitySet } from "../../entity/EntitySet";
import { ProductConfigurationContext } from "../../ProductConfigurationContext";
import { isBarometer } from "../helpers/FeaturesHelper";
import { useAppSelector } from "../../state/store";
import { selectSubsetId } from "../../state/subsetSlice";

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

interface IBrandAnalysisScorecardComponentProps {
    title: AnalysisScorecardTitle,
    brand: EntityInstance,
    entitySet: EntitySet,
    metric: Metric,
    curatedFilters: CuratedFilters,
    useTitleDescriptor?: boolean,
    pastTenseVerb: string | null,
    trackLowSample: boolean,
    linkButton?: JSX.Element,
    helpText:string,
    subPage:boolean,
}

const BrandAnalysisScorecardComponent: React.FunctionComponent<IBrandAnalysisScorecardComponentProps> = (props) => {
    const [percentage, setPercentage] = React.useState<number | null>(null);
    const [competitorPercentage, setCompetitorPercentage] = React.useState<number | null>(null);
    const [dataState, setDataState] = React.useState(AnalysisDataState.Loading);
    const {productConfiguration} = React.useContext(ProductConfigurationContext);
    const analysis = analyse(percentage! * 100, props.title, props.brand.name, props.pastTenseVerb);
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const getMetricData = () => {
        setDataState(AnalysisDataState.Loading);
        const requestModel = ViewHelper.createCuratedRequestModel(
            [props.brand.id],
            [props.metric],
            props.curatedFilters,
            props.brand.id,
            {},
            subsetId,
            timeSelection
        );
        if (requestModel) {
            const dataClient = props.trackLowSample ? Factory.DataClient(err => err()) : Factory.DataClientWithNoHandler(err => err());
            const competitorsModel = ViewHelper.createCuratedRequestModel(
                props.entitySet.getInstances().getAll().map(b => b.id),
                [props.metric],
                props.curatedFilters,
                props.brand.id,
                {},
                subsetId,
                timeSelection
            );
            Promise.all(
                [
                    dataClient.getScorecardPerformanceResults(requestModel),
                    dataClient.getScorecardPerformanceResultsAverage(competitorsModel)
                ])
                .then(r => {
                    setPercentage(r[0].metricResults[0].periodResults[0].weightedResult)
                    setCompetitorPercentage(r[1].metricResults[0].competitorAverage);
                    setDataState(AnalysisDataState.Show);
                })
                .catch((e: any) => {
                    if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                        setDataState(AnalysisDataState.NoData);
                    } else {
                        setDataState(AnalysisDataState.Error);
                        throw e;
                    }
                });
        }
    }

    React.useEffect(() => {
        getMetricData();
    }, [
        JSON.stringify(props.curatedFilters),
        props.brand
    ]);

    const getDisplayTitle = () => {
        if (props.useTitleDescriptor) {
            return getTitleDescription(props.title, productConfiguration.productName);
        }
        return getTitle(props.title, productConfiguration.productName);
    };

    const getCardContent = () => {
        const difference = percentage! - competitorPercentage!;
        const aboveText = Math.round(100 * (percentage ?? 0)) == Math.round(100 * (competitorPercentage ?? 0)) ?
            "Equal to " : (percentage! > competitorPercentage! ?
                `${NumberFormattingHelper.formatPercentage0Dp(difference)} above` :
                `${NumberFormattingHelper.formatPercentage0Dp(-difference)} below`);
        const isGood = (percentage! < competitorPercentage!) == props.metric.downIsGood;
        const aboveStyle = isGood ? style.good : style.bad;
        return <>
            <CardRoundelComponent
                content={analysis}
                value={NumberFormattingHelper.formatPercentage0Dp(percentage)}
                title={getDisplayTitle()}
                isBarometer={isBarometer(productConfiguration)}
                isSubPage={props.subPage}
            />
            <div className={`${style.scorecardComponent} ${style.evenJustified}`}>
                {percentage !== null ?
                    <div className={style.cardAdditionalText} role="sub-text">
                        <span className={aboveStyle} data-test-id={isGood ? "good": null}>{aboveText} </span> {props.entitySet.name} average
                    </div>
                    : null}
                {props.linkButton}
            </div>
            <div className={style.cardAdditionalText}>
                <span>{props.helpText}</span>
            </div>
        </>
    };

    return <>
        {getDataWrapper(dataState, getCardContent, {
            twoThrobbers: true,
            withRoundel: true
        })}
    </>;
}

export default BrandAnalysisScorecardComponent;