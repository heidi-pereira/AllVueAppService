import React, {useEffect} from "react";
import {ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle, Modal, ModalBody, ModalHeader} from "reactstrap";
import * as BrandVueApi from "../../../../BrandVueApi";
import {
    CopyWeightingPlanModel,
    ExportRespondentWeightsRequest,
    Factory,
    Subset,
    UiWeightingConfigurationRoot,
    UiWeightingPlanConfiguration
} from "../../../../BrandVueApi";
import {DataSubsetManager} from "../../../../DataSubsetManager";
import Throbber from "../../../throbber/Throbber";
import {toast, Toaster} from 'react-hot-toast';
import WeightingPlansConfigurationPage from "../../../../pages/WeightingPlansConfigurationPage";
import {isWeightingsConfigAccessible, isWeightingsExportAccessible,} from "../../../helpers/FeaturesHelper";
import {saveFile} from "../../../../helpers/FileOperations";
import {MetricSet} from "../../../../metrics/metricSet";
import { IGoogleTagManager } from "../../../../googleTagManager";
import {IEntityConfiguration} from "../../../../entity/EntityConfiguration";
import Tooltip from "../../../Tooltip";
import {PageHandler} from "../../../PageHandler";
import style from "./WeightingSettingsPage.module.less"
import {ProductConfigurationContext} from "../../../../ProductConfigurationContext";
import WeightingSettingsCard from "./WeightingSettingsCard";
import DeleteWeightingModal from "./Modals/DeleteWeightingModal";
import {useNavigate} from "react-router-dom";
import { useReadVueQueryParams } from "../../../helpers/UrlHelper";

interface ISettingsProps {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    entityConfiguration: IEntityConfiguration
    metrics: MetricSet;
    averages: BrandVueApi.IAverageDescriptor[];
}

