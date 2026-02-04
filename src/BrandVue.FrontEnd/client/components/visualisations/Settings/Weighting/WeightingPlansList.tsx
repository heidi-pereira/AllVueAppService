import React from "react";
import { UiWeightingConfigurationRoot, Subset, IAverageDescriptor } from "../../../../BrandVueApi";
import { MetricSet } from "../../../../metrics/metricSet";
import style from "./WeightingPlansList.module.less"
import toast from "react-hot-toast";
import WeightingPlansListItem from "./WeightingPlansListItem"

interface IWeightingPlansListProps {
    allSubsets: Subset[];
    plans: UiWeightingConfigurationRoot[];
    metrics: MetricSet;
    averages: IAverageDescriptor[];
    isExportWeightsAvailable: Boolean;
    onDeleteClick: (e: React.MouseEvent, weightingPlan: UiWeightingConfigurationRoot) => void;
    onAddWave: (weightingPlan: UiWeightingConfigurationRoot) => void;
}

const WeightingPlansList = (props: IWeightingPlansListProps) => {

    const onDeleteSubset = (e: React.MouseEvent, subsetId: string):void => {
        const deletedEntry = props.plans.find(weightingConfiguration => weightingConfiguration.subsetId === subsetId);
        if (deletedEntry) {
            props.onDeleteClick(e, deletedEntry);
        }
    }
    const getSubset = (root: UiWeightingConfigurationRoot): Subset|undefined => {
        const weightingPlanSubset = props.allSubsets.find(s => s.id === root.subsetId);
        return weightingPlanSubset ;
    }

    const expandWaves = props.plans.length == 1;

    return (
        <div className={style.weightingsContainer}>
            <div className={style.weightingsHeader}>
                <div className={style.headerStatus}></div>
                <div className={style.headerName}>Name</div>
                <div className={style.headerProjectType}>Project type</div>
                <div className={style.headerSegment}>Segment</div>
                <div className={style.headerWeightedOn}>Weighted on</div>
                <div className={style.headerActions}></div>
            </div>
            <div className={`${style.weightingsContent} ${style.weightingsContentScrollable}`}>
                {
                    props.plans.map(weightingConfiguration =>
                        <WeightingPlansListItem
                            key={`item-${weightingConfiguration.subsetId}`}
                            subset={getSubset(weightingConfiguration)}
                            weightingConfiguration={weightingConfiguration}
                            metrics={props.metrics}
                            averages={props.averages}
                            showNumberOfResponses={true}
                            expandWaves={expandWaves}
                            onDeleteClick={onDeleteSubset}
                            onAddWave={props.onAddWave}
                            onErrorMessage={message => toast.error(message)}
                        />
                    )
                }
            </div>
        </div>
    );
}

export default WeightingPlansList;