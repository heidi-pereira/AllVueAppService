import React from "react";
import style from "./WeightingPlansListItem.module.less"
import WeightingWaveList from "./WeightingWaveList";
import WeightingPlansListExport from "./WeightingPlansListExport";
import {
    Factory, WeightingType, WeightingStyle, IAverageDescriptor, WeightingMethod, UiWeightingConfigurationRoot, Subset, VariableGrouping,
    UiWeightingPlanConfiguration,
    SampleSize,
    ExportRespondentWeightsRequest,
    PermissionFeaturesOptions
} from "../../../../BrandVueApi";
import { AverageIds } from "../../../helpers/PeriodHelper";
import { MetricSet } from "../../../../metrics/metricSet";
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from "./Controls/MaterialSymbol";
import { saveAs } from "file-saver";
import WeightingValidationControl from "./Controls/WeightingValidationControl";
import { navigateToWeightingPlan, doesInstanceHaveWeights, navigateToWeightingImport, getWavesForSurveyOrTimeBasedVariable, reloadMetrics } from "./WeightingHelper";
import CopyToSiblingsModal from "./Modals/CopyToSiblingsModal";
import { WaveDescription } from "./WeightingWaveListItem";
import { useEntityConfigurationStateContext } from "../../../../entity/EntityConfigurationStateContext";
import { IEntityConfiguration } from "../../../../entity/EntityConfiguration";
import { WeightingPlanValidation } from "./WeightingPlanValidation";
import VariableContentModal from "../../../../components/visualisations/Variables/VariableModal/VariableContentModal";
import { Metric } from "../../../../metrics/metric";
import { useNavigate } from "react-router-dom";
import { useAppSelector } from "client/state/store";
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import FeatureGuard from "client/components/FeatureGuard/FeatureGuard";

interface IWeightingPlansListItemProps {
    subset: Subset | undefined;
    weightingConfiguration: UiWeightingConfigurationRoot;
    metrics: MetricSet;
    averages: IAverageDescriptor[];
    showNumberOfResponses: boolean;
    expandWaves: boolean;
    onDeleteClick?: (e: React.MouseEvent, subsetId: string) => void;
    onAddWave: (weightingPlan: UiWeightingConfigurationRoot) => void;
    onErrorMessage: (message: string) => void;
}

export class DropDownItemDescription {
    Id: string;
    IsHeader: boolean;
    Text: string;
    Symbol?: MaterialSymbolType;
    onClicked?: () => void;
}

