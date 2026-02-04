import React from "react";
import { AnalysisScorecardTitle } from "../components/helpers/AnalysisHelper";
import { IDashPartProps } from "../components/DashBoard";
import { BasePart } from "./BasePart";
import BrandAnalysisPotentialScore from "../components/brandanalysis/BrandAnalysisPotentialScore";

export class BrandAnalysisPotentialScorePart extends BasePart {
    getPartComponent(props: IDashPartProps): JSX.Element | null {
        var metrics = props.enabledMetricSet.getMetrics(props.partConfig.descriptor.spec1)
        const analysisEntity = props.entitySet.getMainInstance();
        return <BrandAnalysisPotentialScore
            brand={analysisEntity}
            activeEntitySet={props.entitySet}
            curatedFilters={props.curatedFilters}
            primaryMetric={metrics[0]}
            title={AnalysisScorecardTitle[props.partConfig.descriptor.spec2]}
            entityConfiguration={props.entityConfiguration}
            pageMetricConfiguration={JSON.parse(props.partConfig.descriptor.spec3)}
        />;
    }
}