const WeightingSettingsPage = (props: ISettingsProps) => {
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const weightingPlansClient = Factory.WeightingPlansClient(error => error());
    const [plans, setPlans] = React.useState<UiWeightingConfigurationRoot[]>([]);
    const [isLoading, setIsLoading] = React.useState(true);
    const [createWeightingModalVisible, setCreateWeightingModalVisible] = React.useState(false);
    const [createWeightingPlanSubset, setCreateWeightingPlanSubset] = React.useState<Subset>();
    const [currentlyDeletingWeightingPlan, setCurrentlyDeletingWeightingPlan] = React.useState<UiWeightingConfigurationRoot | null>(null);
    const [subsetDropdownOpen, setSubsetDropdownOpen] = React.useState(false);
    const [weightingPlanToCopy, setWeightingPlanToCopy] = React.useState<UiWeightingConfigurationRoot | null>(null);
    const allSubsets = DataSubsetManager.getAll() || [];
    const navigate = useNavigate();
    const { getQueryParameter } = useReadVueQueryParams();
    const toastError = (error: Error, userFriendlyText: string) => {
        toast.error(userFriendlyText);
        console.log(error);
        setIsLoading(false);
    }

    const setDefaultSubset = (weightingPlans: UiWeightingConfigurationRoot[], subsets: Subset[]) => {
        const availableSubsets = subsets.filter(subset => !weightingPlans.map(wp => wp.subsetId).includes(subset.id));
        setCreateWeightingPlanSubset(availableSubsets.length > 0 ? availableSubsets[0] : allSubsets[0]);
    }

    const getPlans = () => {
        weightingPlansClient.getWeightingPlans().then(p => {
            setPlans(p);
            setDefaultSubset(p, allSubsets);
            setIsLoading(false);
        }).catch((e: Error) => toastError(e, "An error occurred trying to get weightings"));
    }

    useEffect(() => {
        getPlans();
    }, []);

    if (isLoading) {
        return (
            <div className={style.weightingSettingsPage}>
                <div className="throbber-container-fixed">
                    <Throbber />
                </div>
            </div>
        );
    }

    const subsetsWithNoWeighting = allSubsets.filter(s => !plans.map(ws => ws.subsetId).includes(s.id));

    const copyWeightingPlan = (subsetId: string, newSubsetId: string) =>
        weightingPlansClient.copyWeightingPlan(new CopyWeightingPlanModel({
            copyFromSubsetId: subsetId,
            subsetId: newSubsetId
        }));

    const onCopyClick = (e: React.MouseEvent, weightingPlan: UiWeightingConfigurationRoot) => {
        e.stopPropagation();
        setWeightingPlanToCopy(weightingPlan);
        setCreateWeightingModalVisible(true);
    }

    const onDeleteClick = (e: React.MouseEvent, weightingPlan: UiWeightingConfigurationRoot) => {
        e.stopPropagation();
        setCurrentlyDeletingWeightingPlan(weightingPlan);
    }

    const onDeleteModalExit = () => {
        setCurrentlyDeletingWeightingPlan(null);
    }

    const NavigateToWeightingPlan = (newWeightingPlan: UiWeightingConfigurationRoot) => {
        navigate("weighting?subsetId=" + newWeightingPlan.subsetId);
    }

    const createWeightingPlan = () => {
        if (weightingPlanToCopy) {
            copyWeightingPlan(weightingPlanToCopy.subsetId, createWeightingPlanSubset!.id);
            return;
        }

        const emptyWeightingPlan = new UiWeightingPlanConfiguration;
        const newWeightingPlanRoot = new UiWeightingConfigurationRoot;
        newWeightingPlanRoot.subsetId = createWeightingPlanSubset!.id;
        newWeightingPlanRoot.uiWeightingPlans = [emptyWeightingPlan];
        NavigateToWeightingPlan(newWeightingPlanRoot);
    }

    const leaveModal = () => {
        setCreateWeightingModalVisible(false);
        setWeightingPlanToCopy(null);
    }

    const getSubsetSelector = (subsets: Subset[]) => {
        if (subsets.length > 0)
            return (
                <div>
                    <label className="variable-page-label">Survey segment</label>
                    <ButtonDropdown isOpen={subsetDropdownOpen} toggle={() => setSubsetDropdownOpen(!subsetDropdownOpen)} className="metric-dropdown">
                        <DropdownToggle className="metric-selector-toggle toggle-button" disabled={subsets.length === 1 && createWeightingPlanSubset !== undefined}>
                            <span>{createWeightingPlanSubset?.displayName ?? "Choose a segment"}</span>
                            <i className={`${style.symbol} material-symbols-outlined`}>arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu>
                            {subsetsWithNoWeighting.map(subset => {
                                return (<DropdownItem key={subset.id} onClick={() => setCreateWeightingPlanSubset(subset)}>
                                    <div className="name-container">
                                        <span className='title'>{subset.displayName}</span>
                                    </div>
                                </DropdownItem>);
                            })
                            }
                        </DropdownMenu>
                    </ButtonDropdown>
                </div>
            );
    };

    const toggle = () => {
        createWeightingModalVisible ? leaveModal() : setCreateWeightingModalVisible(true);
    }

    const getHeaderTitle = (weightingPlanToCopy: UiWeightingConfigurationRoot | null) => {
        return weightingPlanToCopy ? `Copy weighting from segment ${weightingPlanToCopy.subsetId}` : "Create weighting";
    }

    const getModalHeader = (headerText: string, onClickExit: () => void) => {
        return (
            <div className="settings-modal-header">
                <div className="close-icon">
                    <button type="button" className="close" onClick={onClickExit}>
                        <i className={`${style.symbol} material-symbols-outlined`}>close</i>
                    </button>
                </div>
                <div className="set-name">{headerText}</div>
            </div>
        );
    }

    const openCreateWeightingModal = () => {
        if (subsetsWithNoWeighting.length == 0) {
            toast.error("All segments for this survey already used in other weightings");
        } else {
            setCreateWeightingModalVisible(true);
        }
    }

    const createWeightingModal = () => {
        const isValidPlan = createWeightingPlanSubset;

        return (
            <Modal isOpen={createWeightingModalVisible} toggle={toggle} modalTransition={{ timeout: 50 }} className="variable-content-modal modal-dialog-centered content-modal settings-create">
                <ModalHeader style={{ width: "100%" }}>
                    {getModalHeader(getHeaderTitle(weightingPlanToCopy), leaveModal)}
                </ModalHeader>
                <ModalBody>
                    {getSubsetSelector(subsetsWithNoWeighting)}
                    <div className="modal-buttons">
                        <button className="modal-button secondary-button" onClick={leaveModal}>Cancel</button>
                        <button className="modal-button primary-button auto-width" disabled={!isValidPlan} onClick={createWeightingPlan}>Create weighting</button>
                    </div>
                </ModalBody>
            </Modal>
        );
    };

    const deleteWeightingPlan = (subsetId: string) => {
        weightingPlansClient.deleteWeightingPlanForSubset(subsetId)
            .then(() => setIsLoading(true))
            .catch((e: Error) => toastError(e, "An error occurred trying to delete weighting"));
    }

    const moveToJsonEditing = (e: any) => {
        e.preventDefault();
        const href = `/weightings-configuration`;
        navigate(href);
    }
    const moveToWeightingGeneration = (e: any) => {
        e.preventDefault();
        const href = `/weightings-generation`;
        navigate(href);
    }

    const exportResponseWeightings = (e: any) => {
        const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());

        return weightingAlgorithmsClient.exportRespondentWeights(new ExportRespondentWeightsRequest(
            { subsetIds: [], averageId: "" }))
            .then(r => saveFile(r, "Weightings- All- (Default)- Private.csv"))
            .catch(error => {
                toast.error("Export failed");
            });
    }

    const weightingSubsetId = getQueryParameter<string>("subsetId");
    const subsetForWeighting = allSubsets.find(subset => subset.id == weightingSubsetId);
    const userEnabledSubsets = allSubsets.filter(x => !x.disabled).map(x => x.displayName).join(",");
    if (weightingSubsetId)
        return (
            <WeightingPlansConfigurationPage
                googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler}
                entityConfiguration={props.entityConfiguration}
                subsetId={weightingSubsetId}
                subsetDisplayName={subsetForWeighting ? subsetForWeighting.displayName : weightingSubsetId}
                averages={props.averages}
                />
        );

    return (
        <div className={style.weightingSettingsPage}>
            <Toaster position='bottom-center' toastOptions={{ duration: 5000 }} />
            {currentlyDeletingWeightingPlan && <DeleteWeightingModal
                    isOpen={currentlyDeletingWeightingPlan !== null}
                    toggle={onDeleteModalExit}
                    cancelDelete={() => setCurrentlyDeletingWeightingPlan(null)}
                    deleteWeighting={() => deleteWeightingPlan(currentlyDeletingWeightingPlan.subsetId)}
                />}
            {createWeightingModal()}
            <div className={style.titleBar}>
                <div>
                    <h3>Weightings</h3>
                </div>
                <div className="help-link">
                    <a href="https://docs.savanta.com/allvue/Content/AllVue/Adding_Weightings_to_your_Project.html" target="_blank">
                        <i className={`${style.symbol} material-symbols-outlined no-symbol-fill`}>info</i>
                        <span className="link-text">How weighting works</span>
                    </a>
                </div>
            </div>
            <div className={style.scrollableCardsContainer}>
                <div className={style.cardContainer}>
                    {plans.length ? plans.map(s =>
                        <WeightingSettingsCard
                            averages={props.averages}
                            key={s.subsetId}
                            metrics={props.metrics}
                            root={s}
                            subsetsWithoutWeighting={subsetsWithNoWeighting}
                            allSubsets={allSubsets}
                            onCopyClick={onCopyClick}
                            onDeleteClick={onDeleteClick}
                            navigateToWeightingPlan={NavigateToWeightingPlan}
                            isExportWeightsAvailable={isWeightingsExportAccessible(productConfiguration)}
                        />) : "No weightings have been created for this project."}
                </div>
            </div>
            <div className={style.settingsNavigationButtons}>
                {(subsetsWithNoWeighting.length >= 1) &&
                    <button className="primary-button" onClick={openCreateWeightingModal}>Create new weighting</button>}
                {isWeightingsConfigAccessible(productConfiguration)  &&
                    <button className="secondary-button system-admin" onClick={(e) => moveToJsonEditing(e)}><span><i className={`${style.symbol} material-symbols-outlined`}>lock</i></span>Edit in JSON</button>
                }
                {(isWeightingsConfigAccessible(productConfiguration) && plans.length > 0)  &&
                    <button className="secondary-button system-admin" onClick={(e) => moveToWeightingGeneration(e)}><span><i className={`${style.symbol} material-symbols-outlined`}>lock</i></span>Generate from response weightings</button>
                }

                {(isWeightingsExportAccessible(productConfiguration) && plans.length > 0 && plans.filter(p => (p.uiWeightingPlans && p.uiWeightingPlans.length > 0) ).length > 0) &&
                    <Tooltip placement="top" title={`Export weights for all segements (${userEnabledSubsets}). Caution: This will force all segments to be loaded.`} >
                        <button className="secondary-button admin" onClick={(e) => exportResponseWeightings(e)}><span><i className={`${style.symbol} material-symbols-outlined`}>supervisor_account</i></span>Export response weightings</button>
                    </Tooltip>
                }
            </div>
        </div>
    );
}
export default WeightingSettingsPage;