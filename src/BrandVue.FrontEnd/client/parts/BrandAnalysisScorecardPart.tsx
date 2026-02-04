import React from "react";
import { AnalysisScorecardTitle } from "../components/helpers/AnalysisHelper";
import { IDashPartProps } from "../components/DashBoard";
import BrandAnalysisScorecard from "../components/brandanalysis/BrandAnalysisScorecard";
import { BasePart } from "./BasePart";

export class BrandAnalysisScorecardPart extends BasePart {
    getPartComponent(props: IDashPartProps): JSX.Element | null {
        var metrics = props.enabledMetricSet.getMetrics(props.partConfig.descriptor.spec1)
        const analysisEntity = props.entitySet.getMainInstance();
        return <BrandAnalysisScorecard
            title={AnalysisScorecardTitle[props.partConfig.descriptor.spec2]}
            brand={analysisEntity}
            entitySet={props.entitySet}
            curatedFilters={props.activeView.curatedFilters}
            metric={metrics[0]}
            pastTenseVerb={this.descriptor.spec3 != '' ? this.descriptor.spec3 : null}
            helpText={this.descriptor.helpText}
        />;
    }
}
