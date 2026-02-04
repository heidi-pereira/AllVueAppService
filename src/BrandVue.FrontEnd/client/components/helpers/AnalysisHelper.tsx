import React, {ReactNode} from "react";
import { getPercentageDescriptor } from "./PercentageDescriptorHelper";
import { getUrlForPageName, getUrlForMetricOrPageDisplayName } from "./PagesHelper";
import { IKeyResult } from "../visualisations/brandanalysis/IKeyResult";
import { BrandAnalysisRequestReturnType } from "./BrandAnalysisRequestHelper";
import { Metric } from "../../metrics/metric";
import style from "../brandanalysis/BrandAnalysis.module.less";
import helperStyles from "./AnaysisHelper.module.less";
import { ViewTypeEnum } from "./ViewTypeHelper";
import { IReadVueQueryParams } from "./UrlHelper";
import { EntityInstance } from "../../entity/EntityInstance";
import { Location } from "react-router-dom";
import { MetricSet } from "client/metrics/metricSet";

export enum AnalysisScorecardTitle {
    Advocacy,
    Buzz,
    Usage,
    Love
}

export const getTitleDescription = (title: AnalysisScorecardTitle, productName: string) => {
    if (title == AnalysisScorecardTitle.Buzz) {
        return "Positive Buzz";
    } else if (title == AnalysisScorecardTitle.Love) {
        return "Brand Love";
    } 
    return getTitle(title, productName);
}

export const getTitle = (title: AnalysisScorecardTitle, productName: string) => {
    if (title == AnalysisScorecardTitle.Usage && productName == "charities") {
        return "Support";
    }
    return AnalysisScorecardTitle[title];
}

export enum AnalysisSubPageComponent {
    Scorecard,
    Score,
    PotentialScore,
    ScoreOverTime,
    WhereNext
}

export enum AnalysisDataState {
    Loading,
    NoData,
    Error,
    Show,
}

export enum AnalysisSubPageEntityType {
    RegionGrouped = "region",
    AgeGrouped = "age"
}
export enum AnalysisSubPageRequestType {
    Crossbreak = "crossbreak",
    ScorecardPerformance = "scorecardPerformance"
}

export type ScoreCurrentVsPrevious = {
    instanceName: string,
    currentScore: number,
    previousScore: number,
    diff: number
}

export const analyse = (percentage: number, title: AnalysisScorecardTitle, brand: string, pastTenseVerb: string | null): JSX.Element => {
    const analysis = getPercentageDescriptor(percentage);
    switch (title) {
        case AnalysisScorecardTitle.Advocacy:
            return <span><strong>{analysis}</strong> people who <strong>{pastTenseVerb ?? "have visited or ordered from"}</strong>  {brand} would <strong>positively promote {brand}</strong>.</span>;
        case AnalysisScorecardTitle.Buzz:
            return <span><strong>{analysis}</strong> people from the general public <strong>have heard something positive</strong> about {brand} in the last month.</span>;
        case AnalysisScorecardTitle.Usage:
            return <span><strong>{analysis}</strong> people from the general public <strong>{pastTenseVerb ?? "have visited or ordered from"}</strong> {brand} in the <strong>last 12 months</strong>.</span>;
        case AnalysisScorecardTitle.Love:
            return <span><strong>{analysis}</strong> people who know {brand} say that they <strong>love {brand}</strong>.</span>;
    }
    return <></>;
}
export const buttonText = (title: AnalysisScorecardTitle, productName: string): string => {
    switch (title) {
        case AnalysisScorecardTitle.Advocacy:
            return "Dive into advocacy";
        case AnalysisScorecardTitle.Buzz:
            return "Breakdown buzz";
        case AnalysisScorecardTitle.Usage:
            return `Analyse ${productName == "charities" ? "support" : "usage"}`;
        case AnalysisScorecardTitle.Love:
            return "Explore love";
        default:
            return "";
    }
}

export const buttonLink = (title: AnalysisScorecardTitle, basePath: string, location: Location, readVueQueryParams: IReadVueQueryParams): string => {
    return `${basePath}/ui${getUrlForPageName(`Brand Analysis ${AnalysisScorecardTitle[title]}`, location, readVueQueryParams, {} )}`;
}

export const button = (url: string, buttonText: string, action?: (url: string) => string) =>
    <div className={`${style.button} not-exported`}>
        <a href={action ? action(url) : url} className="hollow-button">
            {buttonText}
        </a>
    </div>;

