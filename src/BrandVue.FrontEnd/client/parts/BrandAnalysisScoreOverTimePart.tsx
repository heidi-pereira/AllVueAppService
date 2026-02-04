import React from "react";
import { AnalysisScorecardTitle } from "../components/helpers/AnalysisHelper";
import { IDashPartProps } from "../components/DashBoard";
import { BasePart } from "./BasePart";
import BrandAnalysisScoreOverTime from "../components/brandanalysis/BrandAnalysisScoreOverTime";

export class BrandAnalysisScoreOverTimePart extends BasePart {
    getPartComponent(props: IDashPartProps): JSX.Element | null {
        var metrics = props.enabledMetricSet.getMetrics(props.partConfig.descriptor.spec1)
        const analysisEntity = props.entitySet.getMainInstance();
        return <BrandAnalysisScoreOverTime
            googleTagManager={props.googleTagManager}
            brand={analysisEntity}
            title={AnalysisScorecardTitle[props.partConfig.descriptor.spec2]}
            activeEntitySet={props.entitySet}
            primaryMetric={metrics[0]}
            partId={props.partConfig.descriptor.id}
            curatedFilters={props.curatedFilters}
            entityConfiguration={props.entityConfiguration}
            updateMetricResultsSummary={props.updateMetricResultsSummary}
            updateAverageRequests={props.updateAverageRequests}
            availableEntitySets={props.availableEntitySets}
            pageMetricConfiguration={JSON.parse(props.partConfig.descriptor.spec3)}/>;
    }
}
