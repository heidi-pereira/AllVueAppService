import React from "react";
import style from "./WeightingWaveListItem.module.less"
import MaterialSymbol, { MaterialSymbolStyle, MaterialSymbolType } from "./Controls/MaterialSymbol";
import { Subset, Factory, WeightingFilterInstance, IAverageDescriptor, WeightingMethod, VariableSampleResult, VariableGrouping, UiWeightingPlanConfiguration,
     WeightingType, WeightingStyle } from "../../../../BrandVueApi";
import WeightingPlansListExport from "./WeightingPlansListExport";
import { AverageIds } from "../../../helpers/PeriodHelper";
import { DropDownItemDescription } from "./WeightingPlansListItem";
import { saveFile } from "../../../../helpers/FileOperations";
import PopoverTooltip, { PopoverType } from "./PopoverTooltip";
import { getGroupCountAndSample } from "../../Variables/VariableModal/Utils/VariableComponentHelpers";
import { doesWaveHaveWeights, navigateToWeightingPlan, navigateToWeightingImport, getReadableWeightingStyle } from "./WeightingHelper";
import WeightingWaveValidationControl from "./Controls/WeightingWaveValidationControl";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle } from "reactstrap";
import { WeightingPlanValidation } from "./WeightingPlanValidation";
import {useNavigate} from "react-router-dom";

interface IWeightingWaveListItemProps {
    wave: WaveDescription;
    subset: Subset | undefined;
    metricName: string;
    averages: IAverageDescriptor[];
    weightingPlanId: number;
    showCloneButton: boolean;
    addEditWave: (wave: WaveDescription) => void;
    cloneWave: (wave: WaveDescription) => void;
    deleteWave: (wave: WaveDescription) => void;
    onErrorMessage: (userFriendlyText: string) => void;
    planValidation: WeightingPlanValidation;
    weightingType: WeightingType;
    weightingStyle: WeightingStyle;
}

export type WaveDescription = {
    InstanceName: string;
    Wave: VariableGrouping;
    EntityId: number;
    Sample?: VariableSampleResult;
    DatabaseId: number | undefined;
    ChildPlans?: UiWeightingPlanConfiguration[];
    ResponseWeightingContextId?: number;
    NumberOfRespondentsForWave?: number;
}