export const subPageButtonMetric = (metrics: MetricSet, buttonLinkMetricName: string) => {
    return metrics.getMetric(buttonLinkMetricName);
}

export interface IButtonOptions {
    buttonAction?: (url: string) => string;
    focusInstance?: EntityInstance;
    entitySetInstances?: EntityInstance[];
}

export const subPageButtonLinkFromMetric = (text: string, metric: Metric, viewType: ViewTypeEnum, location: Location, readVueQueryParams: IReadVueQueryParams, buttonOptions?: IButtonOptions) => {
    let url = `ui${getUrlForMetricOrPageDisplayName(metric.name, location, readVueQueryParams, { viewTypeNameOrUrl: ViewTypeEnum[viewType] })}`;
    return subPageButtonLink(text, url, buttonOptions);
}

export const subPageButtonLink = (text: string, url: string, buttonOptions?: IButtonOptions) => {
    if (url.length < 4) {
        return;
    }
    
    if (buttonOptions?.entitySetInstances && buttonOptions?.focusInstance && !buttonOptions.entitySetInstances.some(e => e.id == buttonOptions.focusInstance?.id)) {
        buttonOptions.entitySetInstances.push(buttonOptions.focusInstance);
        url = url.replace("?", `?highlighted=${buttonOptions.entitySetInstances.map(e => e.id).join(".")}&`);
    }

    if (buttonOptions?.focusInstance) {
        url = url.replace("?", `?active=${buttonOptions?.focusInstance.id}&`);
    }

    return button(url, text, buttonOptions?.buttonAction);
}

const subPageComponentImage = (subPage: AnalysisSubPageComponent, cdnAssetsEndpoint: string): string => {
    return `${cdnAssetsEndpoint}/img/brand-analysis/brandAnalysis${AnalysisSubPageComponent[subPage]}.svg`;
}

export const subPageComponentBackground = (subPage: AnalysisSubPageComponent, cdnAssetsEndpoint: string) => {
    return {
        backgroundImage: `url(${subPageComponentImage(subPage, cdnAssetsEndpoint)})`,
        backgroundRepeat: `no-repeat`,
        backgroundPosition: subPage === AnalysisSubPageComponent.Score || subPage === AnalysisSubPageComponent.ScoreOverTime
            ? "right bottom"
            : "left bottom",
        backgroundSize: "380px"
    }
}

export const analysisScorecardFooter = (cdnAssetsEndpoint: string, productName: string) => {
    const userDescription = productName == "charities" ? "supporters" : "customers";
    const footerText = `Our Bigger and More Loved framework is a brand growth framework building on traditional thinking around saliency and usage; and layering on sentiment and experience. At its most basic, the framework benchmarks how you are winning and keeping your ${userDescription}.`;
    return (
        <div className={style.brandAnalysisFooter}>
            <img src={`${cdnAssetsEndpoint}/img/brand-analysis/brandAnalysisFooterLeft.svg`} className={style.footerImg} />
            <div>
                <div className={style.footerTitle}>What is this telling me?</div>
                <div className={style.footerText}>{footerText}</div>
            </div>
            <img src={`${cdnAssetsEndpoint}/img/brand-analysis/brandAnalysisFooterRight.svg`} className={style.footerImg} />
        </div>
    )
};

export const diffDescription = (originalScore: number, comparisonScore: number) => {
    return originalScore === comparisonScore
        ? "equal to"
        : originalScore > comparisonScore
        ? "ahead of"
        : "behind";
}

export const orderMetricScoreResults = (results: ScoreCurrentVsPrevious[]) => {
    return [...results].sort((a, b) => b.diff - a.diff);
}

