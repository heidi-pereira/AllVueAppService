import React from "react";
import AnalysisScorecard from "../components/AnalysisScorecard";
import { AnalysisScorecardTitle } from "../components/helpers/AnalysisHelper";
import { IDashPartProps } from "../components/DashBoard";
import { BasePart } from "./BasePart";
import { isBrandAnalysisSubPage } from "../components/helpers/PagesHelper";

export class AnalysisScorecardPart extends BasePart {
    getPartComponent(props: IDashPartProps): JSX.Element | null {
        var metrics = props.enabledMetricSet.getMetrics(props.partConfig.descriptor.spec1)
        const analysisEntity = props.entitySet.getMainInstance();
        return <AnalysisScorecard
            key={this.descriptor.spec2}
            title={AnalysisScorecardTitle[this.descriptor.spec2]}
            brand={analysisEntity}
            entitySet={props.entitySet}
            curatedFilters={props.activeView.curatedFilters}
            metric={metrics[0]}
            pastTenseVerb={this.descriptor.spec3 != '' ? this.descriptor.spec3 : null}
            helpText={this.descriptor.helpText}
            isSubPage={isBrandAnalysisSubPage(props.session.activeDashPage)}
        />;
    }
}
