import React from "react";
import { useEffect } from 'react';
import { Toaster, toast } from 'react-hot-toast';
import style from "./WeightingSettingsPageV3.module.less"
import { DataSubsetManager } from "../../../../DataSubsetManager";
import { Factory, UiWeightingConfigurationRoot, Subset, IAverageDescriptor } from "../../../../BrandVueApi";
import { MetricSet } from "../../../../metrics/metricSet";
import WeightingPlansConfigurationPage from "../../../../pages/WeightingPlansConfigurationPage";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { PageHandler } from "../../../PageHandler";
import { IEntityConfiguration } from "../../../../entity/EntityConfiguration";
import Throbber from "../../../throbber/Throbber";
import CreateWeightingModal from "./Modals/CreateWeightingModal";
import DeleteWeightingModal from "./Modals/DeleteWeightingModal";
import WeightingPlansList from "./WeightingPlansList";
import WeightingImport from "./WeightingImport";
import MaterialSymbol, { MaterialSymbolType, MaterialSymbolStyle } from "./Controls/MaterialSymbol";
import { useReadVueQueryParams } from "../../../helpers/UrlHelper";

export enum WeightingType {
    RIM,
    ImportTracker,
    ImportAdhoc
}

interface IWeightingSettingsPageV3Props {
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    entityConfiguration: IEntityConfiguration
    metrics: MetricSet;
    averages: IAverageDescriptor[];
}