export const getDataWrapper = (dataState: AnalysisDataState, showData: () => ReactNode | undefined, options?: { twoThrobbers?: boolean, withRoundel?: boolean, showThrobber?: boolean, showHal?: boolean, errorText?: string | null }) => {
    switch (dataState) {
        case AnalysisDataState.Loading:
            if (options?.showHal == true) {
                return <svg className={helperStyles.hal} xmlns="http://www.w3.org/2000/svg" viewBox="-10 -10 220 220" width="200px" role="loading">
                    <defs>
                        <radialGradient id="outerLens" cx="50%" cy="50%" r="50%" fx="50%" fy="50%">
                            <stop offset="90%" style={{ stopColor: "#000000", stopOpacity: 0 }} />
                            <stop offset="100%" style={{ stopColor: "#808080", stopOpacity: 1 }} />
                        </radialGradient>
                        <radialGradient id="innerLens" cx="50%" cy="50%" r="50%" fx="50%" fy="50%">
                            <stop offset="0%" style={{ stopColor: "#ffffff", stopOpacity: 0 }} />
                            <stop offset="100%" style={{ stopColor: "#000000", stopOpacity: 0.5 }} />
                        </radialGradient>
                        <radialGradient id="irisGradient" cx="50%" cy="50%" r="50%" fx="50%" fy="50%">
                            <stop offset="0%" style={{ stopColor: "#000000", stopOpacity: 1 }} />
                            <stop offset="100%" style={{ stopColor: "rgba(244,173,179,1)", stopOpacity: 0.2 }} />
                        </radialGradient>
                        <radialGradient id="innerIrisGradient" cx="50%" cy="50%" r="50%" fx="50%" fy="50%">
                            <stop offset="0%" style={{ stopColor: "rgba(244,173,179,1)", stopOpacity: 1 }} />
                            <stop offset="100%" style={{ stopColor: "rgba(244,173,179,1)", stopOpacity: 1 }} />
                        </radialGradient>
                        <radialGradient id="pupil" cx="50%" cy="50%" r="50%" fx="50%" fy="50%">
                            <stop offset="0%" style={{ stopColor: "#ffffff", stopOpacity: 1 }} />
                            <stop offset="100%" style={{ stopColor: "rgba(244,173,179,1)", stopOpacity: 0 }} />
                        </radialGradient>
                    </defs>
                    <g id="hal-eye">
                        <circle cx="100" cy="100" r="110" fill="url(#outerLens)" opacity="0.8"></circle>
                        <circle cx="100" cy="100" r="104" fill="url(#innerLens)"></circle>
                        <circle id="iris" className={helperStyles.iris} cx="100" cy="100" r="70" fill="url(#irisGradient)"></circle>
                        <circle id="innerIris" className={helperStyles.innerIris} cx="100" cy="100" r="40" fill="url(#innerIrisGradient)"></circle>
                        <circle cx="100" cy="100" r="25" fill="url(#pupil)"></circle>
                        <circle cx="66" cy="66" r="10" fill="url(#pupil)"></circle>
                    </g>
                    <g id="hal-ring" className={helperStyles.halRing}  filter="url(#f1)">
                        <circle cx="195" cy="131" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="181" cy="159" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="159" cy="181" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="131" cy="195" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="100" cy="200" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="69" cy="195" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="41" cy="181" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="19" cy="159" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="5" cy="131" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="0" cy="100" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="5" cy="69" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="19" cy="41" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="41" cy="19" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="69" cy="5" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="100" cy="0" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="131" cy="5" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="159" cy="19" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="181" cy="41" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="195" cy="69" r="2" fill="rgba(244,173,179,1)"></circle>
                        <circle cx="200" cy="100" r="2" fill="rgba(244,173,179,1)"></circle>
                    </g>
                    <g id="hal-eye">
                    </g>
                </svg>
            }
            if (options?.showThrobber == false) {
                return <>
                    {showData()}
                </>
            }
            return <>
                {options?.withRoundel == true && <div className={`${style.cardRoundel} ${style.throbber}`}>
                    <div className={`${style.roundel}`}  role="loading">
                        <div></div>
                    </div>
                </div>}
                <div className={`${style.subtext} ${style.throbber}`}  role="loading">
                    <div></div>
                    {options?.twoThrobbers == true && <div></div>}
                </div>
            </>
        case AnalysisDataState.Show:
            return <>
                {showData()}
            </>
        case AnalysisDataState.Error:
            return <div className={`${style.subtext} ${style.error}`}>
                <i className="material-symbols-outlined no-symbol-fill">info</i>
                <div>{options?.errorText ? options.errorText : "There was an error loading results"}</div>
            </div>
        case AnalysisDataState.NoData:
            return <div className={`${style.subtext} ${style.noData}`}>
                <div>No data for the brand and time period selected</div>
            </div>
    };
}

export const updateResult = (getResults: IKeyResult[] | null, returnType: BrandAnalysisRequestReturnType, updateState: (s: IKeyResult[]) => void, updateDataState: (ds: AnalysisDataState) => void) => {
    if (returnType == BrandAnalysisRequestReturnType.Succeeded) {
        if (!getResults) {
            updateDataState(AnalysisDataState.NoData)
            return;
        }
        updateState(getResults);
        updateDataState(AnalysisDataState.Show)
    } else {
        updateDataState(AnalysisDataState.Error)
    };
};