const WeightingPlansListItem = (props: IWeightingPlansListItemProps) => {
    const [wavesExpanded, setWavesExpanded] = React.useState<boolean>(props.expandWaves);
    const [isWaveASurveyOrTimeBasedVariable, setIsWaveASurveyOrTimeBasedVariable] = React.useState<boolean>(false);
    const [waves, setWaves] = React.useState<VariableGrouping[]>([]);
    const [weightingStyle, setWeightingStyle] = React.useState<WeightingStyle>(WeightingStyle.Unknown);
    const [weightingType, setWeightingType] = React.useState<WeightingType>(WeightingType.Unknown);
    const [isLoading, setIsLoading] = React.useState(true);
    const [numberOfResponsesIsLoading, setNumberOfResponsesIsLoading] = React.useState(true);
    const [numberOfRespondents, setNumberOfRespondents] = React.useState(0);
    const [respondentsPerWave, setRespondentsPerWave] = React.useState<SampleSize[]>([]);
    const [cloneWaveModalOpen, setCloneWaveModalOpen] = React.useState(false);
    const [waveToClone, setWaveToClone] = React.useState<WaveDescription>();
    const [metricsSet, setMetricsSet] = React.useState<MetricSet>(new MetricSet({ metrics: [] }));
    const navigate = useNavigate();
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const [planValidation, setPlanValidation] = React.useState<WeightingPlanValidation>(WeightingPlanValidation.fromUiWeightingPlanConfiguration(props.weightingConfiguration.uiWeightingPlans[0]));
    const { variables, loading: isVariablesLoading } = useAppSelector(selectHydratedVariableConfiguration);
    const [isVariableModalOpen, setIsVariableModalOpen] = React.useState<boolean>(false)
    const [isVariableModalAvailable, setIsVariableModalAvailable] = React.useState<boolean>(false)

    const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());
    const weightingPlansClient = Factory.WeightingPlansClient(error => error());
    const subsetId = props.subset?.id;

    const getPlanType = () => {
        if (subsetId) {
            weightingAlgorithmsClient.weightingTypeAndStyle(subsetId).then(p => {
                setWeightingStyle(p.style);
                setWeightingType(p.type);
                setIsLoading(false);
            }).catch((e: Error) => {
                props.onErrorMessage("An error occurred trying to load weightings information")
                setIsLoading(false);
            });
        }
        else {
            setIsLoading(false);
        }
    }

    const getNumberOfResponses = () => {
        if (subsetId !== undefined) {
            weightingAlgorithmsClient.getTotalSampleSizeWithFilters(subsetId, []).then(response => {
                setNumberOfRespondents(response);
                weightingAlgorithmsClient.getSampleSizeByWeightingForTopLevel(subsetId).then(response => {
                    setRespondentsPerWave(response);
                    setNumberOfResponsesIsLoading(false);
                })

            }).catch((e: Error) => {
                props.onErrorMessage("An error occurred trying to load number of responses")
                setNumberOfResponsesIsLoading(false);
            })
        }
        else {
            setNumberOfResponsesIsLoading(false);
        }
    }

    const getValidation = () => {
        if (subsetId) {
            weightingPlansClient.isWeightingPlanDefinedAndValidV2(subsetId).then(validation => {
                setPlanValidation(planValidation.setFromDetailedPlanValidationV2(validation));
            }).catch((e: Error) => { props.onErrorMessage(`An error occurred trying to validate survey segment ${subsetId}`); })
                .finally(() => { setIsLoading(false); });
        } else {
            props.onErrorMessage(`No subset ID available`);
            setIsLoading(false);
        }
    }
    const getVariableIdForWave = (): number | undefined => {
        const metric = metricsSet.metrics.find(m => m.name == props.weightingConfiguration.uiWeightingPlans[0].variableIdentifier);
        return metric?.variableConfigurationId;
    }

    const getRelatedMetric = (): Metric | undefined => {
        const metric = metricsSet.metrics.find(m => m.name == props.weightingConfiguration.uiWeightingPlans[0].variableIdentifier);
        return metric;
    }

    const getWaves = (variableIdentifier: string) => {
        setIsWaveASurveyOrTimeBasedVariable(false);
        if (variableIdentifier) {
            if (weightingType == WeightingType.Tracker) {
                getWavesForSurveyOrTimeBasedVariable(props.weightingConfiguration.uiWeightingPlans[0].variableIdentifier, metricsSet, variables)
                    .then((variableWaves) => {
                        if (variableWaves.length > 0) {
                            setIsWaveASurveyOrTimeBasedVariable(true);
                        }
                        setWaves(variableWaves);
                        setPlanValidation(planValidation.setWaveErrorsFromVariableGrouping(variableWaves, metricsSet, entityConfiguration));
                    });
            }
        };
    }

    const loadMetrics = async () => {
        if (props.subset?.id) {
            const metricSet = await reloadMetrics(props.subset.id);
            setMetricsSet(metricSet ?? props.metrics);
        }
    }

    React.useEffect(() => {
        loadMetrics();
        getPlanType();
        getNumberOfResponses();
        getValidation();
    }, [props.subset?.id]);

    React.useEffect(() => {
        if (!isVariablesLoading && metricsSet.metrics.length > 0) {
            getWaves(props.weightingConfiguration.uiWeightingPlans[0].variableIdentifier);
            setIsVariableModalAvailable(getVariableIdForWave() != undefined)
        }
    }, [weightingType, metricsSet.metrics.length, isVariablesLoading, variables, JSON.stringify(props.weightingConfiguration.uiWeightingPlans[0])])

    const isValidSurveySegment = () => props.subset !== undefined;
    const displayNumberOfRespondents = () => {
        let message;
        if (numberOfResponsesIsLoading) {
            message = 'Loading...'
        } else if (numberOfRespondents <= 0) {
            message = 'No responses'
        } else {
            message = `${numberOfRespondents} Responses`
        }
        return <div className={style.details}>{message}</div>
    }

    const displayWeightingType = () => {
        if (isLoading) {
            return <>Loading...</>
        }
        return <>{weightingType}</>
    }

    const displayWeightingStyle = () => {
        if (isLoading) {
            return <>Loading...</>
        }

        switch (weightingStyle) {
            case WeightingStyle.ResponseWeighting:
                return <>Response level weighting</>;

            case WeightingStyle.Interlocked:
                return <>Interlocked</>

            case WeightingStyle.RIM:
                return <>RIM</>
            case WeightingStyle.Expansion:
                return <>Expansion</>
            case WeightingStyle.Unknown:
                return <>Unknown</>
            default:
                return <>Mixed ({weightingStyle})</>
        }
    }

    const getSubsetDisplayName = (): string => {
        return props.subset?.displayName ?? props.weightingConfiguration.subsetId;
    }

    const onExportResponseWeightings = (root: UiWeightingConfigurationRoot, subsetDisplayName: string, averageId: string) => {
        return weightingAlgorithmsClient.exportRespondentWeights(
            new ExportRespondentWeightsRequest(
                { subsetIds: [root.subsetId], averageId: averageId }))
            .then(
                r => {
                    saveAs(r.data, `Weightings- ${subsetDisplayName}- (${averageId})- Private.csv`);
                })
            .catch(error => {
                props.onErrorMessage("Export failed");
            });
    }

    const onDownloadWeightingsIntegrityReport = (root: UiWeightingConfigurationRoot, subsetDisplayName: string) => {
        const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());
        return weightingAlgorithmsClient.respondentWeightsReport(root.subsetId, root.uiWeightingPlans.map(x => x.variableIdentifier))
            .then(r => saveAs(r.data, `Integrity Weighting Report- ${subsetDisplayName}- Private.csv`))
            .catch(error => {
                props.onErrorMessage("Report failed");
            });
    }

    const getQuestionNameForIdentifier = (identifier: string): string => {
        const planMetric = metricsSet.metrics!.find(x => x.name == identifier);
        const title = planMetric ? planMetric.varCode : identifier;
        return title;
    };

    const getExportOptions = (root: UiWeightingConfigurationRoot, subsetDisplayName: string, averages: IAverageDescriptor[]): DropDownItemDescription[] => {
        const customPeriodAverage = averages.find(x =>
            !x.disabled &&
            x.weightingMethod === WeightingMethod.QuotaCell &&
            x.averageId === AverageIds.CustomPeriod
        );
        const otherAverages = averages.filter(x =>
            !x.disabled &&
            x.weightingMethod === WeightingMethod.QuotaCell &&
            x.averageId !== AverageIds.CustomPeriod
        );
        const defaultAverage = customPeriodAverage ?? otherAverages.find(a => a.isDefault) ?? otherAverages[0];

        const options: DropDownItemDescription[] = [];

        options.push({ Id: 'H1', IsHeader: true, Text: 'Export weights for' })
        if (defaultAverage) {
            options.push({
                Id: 'default',
                IsHeader: false,
                Text: `Default (${subsetDisplayName} ${defaultAverage.averageId !== AverageIds.CustomPeriod ? defaultAverage.displayName : ''})`,
                Symbol: MaterialSymbolType.weight,
                onClicked: () => onExportResponseWeightings(root, subsetDisplayName, defaultAverage.averageId)
            });
        }
        otherAverages.forEach((v, i) => options.push({
            Id: `average_${v.averageId}`,
            IsHeader: false,
            Text: v.displayName,
            Symbol: MaterialSymbolType.weight,
            onClicked: () => onExportResponseWeightings(root, subsetDisplayName, v.averageId)
        }));

        if (!(weightingStyle.toString().includes(WeightingStyle.ResponseWeighting.toString()))) {
            options.push({ Id: 'R1', IsHeader: true, Text: 'Reports' })
            options.push({ Id: 'report', IsHeader: false, Text: `Weighting integrity`, Symbol: MaterialSymbolType.report, onClicked: () => onDownloadWeightingsIntegrityReport(root, subsetDisplayName) })
        }
        return options;
    }

    const getPrimaryWeightedOn = (): string => {
        if (weightingType == WeightingType.Tracker) {
            return getQuestionNameForIdentifier(props.weightingConfiguration.uiWeightingPlans[0].variableIdentifier);
        }
        else {
            if (weightingStyle == WeightingStyle.ResponseWeighting) {
                return "";
            }

            if (weightingStyle == WeightingStyle.RIM) {
                return props.weightingConfiguration.uiWeightingPlans.map(x => getQuestionNameForIdentifier(x.variableIdentifier)).join(",")
            }
        }
        return "";
    }

    const viewWavesLink = (wavesExpanded: boolean, waveCount: number) => {
        return (
            <>
                <MaterialSymbol symbolType={wavesExpanded ? MaterialSymbolType.arrow_drop_up : MaterialSymbolType.arrow_drop_down} symbolStyle={MaterialSymbolStyle.outlined} />
                <span>{`View waves (${waveCount})`}</span>
            </>
        )
    }

    const openCloneWaveModal = (wave: WaveDescription) => {
        setWaveToClone(wave);
        setCloneWaveModalOpen(true);
    }

    const cloneWave = (targetInstanceIds: number[]) => {
        const waveTargetId = waveToClone?.DatabaseId;
        if (props.subset && waveTargetId) {
            weightingPlansClient.copyWeightingPlansToSiblings(props.subset.id, waveTargetId, 0, targetInstanceIds)
                .catch((e: Error) => props.onErrorMessage("An error occurred trying to update weighting"));
        }
    }

    const getCloneModal = (entityConfiguration: IEntityConfiguration, plan: UiWeightingPlanConfiguration, selectedWave?: WaveDescription) => {
        if (selectedWave) {
            const variableMeasure = metricsSet.metrics!.find(x => x.name == plan.variableIdentifier);
            const allEntityInstances = variableMeasure ? entityConfiguration.getAllEnabledInstancesForType(variableMeasure.entityCombination[0]) : [];

            if (allEntityInstances) {
                const selectedInstance = allEntityInstances.find(x => x.id == selectedWave.EntityId);
                const sortedInstancesExcludingMe = allEntityInstances.filter(x => x.id != selectedWave.EntityId).sort((a, b) => a.id - b.id)
                const sortedInstancesExcludingMeAndNoWeightings = sortedInstancesExcludingMe.filter(instance => !doesInstanceHaveWeights(instance, plan));

                return (
                    <CopyToSiblingsModal
                        isOpen={cloneWaveModalOpen}
                        activeInstance={selectedInstance!}
                        closeModal={() => setCloneWaveModalOpen(false)}
                        confirm={cloneWave}
                        entityInstances={sortedInstancesExcludingMe}
                        entityInstancesWithNoWeightings={sortedInstancesExcludingMeAndNoWeightings}
                        flattenToRim={false}
                    />
                )
            }
        }
    }

    return (
        <div className={style.weightingPlanOverview}>
            <div className={`${style.weightingPlan} ${!isValidSurveySegment() ? style.invalidSubset : ''}`} >
                <div className={style.weightingStatus}>
                    <WeightingValidationControl isSubsetValid={isValidSurveySegment()} subsetId={props.subset?.id ?? props.weightingConfiguration.subsetId} onErrorMessage={props.onErrorMessage} planValidation={planValidation} />
                </div>
                <div className={style.nameWeightingStyle} title={getSubsetDisplayName()}>
                    <div className={style.name}>Segment: {getSubsetDisplayName()}</div>
                    <div className={style.details}>{displayWeightingStyle()}</div>
                </div>
                <div className={style.projectType}>{displayWeightingType()}
                    {isWaveASurveyOrTimeBasedVariable &&
                        <div className={style.viewWavesLink} onClick={() => setWavesExpanded((wavesExpanded) => !wavesExpanded)}>
                            {viewWavesLink(wavesExpanded, waves.length)}
                        </div>
                    }
                </div>
                <div className={style.segment} title={getSubsetDisplayName()}>
                    {getSubsetDisplayName()}
                    {props.showNumberOfResponses && displayNumberOfRespondents()}
                </div>
                <div className={style.weightedOn} title={getPrimaryWeightedOn()}>
                    <div className={style.variableActions}>
                        <div>
                            {getPrimaryWeightedOn()}
                        </div>
                        {isVariableModalAvailable && weightingType == WeightingType.Tracker &&
                            <FeatureGuard permissions={[PermissionFeaturesOptions.VariablesEdit]}>
                                <div title={`Edit variable '${getPrimaryWeightedOn()}'`} onClick={() => setIsVariableModalOpen(true)}>

                                    <MaterialSymbol symbolType={MaterialSymbolType.edit} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} noFill />
                                </div>
                            </FeatureGuard>
                        }
                    </div>
                </div>
                <div className={style.actions}>
                    {props.subset !== undefined &&
                        <>
                            {!(weightingStyle == WeightingStyle.ResponseWeighting && weightingType == WeightingType.Tracker) &&
                                <div title="Edit weighting" onClick={() => weightingStyle == WeightingStyle.ResponseWeighting ? navigateToWeightingImport(props.subset!, navigate,) : navigateToWeightingPlan(props.weightingConfiguration.subsetId, navigate)}>
                                    <MaterialSymbol symbolType={MaterialSymbolType.edit} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} noFill />
                                </div>
                            }
                            <div title="Download weighting">
                                <WeightingPlansListExport exportButtonMenuItems={getExportOptions(props.weightingConfiguration, getSubsetDisplayName(), props.averages)}></WeightingPlansListExport>
                            </div>
                            <div title="Delete weighting" onClick={(e) => props.onDeleteClick?.(e, props.subset!.id)}>
                                <MaterialSymbol symbolType={MaterialSymbolType.delete} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} />
                            </div>
                        </>
                    }
                </div>
            </div>
            <div className={`${style.waveListContainer} ${wavesExpanded ? style.open : ""}`}>
                {isWaveASurveyOrTimeBasedVariable && wavesExpanded &&
                    <WeightingWaveList
                        subset={props.subset}
                        weightingPlanConfiguration={props.weightingConfiguration.uiWeightingPlans[0]}
                        variableWaves={waves}
                        metrics={metricsSet}
                        averages={props.averages}
                        cloneWave={openCloneWaveModal}
                        onErrorMessage={props.onErrorMessage}
                        planValidation={planValidation}
                        weightingStyle={weightingStyle}
                        weightingType={weightingType}
                        sampleSizesByWaveInstance={respondentsPerWave}
                    />
                }
            </div>
            {getCloneModal(entityConfiguration, props.weightingConfiguration.uiWeightingPlans[0], waveToClone)}
            <VariableContentModal
                isOpen={isVariableModalOpen}
                setIsOpen={setIsVariableModalOpen}
                variableIdToView={getVariableIdForWave()}
                subsetId={props.subset?.id ?? props.weightingConfiguration.subsetId}
                relatedMetric={getRelatedMetric()}
            />

        </div>
    );
}

export default WeightingPlansListItem;