import React from "react";
import { IDashPartProps } from "../components/DashBoard";
import { BasePart } from "./BasePart";
import StackedChart from "../components/visualisations/StackedChart";

export class StackedChartPart extends BasePart {
    getPartComponent(props: IDashPartProps): JSX.Element | null {
        var metrics = props.enabledMetricSet.getMetrics(props.partConfig.descriptor.spec1)

        return <StackedChart
            googleTagManager={props.googleTagManager}
            title={props.partConfig.descriptor.spec2}
            height={640}
            metrics={metrics}
            curatedFilters={props.activeView.curatedFilters}
            entitySet={props.entitySet}
            activeBrand={props.entitySet.getMainInstance()}
            ordering={props.partConfig.descriptor.ordering}
            orderingDirection={props.partConfig.descriptor.orderingDirection}
            availableEntitySets={props.availableEntitySets}
            updateAverageRequests={props.updateAverageRequests}
            colours={props.partConfig.descriptor.colours}/>;
    }
}
