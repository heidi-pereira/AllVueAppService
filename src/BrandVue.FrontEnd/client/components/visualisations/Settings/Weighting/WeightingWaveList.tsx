import React from "react";
import style from "./WeightingWaveList.module.less"
import SearchInput from "../../../SearchInput";
import { UiWeightingPlanConfiguration, Subset, IAverageDescriptor, WeightingStyle, VariableGrouping, WeightingType, Factory, SampleSize } from "../../../../BrandVueApi";
import { MetricSet } from "../../../../metrics/metricSet";
import WeightingWaveListItem, { WaveDescription } from "./WeightingWaveListItem";
import { navigateToWeightingImport, navigateToWeightingPlan, doesWaveHaveWeights, createWaveDescriptor, isWaveRIMWeighted } from "./WeightingHelper";
import { WeightingPlanValidation } from "./WeightingPlanValidation";
import DeleteWeightingModal from "./Modals/DeleteWeightingModal";
import {useNavigate} from "react-router-dom";

interface IWeightingWaveListProps {
    subset: Subset | undefined;
    weightingPlanConfiguration: UiWeightingPlanConfiguration;
    variableWaves: VariableGrouping[];
    metrics: MetricSet;
    averages: IAverageDescriptor[];
    weightingStyle: WeightingStyle;
    cloneWave: (wave: WaveDescription) => void;
    onErrorMessage: (userFriendlyText: string) => void;
    planValidation: WeightingPlanValidation;
    weightingType: WeightingType;
    sampleSizesByWaveInstance: SampleSize[];
}

const WeightingWaveList = (props: IWeightingWaveListProps) => {
    const [searchText, setSearchText] = React.useState<string>("");
    const [currentlyDeletingWave, setCurrentlyDeletingWave] = React.useState<WaveDescription | null>(null);
    const navigate = useNavigate();
    const allWaveDescriptors = props.variableWaves.map(wave => {
        const weighting = props.weightingPlanConfiguration.uiChildTargets.find(target => target.entityInstanceId === wave.toEntityInstanceId);
        const numberOfRespondentsForWave = props.sampleSizesByWaveInstance.find(x => x.entityId == wave.toEntityInstanceId)?.sampleCount;
        return createWaveDescriptor(wave, weighting, numberOfRespondentsForWave);
        });

    const getMatchingWaves = (waves: WaveDescription[], searchText: string) => {
        const textToSearch = searchText.trim().toLowerCase();
        return waves.filter(wave =>
            wave.InstanceName.toLowerCase().includes(textToSearch)).sort((a, b) => {
                return b.EntityId - a.EntityId;
            });
    }

    const onEditWave = (wave: WaveDescription, subset: Subset | undefined, variableIdentifier: string, weightingStyle: WeightingStyle) => {
        if (subset) {
            if(props.weightingStyle == WeightingStyle.ResponseWeighting){
                navigateToWeightingImport(subset, navigate,variableIdentifier, wave.EntityId);
            } else {
                navigateToWeightingPlan(subset.id,navigate, wave);
            }
        }
    }

    const deleteWave = (subset: Subset | undefined, wave: WaveDescription) => {
        if (subset && wave.DatabaseId) {
            const weightingPlanClient = Factory.WeightingPlansClient(error => error());
            weightingPlanClient.deleteWeightingTarget(subset.id, props.weightingPlanConfiguration.id, wave.DatabaseId)
                .catch((e: Error) => props.onErrorMessage("An error occurred trying to delete weighting wave"));
        }
    }

    const showCloneButton = (wave: WaveDescription, waves: WaveDescription[]) => {
        if (isWaveRIMWeighted(wave)) {
            return waves.some(w => w.EntityId !== wave.EntityId && !doesWaveHaveWeights(w));
        }
        return false;
    }

    const getWave = (wave: WaveDescription) => {
        return (
            <WeightingWaveListItem
                key={props.weightingPlanConfiguration.variableIdentifier + wave.EntityId}
                wave={wave}
                subset={props.subset}
                metricName={props.weightingPlanConfiguration.variableIdentifier}
                averages={props.averages}
                weightingPlanId={props.weightingPlanConfiguration.id}
                showCloneButton={showCloneButton(wave, allWaveDescriptors)}
                addEditWave={(wave) => onEditWave(wave, props.subset, props.weightingPlanConfiguration.variableIdentifier, props.weightingStyle)}
                cloneWave={props.cloneWave}
                deleteWave={setCurrentlyDeletingWave}
                onErrorMessage={props.onErrorMessage}
                planValidation={props.planValidation}
                weightingStyle={props.weightingStyle}
                weightingType={props.weightingType}
            />
        );
    }

    return (
        <div className={style.weightingWaveList}>
            {currentlyDeletingWave && <DeleteWeightingModal
                isOpen={currentlyDeletingWave !== null}
                toggle={() => setCurrentlyDeletingWave(null)}
                cancelDelete={() => setCurrentlyDeletingWave(null)}
                deleteWeighting={() => deleteWave(props.subset, currentlyDeletingWave)}
                wave={currentlyDeletingWave}
            />}
            <div className={style.searchRow}>
                <div >
                    <SearchInput id="wave-search-input" className={style.search} text={searchText} onChange={(text) => {setSearchText(text)}} />
                </div>
            </div>
            <div className={style.waveContainer} >
                {getMatchingWaves(allWaveDescriptors, searchText).map(getWave)}
            </div>
        </div>
    );
}

export default WeightingWaveList;