const WeightingSettingsPageV3 = (props: IWeightingSettingsPageV3Props) => {
    const weightingPlansClient = Factory.WeightingPlansClient(error => error());    
    const [plans, setPlans] = React.useState<UiWeightingConfigurationRoot[]>([]);
    const [isLoading, setIsLoading] = React.useState(true);
    const [createWeightingModalVisible, setCreateWeightingModalVisible] = React.useState(false);
    const [currentlyDeletingWeightingPlan, setCurrentlyDeletingWeightingPlan] = React.useState<UiWeightingConfigurationRoot | null>(null);
    const [weightingPlanToCopy, setWeightingPlanToCopy] = React.useState<UiWeightingConfigurationRoot | null>(null);
    const [existingWeightingPlan, setExistingWeightingPlan] = React.useState<UiWeightingConfigurationRoot>();
    const allSubsets = DataSubsetManager.getAll() || [];
    const availableSubsets = allSubsets.filter(subset => !plans.some(plan => plan.subsetId == subset.id))
    const { getQueryParameter } = useReadVueQueryParams();
    const toastError = (error: Error, userFriendlyText: string) => {
        toast.error(userFriendlyText);
        console.log(error);
        setIsLoading(false);
    }

    const getPlans = () => {
        weightingPlansClient.getWeightingPlans().then(p => {
            setPlans(p);
            setIsLoading(false);
        }).catch((e: Error) => toastError(e, "An error occurred trying to get weightings"));
    }

    useEffect(() => {
        getPlans();
    }, []);

    if (isLoading) {
        return (
            <div className={style.weightingSettingsPage}>
                <Throbber inFixedContainer />
            </div>
        );
    }

    const onDeleteClick = (e: React.MouseEvent, weightingPlan: UiWeightingConfigurationRoot) => {
        e.stopPropagation();
        setCurrentlyDeletingWeightingPlan(weightingPlan);
    }

    const onDeleteModalExit = () => {
        setCurrentlyDeletingWeightingPlan(null);
    }

    const onAddWave = (weightingPlan: UiWeightingConfigurationRoot): void => {
        setExistingWeightingPlan(weightingPlan);
        setCreateWeightingModalVisible(true);
    }

    const deleteWeightingPlan = (subsetId: string) => {
        weightingPlansClient.deleteWeightingPlanForSubset(subsetId)
            .then(() => setIsLoading(true))
            .catch((e: Error) => toastError(e, "An error occurred trying to delete weighting"));
    }

    const weightingSubsetId = getQueryParameter<string>("subsetId");
    const weightingImportSubsetId = getQueryParameter<string>("importSubsetId");
    const weightingImportWaveVariableId = getQueryParameter<string>("importWaveVariableId");
    const weightingImportWaveId = getQueryParameter<number>("importWaveId");
    const subsetForWeighting = allSubsets.find(subset => subset.id == weightingSubsetId);

    const pendingRefresh = window.sessionStorage.getItem('pendingRefresh');
    const savedWeightingName = window.sessionStorage.getItem('savedWeightingName');

    if (pendingRefresh !== null) {
        window.sessionStorage.removeItem('pendingRefresh');
    }
    else {
        if (savedWeightingName !== null) {
            window.sessionStorage.removeItem('savedWeightingName');
            const message = savedWeightingName
                ? <p>Weighting has been applied to <strong>{savedWeightingName}</strong> successfully</p>
                : <p>Weighting has been applied successfully</p>;
            toast.success(message);
        };
    }

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

    if (weightingImportSubsetId) {
        return (
            <WeightingImport
                subsetId={weightingImportSubsetId}
                waveVariableIdentifier={weightingImportWaveVariableId ?? ""}
                waveId={weightingImportWaveId}
                metrics={props.metrics}
            />
        )
    }

    const getDocsLink = () => {
        // Temporary until new weighting UI is in use in prod
        const isTestBetaUAT = /.*\.(test|beta|uat)\.all-vue.*/.test(window.location.hostname);
        if (isTestBetaUAT) {
            return "https://docs.savanta.com/internal/Content/AllVue/Adding_Weightings_to_your_Project.html";
        }
        return "https://docs.savanta.com/allvue/Content/AllVue/Adding_Weightings_to_your_Project.html";
    }

    const getHelpLink = () => {
        return (
            <div className="help-link">
                <a href={getDocsLink()} target="_blank">
                    <MaterialSymbol symbolType={MaterialSymbolType.info} symbolStyle={MaterialSymbolStyle.outlined} noFill />
                    <span className="link-text">How weighting works</span>
                </a>
            </div>
        )
    }

    const getAddWeightingButton = () => {
        return (
            <div className={style.controlStrip}>
            <button className="hollow-button" onClick={() => {setCreateWeightingModalVisible(true)}} disabled={availableSubsets.length == 0}>
                < i className="material-symbols-outlined">weight</i>
                Add Weighting
            </button>
        </div>

        );
    }

    const getNoPlansPage = () => {
        return(
            <div className={style.noWeighting}>
                <div className={style.helpLink}>
                    {getHelpLink()}
                </div>
                <div className={style.body}>
                    <div className={style.titleBar}>
                        Weightings
                    </div>
                    <div className={style.text}>
                        <span>No weightings have been added</span>
                    </div>
                    {getAddWeightingButton()}

                </div>
            </div>
        )
    }

    const getPlansPage = () => {
        return (
            <div className={style.weighting}>
                <div className={style.titleBar}>
                    <div>
                        <h3>Weightings</h3>
                    </div>
                    {getHelpLink()}
                </div>
                {getAddWeightingButton()}
                <WeightingPlansList
                    allSubsets={allSubsets}
                    plans={plans}
                    metrics={props.metrics}
                    averages={props.averages}
                    isExportWeightsAvailable={true}
                    onDeleteClick={onDeleteClick}
                    onAddWave={onAddWave}
                />
            </div>
        )
    }

    return (
        <>
            <div className={style.weightingSettingsPage}>
                <Toaster position='bottom-center' toastOptions={{ duration: 5000 }} />
                {currentlyDeletingWeightingPlan && <DeleteWeightingModal
                    isOpen={currentlyDeletingWeightingPlan !== null}
                    toggle={onDeleteModalExit}
                    cancelDelete={() => setCurrentlyDeletingWeightingPlan(null)}
                    deleteWeighting={() => deleteWeightingPlan(currentlyDeletingWeightingPlan.subsetId)}
                    segmentName={currentlyDeletingWeightingPlan.subsetId}
                />}
                <CreateWeightingModal
                    isVisible={createWeightingModalVisible}
                    toggle={() => setCreateWeightingModalVisible(!createWeightingModalVisible)}
                    copyFromWeightingPlan={weightingPlanToCopy}
                    existingWeightingPlan={existingWeightingPlan}
                    plans={plans}
                />

                {plans?.length > 0 ? getPlansPage(): getNoPlansPage()}
            </div>
        </>
    );
}

export default WeightingSettingsPageV3;