import React from "react";
import { EntityInstance } from "../entity/EntityInstance";
import { CuratedFilters } from "../filter/CuratedFilters";
import { Metric } from "../metrics/metric";
import BrandAnalysisScorecardComponent from "./brandanalysis/BrandAnalysisScorecardComponent";
import {
    AnalysisScorecardTitle,
    buttonLink,
    buttonText
} from "./helpers/AnalysisHelper";
import style from "./brandanalysis/BrandAnalysis.module.less";
import { ProductConfigurationContext } from "../ProductConfigurationContext";
import { EntitySet } from "../entity/EntitySet";
import { useLocation } from "react-router-dom";
import { useReadVueQueryParams } from "./helpers/UrlHelper";

interface IAnalysisScorecardProps {
    title: AnalysisScorecardTitle,
    brand: EntityInstance,
    entitySet: EntitySet,
    metric: Metric,
    curatedFilters: CuratedFilters;
    pastTenseVerb: string | null;
    helpText: string;
    isSubPage: boolean;
}

const AnalysisScorecard: React.FunctionComponent<IAnalysisScorecardProps> = (props) => {
    const {productConfiguration} = React.useContext(ProductConfigurationContext);
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();
    return <div className="PartColumn analysis-scorecard-container">
        <div className="part-container">
            <div className={`tile ${style.brandAnalysisPage}`}>
                <div className={style.cardContent}>
                    <BrandAnalysisScorecardComponent
                        trackLowSample={true} title={props.title}
                        brand={props.brand} metric={props.metric}
                        entitySet={props.entitySet}
                        curatedFilters={props.curatedFilters}
                        pastTenseVerb={props.pastTenseVerb}
                        linkButton={
                            <div className={`${style.cardContent} ${style.cardPadded}`}>
                                <div className={`not-exported ${style.exploreButton}`}>
                                    <a href={buttonLink(props.title, productConfiguration.appBasePath, location, readVueQueryParams)}
                                       className="hollow-button">
                                        <i className="material-symbols-outlined">arrow_forward</i>
                                        <div>
                                            {buttonText(props.title, productConfiguration.productName)}
                                        </div>
                                    </a>
                                </div>
                            </div>}
                        helpText={props.helpText}
                        subPage={false}
                    />
                </div>
            </div>
        </div>
    </div>;
}

export default AnalysisScorecard;