import React from "react";
import { Modal, ModalHeader, ModalBody, ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from "reactstrap";
import { Subset, Factory, UiWeightingConfigurationRoot, UiWeightingPlanConfiguration, CopyWeightingPlanModel, VariableConfigurationModel, UiWeightingTargetConfiguration, WeightingStyle } from "../../../../../BrandVueApi";
import { DataSubsetManager } from "../../../../../DataSubsetManager";
import { useMetricStateContext } from "../../../../../metrics/MetricStateContext";
import { Metric } from "../../../../../metrics/metric";
import style from './CreateWeightingModal.module.less'
import SearchInput from "../../../../../components/SearchInput";
import { getReadableWeightingStyle, getWavesFromVariableDefinition, navigateToWeightingImport, navigateToWeightingPlan } from "../WeightingHelper";
import toast from "react-hot-toast";
import { selectHydratedVariableConfiguration } from 'client/state/variableConfigurationSelectors';
import { useAppSelector } from "client/state/store";
import IconWithPopover, { IconType } from "../IconWithPopover";
import { metricsThatMatchSearchText } from "../../../../../metrics/metricHelper";
import {useNavigate} from "react-router-dom";

interface ICreateWeightingModal {
    isVisible: boolean;
    toggle: () => void;
    copyFromWeightingPlan: UiWeightingConfigurationRoot | null;
    existingWeightingPlan?: UiWeightingConfigurationRoot;
    plans: UiWeightingConfigurationRoot[];
}

export enum ProjectType{
    AdHoc = "Ad hoc",
    Tracker = "Tracker"
}

const CreateWeightingModal = (props: ICreateWeightingModal) => {
    const [projectType, setProjectType] = React.useState<ProjectType>(ProjectType.AdHoc)
    const allSubsets = DataSubsetManager.getAll() || [];
    const subsetsWithNoWeighting = allSubsets.filter(s => !props.plans.map(ws => ws.subsetId).includes(s.id));
    const [selectedSubset, setSelectedSubset] = React.useState<Subset|undefined>(subsetsWithNoWeighting.length == 1 ? subsetsWithNoWeighting[0] : undefined);
    const [subsetDropdownOpen, setSubsetDropdownOpen] = React.useState(false);
    const [waveSelectorDropdownOpen, setWaveSelectorDropdownOpen] = React.useState(false);
    const [projectTypeDropdownOpen, setProjectTypeDropdownOpen] = React.useState(false);
    const { selectableMetricsForUser: metrics } = useMetricStateContext();
    const [selectedWave, setSelectedWave] = React.useState<Metric|undefined>();
    const [weightingPlanToCopy, setWeightingPlanToCopy] = React.useState<UiWeightingConfigurationRoot | null>(null);
    const weightingPlansClient = Factory.WeightingPlansClient(error => error());
    const [searchText, setSearchText] = React.useState<string>('');
    const { variables } = useAppSelector(selectHydratedVariableConfiguration);
    const [weightingStyle, setWeightingStyle] = React.useState<WeightingStyle>(WeightingStyle.RIM)
    const navigate = useNavigate();
    const getSubsetSelector = (subsets: Subset[]) => {
        if (subsets.length > 0){
            return (
                <div>
                    <div className={style.segmentLabel}>
                        <label className="variable-page-label">Segment</label>
                        <div className={style.segmentHelp}>
                            <IconWithPopover id="pop-segment-info" iconType={IconType.Info} popoverContent={<>A group of respondents to which you are applying a weight. It's connected to the sample links we send out for surveys and can be a group of participants defined by their demographics.</>}/>
                        </div>
                    </div>
                    <ButtonDropdown isOpen={subsetDropdownOpen} toggle={() => setSubsetDropdownOpen(!subsetDropdownOpen)} className="metric-dropdown">
                        <DropdownToggle className="metric-selector-toggle toggle-button" disabled={subsets.length === 1 && selectedSubset !== undefined}>
                            <span>{selectedSubset?.displayName ?? "Choose a segment"}</span>
                            <i className={`${style.symbol} material-symbols-outlined`}>arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu>
                            {subsetsWithNoWeighting.map(subset => {
                                return (<DropdownItem key={subset.id} onClick={() => setSelectedSubset(subset)}>
                                    <div className="name-container">
                                        <span className='title'>{subset.displayName}</span>
                                    </div>
                                </DropdownItem>);
                            })}
                        </DropdownMenu>
                    </ButtonDropdown>
                </div>
            );
        }
    };

    const getModalHeader = (headerText: string) => {
        return (
            <div className="settings-modal-header">
                <div className="close-icon">
                    <button type="button" className="btn btn-close" onClick={closeAndResetModal}>   
                    </button>
                </div>
                <div className="set-name">{headerText}</div>
            </div>
        );
    }

    const getProjectType = () => {
        const getTrackerOption = () => {
            const waveVariablesAvailable = metrics.some(m => m.isWaveMeasure || m.isSurveyIdMeasure);

            if (!waveVariablesAvailable) {
                const popoverContent = <span>No wave variable is set up. <strong>Create a wave variable in the data tab to add tracker.</strong></span>;

                return (
                    <DropdownItem key={2} className={`${style.dropDownItem} ${style.disabled}`} disabled>
                        Tracker <IconWithPopover id="pop-wave-info" iconType={IconType.Info} popoverContent={popoverContent} />
                    </DropdownItem>
                )
            }

            return <DropdownItem key={2} className={style.dropDownItem} onClick={() => setProjectType(ProjectType.Tracker)}>
                <span className='title'>Tracker</span>
            </DropdownItem>
        }

        return (
            <div className={style.dropDownContainer}>
                <label className="variable-page-label">Project type</label>
                <ButtonDropdown isOpen={projectTypeDropdownOpen} toggle={() => setProjectTypeDropdownOpen(!projectTypeDropdownOpen)} className={style.dropDown}>
                    <DropdownToggle className={`toggle-button ${style.dropDownToggle}`}>
                        <span>{projectType}</span>
                        <i className={`${style.symbol} material-symbols-outlined`}>arrow_drop_down</i>
                    </DropdownToggle>
                    <div className={style.dropDownMenu}>
                        <DropdownMenu className={style.dropDownContents}>
                            <DropdownItem key={1} className={style.dropDownItem} onClick={() => setProjectType(ProjectType.AdHoc)}>
                                <span className='title'>Ad hoc</span>
                            </DropdownItem>
                            {getTrackerOption()}
                        </DropdownMenu>
                    </div>
                </ButtonDropdown>
            </div>
        )
    };

    const getMatchedMetrics = (): Metric[] => {
        if (!searchText || searchText.trim() == '') {
            return metrics;
        }
        return metricsThatMatchSearchText(searchText, metrics);
    }

    const getWaveOrWeightTypeSelector = () => {
        if (projectType == ProjectType.Tracker) {
            const matchedMetrics = getMatchedMetrics();
            const waveVariableMetrics = matchedMetrics.filter(m => m.isWaveMeasure || m.isSurveyIdMeasure);
            if (waveVariableMetrics.length === 1 && !selectedWave)
            {
                setSelectedWave(waveVariableMetrics[0]);
            }
    
            return (
                <div className={style.waveSelector}>
                    <label className="variable-page-label">Wave variable</label>
                    <ButtonDropdown isOpen={waveSelectorDropdownOpen} toggle={() => setWaveSelectorDropdownOpen(!waveSelectorDropdownOpen)} className={`metric-dropdown ${style.waveSelectorDropdown}`}>
                        <DropdownToggle className="metric-selector-toggle toggle-button" disabled={waveVariableMetrics.length === 1 && selectedWave !== undefined}>
                            <span>{selectedWave?.name ?? "Choose a variable"}</span>
                            <i className={`${style.symbol} material-symbols-outlined`}>arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu className={style.waveSelectorDropdownMenu}>
                            <SearchInput id="question-search" onChange={(text) => setSearchText(text)} text={searchText} className="question-search-input-group" autoFocus={true} />
                            <DropdownItem divider />
                            <div className={style.variableList}>
                                {getItemsWithHeader("Wave variables", waveVariableMetrics)}
                            </div>
                        </DropdownMenu>
                    </ButtonDropdown>
                </div>
            );
        } else {
            return (
                <div>
                    <label className="variable-page-label">Weighting style</label>
                    <ButtonDropdown isOpen={waveSelectorDropdownOpen} toggle={() => setWaveSelectorDropdownOpen(!waveSelectorDropdownOpen)} className="metric-dropdown">
                        <DropdownToggle className="metric-selector-toggle toggle-button">
                            <span>{getReadableWeightingStyle(weightingStyle)}</span>
                            <i className={`${style.symbol} material-symbols-outlined`}>arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu>
                            <DropdownItem key={1} onClick={() => setWeightingStyle(WeightingStyle.RIM)}>
                                <span className={style.weightingModalOption}>{getReadableWeightingStyle(WeightingStyle.RIM)}</span>
                            </DropdownItem>
                            <DropdownItem key={2} onClick={() => setWeightingStyle(WeightingStyle.ResponseWeighting)}>
                                <span className={style.weightingModalOption}>{getReadableWeightingStyle(WeightingStyle.ResponseWeighting)}</span>
                            </DropdownItem>
                        </DropdownMenu>
                    </ButtonDropdown>
                </div>
            )
        }
    };

    const createWeightingPlan = () => {
        if (weightingPlanToCopy) {
            copyWeightingPlan(weightingPlanToCopy.subsetId, selectedSubset!.id);
            return;
        }

        if (projectType == ProjectType.Tracker) {
            const weightingPlan = new UiWeightingPlanConfiguration;
            const waveVariable = variables.find(v => v.id == selectedWave!.variableConfigurationId);
            if(!waveVariable) {
                toast.error("Unable to find definition for selected variable");
                return;
            }
            weightingPlan.variableIdentifier = waveVariable.identifier;
            weightingPlan.isWeightingGroupRoot = true;

            const newWeightingPlanRoot = new UiWeightingConfigurationRoot;
            newWeightingPlanRoot.subsetId = selectedSubset!.id;
            newWeightingPlanRoot.uiWeightingPlans = [weightingPlan];    
            weightingPlansClient.createWeightingPlan(newWeightingPlanRoot)
            .catch((e: Error) => toast.error("An error occurred trying to create weighting"));
        } else {
            if(weightingStyle == WeightingStyle.RIM){
                closeAndResetModal();
                navigateToWeightingPlan(selectedSubset!.id, navigate);
            } else {
                closeAndResetModal();
                navigateToWeightingImport(selectedSubset!, navigate);
            }
        }
    }

    const selectWave = (metric: Metric) => {
        setSearchText('');
        setSelectedWave(metric);
    }

    const getItemsWithHeader = (label: string, mappedMetrics: Metric[]) => {
        return (
            <>
                <DropdownItem header>
                    {label}
                </DropdownItem>
                {mappedMetrics.map((m) => {
                    return (
                        <DropdownItem key={m.name} onClick={() => selectWave(m)}>
                            <span className={style.weightingModalOption}>{m.name}</span>
                        </DropdownItem>
                    );
                })}
            </>
        )
    }

    const closeAndResetModal = () => {
        props.toggle();
        setProjectType(ProjectType.AdHoc);
        setSelectedSubset(undefined);
        setSelectedWave(undefined);
        setWeightingPlanToCopy(null);
        setSearchText('');
    }

    const isValidPlan = selectedSubset;

    const copyWeightingPlan = (subsetId: string, newSubsetId: string) =>
        weightingPlansClient.copyWeightingPlan(new CopyWeightingPlanModel({
            copyFromSubsetId: subsetId,
            subsetId: newSubsetId
        })
    );

    const getHeaderTitle = (weightingPlanToCopy: UiWeightingConfigurationRoot | null) => {
        return weightingPlanToCopy ? `Copy weighting from segment ${weightingPlanToCopy.subsetId}` : "Create weighting";
    }

    return (
        <Modal isOpen={props.isVisible} toggle={props.toggle} modalTransition={{ timeout: 50 }} className="variable-content-modal modal-dialog-centered content-modal settings-create">
                <ModalHeader style={{ width: "100%" }}>
                    {getModalHeader(getHeaderTitle(weightingPlanToCopy))}
                </ModalHeader>
            <ModalBody className={style.modalBody}>
                {getProjectType()}
                {getSubsetSelector(subsetsWithNoWeighting)}
                {getWaveOrWeightTypeSelector()}
            </ModalBody>
            <ModalBody>
                <div className="modal-buttons">
                    <button className="modal-button secondary-button" onClick={closeAndResetModal}>Cancel</button>
                    <button className="modal-button primary-button" disabled={!isValidPlan} onClick={createWeightingPlan}>Next</button>
                </div>
            </ModalBody>
        </Modal>
    );
}

export default CreateWeightingModal;