import React from "react";
import { EntityInstance } from "../../entity/EntityInstance";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { AnalysisScorecardTitle } from "../helpers/AnalysisHelper";
import BrandAnalysisScorecardComponent from "./BrandAnalysisScorecardComponent";
import style from "./BrandAnalysis.module.less";
import { EntitySet } from "../../entity/EntitySet";

interface IBrandAnalysisScorecardProps {
    title: AnalysisScorecardTitle,
    brand: EntityInstance,
    entitySet: EntitySet,
    metric: Metric,
    curatedFilters: CuratedFilters;
    pastTenseVerb: string | null;
    helpText: string;
}

const BrandAnalysisScorecard: React.FunctionComponent<IBrandAnalysisScorecardProps> = (props) => {
    return <div className={`tile ${style.brandAnalysisSubPage} ${style.title}`}>
        <div>
            <BrandAnalysisScorecardComponent 
                trackLowSample={false} 
                title={props.title} 
                brand={props.brand}
                entitySet={props.entitySet}
                metric={props.metric} 
                curatedFilters={props.curatedFilters} 
                useTitleDescriptor={true} 
                pastTenseVerb={props.pastTenseVerb}
                helpText={props.helpText}
                subPage={true}
            />
         </div>
    </div>;
}

export default BrandAnalysisScorecard;