const WeightingWaveListItem = (props: IWeightingWaveListItemProps) => {
    const [numberOfRespondents, setNumberOfRespondents] = React.useState(0);
    const [numberOfResponsesIsLoading, setNumberOfResponsesIsLoading] = React.useState(true);
    const [numberOfResponsesError, setNumberOfResponsesError] = React.useState(false);
    const [downloadPopoverOpen, setDownloadPopoverOpen] = React.useState(false);
    const [deletePopoverOpen, setDeletePopoverOpen] = React.useState(false);
    const [isAddWeightingOpen, setIsAddWeightingOpen] = React.useState(false);
    const navigate = useNavigate();
    React.useEffect(() => {
        let isMounted = true;
        const checkIsMounted = () => {
            return isMounted;
        }
        if (props.subset) {
            if (!doesWaveHaveWeights(props.wave)) {
                getResponseCountFromVariable(props.subset, props.wave, checkIsMounted)
            }
            else {
                if (props.wave.NumberOfRespondentsForWave == undefined && !props.planValidation.isValid) {
                    getResponseCountFromVariable(props.subset, props.wave, checkIsMounted);
                }
                else {
                    setNumberOfRespondents(props.wave.NumberOfRespondentsForWave ?? 0);
                    setNumberOfResponsesIsLoading(false);
                }
            }
        }
        return () => {
            isMounted = false;
        }
    }, []);

    const getResponseCountFromVariable = (subset: Subset, wave: WaveDescription, isMounted: () => boolean) => {
        getGroupCountAndSample(subset.id, wave.Wave)
            .then(response => {
                if (isMounted()) {
                    setNumberOfRespondents(response[0].count);
                }
            }).catch((e: Error) => {
                if (isMounted()) {
                    setNumberOfResponsesError(true);
                }
            }).finally(() => {
                if (isMounted()) {
                    setNumberOfResponsesIsLoading(false)
                }
            });
    }

    const displayNumberOfRespondents = () => {
        if (numberOfResponsesError) {
            return <div className={`${style.error} ${style.details}`}>Error loading number of responses</div>
        }
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

    const onExportResponseWeightings = (subsetId: string, average: IAverageDescriptor) => {
        const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());
        const weightingFilterInstances: WeightingFilterInstance[] = [];
        const filterPartOfFileName = `${props.metricName}-${props.wave.InstanceName}`.replace("/", "- ");
        
        const myInstance = new WeightingFilterInstance();
        myInstance.filterInstanceId = props.wave.EntityId;
        myInstance.filterMetricName = props.metricName;

        weightingFilterInstances.push(myInstance);
        return weightingAlgorithmsClient.exportRespondentWeightsForSubset(subsetId, average.averageId, weightingFilterInstances)
            .then(r => saveFile(r, `Weightings- ${subsetId}- (${average.displayName}- ${filterPartOfFileName})- Private.csv`))
            .catch(error => {
                props.onErrorMessage("Export failed");
            });
    }

    const getExportOptions = (subset: Subset|undefined, averages: IAverageDescriptor[]): DropDownItemDescription[] => {
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
        if (subset) {
            options.push({ Id: 'H1', IsHeader: true, Text: 'Export weights for' })
            options.push({
                Id: 'default',
                IsHeader: false,
                Text: `Default (${subset.displayName} ${defaultAverage.averageId !== AverageIds.CustomPeriod ? defaultAverage.displayName : ''})`,
                Symbol: MaterialSymbolType.weight,
                onClicked: () => onExportResponseWeightings(subset.id, defaultAverage)
            });

            otherAverages.forEach(average => options.push({
                Id: `average_${average.averageId}`,
                IsHeader: false,
                Text: average.displayName,
                Symbol: MaterialSymbolType.weight,
                onClicked: () => onExportResponseWeightings(subset.id, average)
            }));
        }
        return options;
    }

    const addWeights = () => {
        if(props.weightingStyle == WeightingStyle.Unknown) {
            return (
                <div>
                    <ButtonDropdown isOpen={isAddWeightingOpen} toggle={() => setIsAddWeightingOpen(!isAddWeightingOpen)} direction="down">
                        <DropdownToggle className={style.addWeightDropdown}>
                            <div className={style.text}>Add weights</div>
                        </DropdownToggle>
                        <DropdownMenu container="body" className={style.buttonDropdown}>
                            <DropdownItem className={style.dropdownItem} onClick={() => navigateToWeightingPlan(props.subset!.id, navigate,props.wave)}>
                                {getReadableWeightingStyle(WeightingStyle.RIM)}
                            </DropdownItem>
                            <DropdownItem className={style.dropdownItem} onClick={() => navigateToWeightingImport(props.subset!, navigate,props.metricName, props.wave.EntityId)}>
                                {getReadableWeightingStyle(WeightingStyle.ResponseWeighting)}
                            </DropdownItem>
                        </DropdownMenu>
                    </ButtonDropdown>
                </div>
            )
        }

        return (
            <div className={style.addWeightButton}>
                <button className="hollow-button" onClick={() => props.weightingStyle == WeightingStyle.ResponseWeighting ? navigateToWeightingImport(props.subset!, navigate,props.metricName, props.wave.EntityId):navigateToWeightingPlan(props.subset!.id, navigate,props.wave)}>
                    Add weights
                </button>
            </div>
        ) 
    }

    const editAction = (wave: WaveDescription) => {
        return (
            <div className={style.editAction}>
                <div className={style.text}>Weighted</div>
                <div className={style.button} title="Edit weighting" onClick={() => props.addEditWave(wave)}>
                    <MaterialSymbol symbolType={MaterialSymbolType.edit} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} noFill />
                </div>
            </div>
        );
    }

    const cloneAction = (wave: WaveDescription) => {
        return (
            <div title="Copy weighting" onClick={() => props.cloneWave(wave)}>
                <MaterialSymbol symbolType={MaterialSymbolType.file_copy} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} noFill />
            </div>
        );
    }

    const downloadAction = () => {
        if (doesWaveHaveWeights(props.wave)) {
            return (
                <div title="Download weighting">
                    <WeightingPlansListExport exportButtonMenuItems={getExportOptions(props.subset, props.averages)}></WeightingPlansListExport>
                </div>
            );
        }

        const downloadPopoverId = `pop-download-${props.wave.EntityId}`;
        const popoverContent = <span>No respondents weighted for this wave. <strong>Add weights to fix this.</strong></span>;
        return (
            <>
                <div id={downloadPopoverId}
                    title="Download weighting"
                    className={style.disabled}
                    onMouseEnter={() => setDownloadPopoverOpen(true)}
                    onMouseLeave={() => setDownloadPopoverOpen(false)}>
                    <MaterialSymbol symbolType={MaterialSymbolType.download} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} />
                </div>
                <PopoverTooltip
                    type={PopoverType.Info}
                    popoverContent={popoverContent}
                    id={downloadPopoverId}
                    isOpen={downloadPopoverOpen}
                    includeHeader={true}
                    limitWidth={true}
                    placement={"left"}
                />
            </>
        );
    }

    const deleteAction = () => {
        if (doesWaveHaveWeights(props.wave)) {
            return (
                <div title="Delete weighting" onClick={() => props.deleteWave(props.wave)}>
                    <MaterialSymbol symbolType={MaterialSymbolType.delete} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} />
                </div>
            ); 
        }

        const deletePopoverId = `pop-delete-${props.wave.EntityId}`;
        const popoverContent = <span>No respondents weighted for this wave. <strong>Add weights to fix this.</strong></span>;
        return (
            <>
                <div id={deletePopoverId}
                    title="Delete weighting"
                    className={style.disabled}
                    onMouseEnter={() => setDeletePopoverOpen(true)}
                    onMouseLeave={() => setDeletePopoverOpen(false)}>
                    <MaterialSymbol symbolType={MaterialSymbolType.delete} symbolStyle={MaterialSymbolStyle.outlined} className={style.symbol} />
                </div>
                <PopoverTooltip
                    type={PopoverType.Info}
                    popoverContent={popoverContent}
                    id={deletePopoverId}
                    isOpen={deletePopoverOpen}
                    includeHeader={true}
                    limitWidth={true}
                    placement={"left"}
                />
            </>
        );
    }

    const getWaveActions = () => {
        if(props.weightingType == WeightingType.Unknown){
            return;
        }
    
        return(
            <div className={style.waveActionContent}>
                {doesWaveHaveWeights(props.wave) ? editAction(props.wave) : addWeights()}
                <div className={style.right}>
                    {props.showCloneButton && cloneAction(props.wave)}
                    {downloadAction()}
                    {deleteAction()}
                </div>
            </div>
        )
    }

    return (
        <div className={style.waveRow}>
            <div className={style.placeHolder}></div>
            <div className={style.placeHolder2}></div>
            <div className={style.waveNameRespondentCount}>
                <div className={style.name} title={props.wave.InstanceName}>{props.wave.InstanceName}</div>
                {displayNumberOfRespondents()}
            </div>
            <div className={style.waveErrorContainer}>
                <WeightingWaveValidationControl wave={props.wave} metricName={props.metricName} planValidation={props.planValidation} weightingPlanId={props.weightingPlanId} />
            </div>
            {
                <div className={style.waveActions}>
                    {getWaveActions()}
                </div>
            }
        </div>
    )
}

export default WeightingWaveListItem;