import Tooltip from "../Tooltip";
import { IAverageDescriptor, WeightingMethod } from '../../BrandVueApi';
import { DataSubsetManager } from "../../DataSubsetManager";
import { useMetricStateContext } from "../../metrics/MetricStateContext";
import { Metric } from "../../metrics/metric";
import { metricSupportsWeighting } from "../../metrics/metricHelper";
import { selectSubsetId } from "client/state/subsetSlice";
import { useAppSelector } from "client/state/store";

interface IDisplayAverageProps {
    average: IAverageDescriptor;
    metric?: Metric;
}

const DisplayAverage = (props: IDisplayAverageProps) => {
    const { questionTypeLookup } = useMetricStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const allSubsets = DataSubsetManager.getAll() || [];
    const subsetDisplayName = allSubsets.find(x => x.id === subsetId)?.displayName || 'All';
    
    const isThereOnlyOneSubset = (): boolean => {
        const allSubsets = DataSubsetManager.getAll().filter(x => !x.disabled) || [];
        return allSubsets.length <= 1;
    }
    const getText = () => {
        const isAverageWighted = props.average.weightingMethod !== WeightingMethod.None;
        const metricDoesNotSupportWeighting = !metricSupportsWeighting(props.metric, questionTypeLookup);

        if (!isAverageWighted || metricDoesNotSupportWeighting) {
            return <><strong>No</strong> weighting applied</>;
        }
        return 'Weighting applied';
    }

    const getWeightingText = () => {
        return (
            <div className="average-description">
                {getText()}
            </div>
        );
    }

    if (isThereOnlyOneSubset() ){
        return getWeightingText();
    }

    const toolTip = (props.average.weightingMethod === WeightingMethod.None) ? `Using data from segment '${subsetDisplayName}' ` : `Weighting data from segment '${subsetDisplayName}'`;

    return (
        <Tooltip title={toolTip} placement="top">
            { getWeightingText() }
        </Tooltip>
    );
}

export default DisplayAverage ;
