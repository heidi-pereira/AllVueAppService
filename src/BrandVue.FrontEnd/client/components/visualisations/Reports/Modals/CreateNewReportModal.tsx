import React from 'react';
import { Modal } from "reactstrap";
import { ModalBody } from 'react-bootstrap';
import { Metric } from '../../../../metrics/metric';
import toast from "react-hot-toast";
import { CreateNewReportPage } from '../Utility/ReportPageBuilder';
import { useSavedReportsContext } from '../SavedReportsContext';
import { IAverageDescriptor, CreateNewReportRequest, Factory, FeatureCode, IApplicationUser, MainQuestionType,
    ReportOrder, ReportOverTimeConfiguration, ReportType, ReportWaveConfiguration, SwaggerException, 
    AdditionalReportSettings} from '../../../../BrandVueApi';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { getUrlSafePageName } from '../../../helpers/UrlHelper';
import { PageHandler } from '../../../PageHandler';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import { isFeatureEnabled } from '../../../helpers/FeaturesHelper';
import { ApplicationConfiguration } from '../../../../ApplicationConfiguration';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { selectSubsetId } from '../../../../state/subsetSlice';
import { fetchTemplatesForUser } from '../../../../state/templatesSlice';
import DetailsStep from './components/DetailsStep';
import QuestionsStep from './components/QuestionsStep';
import OverTimeStep from './components/OverTimeStep';
import ChooseTemplateStep from './components/ChooseTemplateStep';
import { MixPanel } from '../../../../components/mixpanel/MixPanel';
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import { fetchVariableConfiguration } from '../../../../state/variableConfigurationsSlice';
import { useCrosstabPageStateContext } from '../../Crosstab/CrosstabPageStateContext';

interface ICreateNewReportModal {
    user: IApplicationUser | null;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    isCreateNewReportModalVisible: boolean;
    setCreateNewReportModalVisibility(isVisible: boolean): void;
    questionTypeLookup: { [key: string]: MainQuestionType };
    applicationConfiguration: ApplicationConfiguration;
    averages: IAverageDescriptor[];
    preSelectedMetric?: Metric;
}

enum CreateReportModalPage {
    Details,
    Questions,
    OverTime,
    ChooseTemplate
};

const CreateNewReportModal = (props: ICreateNewReportModal) => {
    const dispatch = useAppDispatch();
    const [selectedMetrics, setSelectedMetrics] = React.useState<Metric[]>(props.preSelectedMetric ? [props.preSelectedMetric] : []);
    const [reportName, setReportName] = React.useState<string>("");
    const [shareReport, setShareReport] = React.useState<boolean>(true);
    const [isDefault, setIsDefault] = React.useState<boolean>(false);
    const [modalPage, setModalPage] = React.useState(CreateReportModalPage.Details);
    const [reportType, setReportType] = React.useState<ReportType>(props.preSelectedMetric ? ReportType.Table : ReportType.Chart);
    const { reportsDispatch } = useSavedReportsContext();
    const [isCreatingReport, setIsCreatingReport] = React.useState<boolean>(false);
    const [waves, setWaves] = React.useState<ReportWaveConfiguration | undefined>(undefined);
    const [overTimeConfig, setOverTimeConfig] = React.useState<ReportOverTimeConfiguration | undefined>(undefined);
    const [isDataWeighted, setIsDataWeighted] = React.useState<boolean>(false);
    const [createFromTemplate, setCreateFromTemplate] = React.useState<boolean>(false);
    const { metricsForReports } = useMetricStateContext();
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const subsetId = useAppSelector(selectSubsetId);
    const isOverTimeFeatureEnabled = isFeatureEnabled(FeatureCode.Overtime_data);
    const canPickOverTime = reportType === ReportType.Chart && isOverTimeFeatureEnabled;
    const canPickWaves = reportType === ReportType.Chart;
    const numberOfPages = canPickWaves || canPickOverTime ? 3 : 2;
    const isCreatingFromDataTab = !props.preSelectedMetric;
    
    const { entityConfigurationDispatch } = useEntityConfigurationStateContext();
    const { metricsDispatch } = useMetricStateContext();
    const { crosstabPageState } = useCrosstabPageStateContext();

    const applyMetricSettingsToReport = props.preSelectedMetric != null;

    React.useEffect(() => {
        setSelectedMetrics(props.preSelectedMetric ? [props.preSelectedMetric] : []);
        setReportType(props.preSelectedMetric ? ReportType.Table : ReportType.Chart);
    }, [props.preSelectedMetric]);

    React.useEffect(() => {
        dispatch(fetchTemplatesForUser());
    }, [dispatch]);

    React.useEffect(() => {
        const weightingsConfigClient = Factory.WeightingPlansClient(error => error());
        weightingsConfigClient.isWeightingPlanDefinedAndValid(subsetId)
            .then(details => setIsDataWeighted(details.isValid))
            .catch((e: Error) => handleError(() => { throw e }));
    }, [subsetId]);

    async function createReport() {
        setIsCreatingReport(true);
        const hasWaves = waves?.waves != null;
        const page = CreateNewReportPage(reportName, selectedMetrics, entityConfiguration, props.questionTypeLookup, reportType, hasWaves);
        let crosstabStateOverride: AdditionalReportSettings | undefined;
        if(applyMetricSettingsToReport) {
            const base = crosstabPageState.metricBaseLookup?.[props.preSelectedMetric!.name];

             crosstabStateOverride = new AdditionalReportSettings({
                calculateIndexScores: crosstabPageState.calculateIndexScores,
                includeCounts: crosstabPageState.includeCounts,
                weightingEnabled: isDataWeighted,
                highlightLowSample: crosstabPageState.highlightLowSample,
                highlightSignificance: crosstabPageState.highlightSignificance,
                displaySignificanceDifferences: crosstabPageState.displaySignificanceDifferences,
                displayMeanValues: crosstabPageState.displayMeanValues,
                significanceType: crosstabPageState.significanceType,
                resultSortingOrder: crosstabPageState.resultSortingOrder,
                decimalPlaces: crosstabPageState.decimalPlaces,
                selectedAverages: crosstabPageState.selectedAverages,
                categories: crosstabPageState.categories,
                sigConfidenceLevel: crosstabPageState.sigConfidenceLevel,
                hideTotalColumn: crosstabPageState.hideTotalColumn,
                showMultipleTablesAsSingle: crosstabPageState.showMultipleTablesAsSingle,
                baseTypeOverride: base?.baseType,
                baseVariableId: base?.baseVariableId
            });
        }

        const request = new CreateNewReportRequest({
            reportType: reportType,
            isShared: shareReport,
            isDefault: isDefault,
            page: page,
            order: ReportOrder.ScriptOrderDesc,
            waves: waves,
            overTimeConfig: overTimeConfig,
            subsetId: subsetId,
            additionalReportSettings: crosstabStateOverride,
        });

        if(request.reportType === ReportType.Chart) {
            MixPanel.track("chartReportCreated");
        }
        else if (request.reportType === ReportType.Table) {
            MixPanel.track("tableReportCreated");
        }
        props.googleTagManager.addEvent(request.reportType === ReportType.Chart ? 'reportsPageCreateNewChart' : 'reportsPageCreateNewTable', props.pageHandler);

        reportsDispatch({ type: "CREATE_REPORT", data: request })
            .then(() => {
                toast.success(`Created report ${reportName}`);
                closeModal();
            })
            .catch(error => handleError(error))
            .finally(() => setIsCreatingReport(false));
    }

    async function createReportFromTemplate(selectedTemplateId: number) {
        setIsCreatingReport(true);
        reportsDispatch({ type: "CREATE_REPORT_FROM_TEMPLATE", data: { templateId: selectedTemplateId, reportName: reportName } })
            .then(() => {
                metricsDispatch({ type: 'RELOAD_METRICS' });
                dispatch(fetchVariableConfiguration()).unwrap();
                entityConfigurationDispatch({type: "RELOAD_ENTITYCONFIGURATION"});
                dispatch({ type: 'RELOAD_ENTITIES' });

                MixPanel.track("templateUsedToCreateNewReport");
                toast.success(`Created report from template`);
                closeModal();
            })
            .catch(error => handleError(error))
            .finally(() => setIsCreatingReport(false));
    }

    async function validateReportName(): Promise<boolean> {
        const client = Factory.SavedReportClient(error => error());
        return client.reportPageNameAlreadyExists(getUrlSafePageName(reportName), null);
    }


    const nextPageFromDetailsStep = () => {
        validateReportName()
            .then(nameAlreadyExists => {
                if (nameAlreadyExists) {
                    toast.error(`Report already exists with name: ${reportName}`);
                } else {
                    createFromTemplate ? setModalPage(CreateReportModalPage.ChooseTemplate): setModalPage(CreateReportModalPage.Questions);
                }
            })
            .catch(error => handleError(error));
    }

    const closeModal = () => {
        props.setCreateNewReportModalVisibility(false);
        setSelectedMetrics([]);
        setReportName("");
        setShareReport(true);
        setIsDefault(false);
        setModalPage(CreateReportModalPage.Details);
        setReportType(ReportType.Chart);
        setWaves(undefined);
        setOverTimeConfig(undefined);
        setCreateFromTemplate(false);
    }

    const toggleShareReport = () => {
        if (shareReport) {
            setIsDefault(false);
        }
        setShareReport(!shareReport);
    }

    const getBodyContent = () => {
        switch (modalPage) {
            case CreateReportModalPage.Details:
                return (
                    <DetailsStep
                        numberOfPages={numberOfPages}
                        reportName={reportName}
                        setReportName={setReportName}
                        reportType={reportType}
                        setReportType={setReportType}
                        shareReport={shareReport}
                        toggleShareReport={toggleShareReport}
                        isDefault={isDefault}
                        setIsDefault={setIsDefault}
                        onCancel={closeModal}
                        onNext={nextPageFromDetailsStep}
                        createFromTemplate={createFromTemplate}
                        setCreateFromTemplate={setCreateFromTemplate}
                        isCreatingFromDataTab={isCreatingFromDataTab}
                    />
                );
            case CreateReportModalPage.Questions:
                return (
                    <QuestionsStep
                        numberOfPages={numberOfPages}
                        isCreatingReport={isCreatingReport}
                        metricsForReports={metricsForReports}
                        selectedMetrics={selectedMetrics}
                        setSelectedMetrics={setSelectedMetrics}
                        canPickWaves={canPickWaves}
                        onBack={() => setModalPage(CreateReportModalPage.Details)}
                        onNext={() => setModalPage(CreateReportModalPage.OverTime)}
                        onCreate={createReport}
                    />
                );
            case CreateReportModalPage.OverTime:
                return (
                    <OverTimeStep
                        numberOfPages={numberOfPages}
                        isOverTimeFeatureEnabled={isOverTimeFeatureEnabled}
                        isCreatingReport={isCreatingReport}
                        applicationConfiguration={props.applicationConfiguration}
                        overTimeConfig={overTimeConfig}
                        setOverTimeConfig={setOverTimeConfig}
                        waves={waves}
                        setWaves={setWaves}
                        questionTypeLookup={props.questionTypeLookup}
                        subsetId={subsetId ?? ""}
                        isDataWeighted={isDataWeighted}
                        onBack={() => setModalPage(CreateReportModalPage.Questions)}
                        onCreate={createReport}
                    />
                );
            case CreateReportModalPage.ChooseTemplate:
                return (
                    <ChooseTemplateStep
                        isCreatingReport={isCreatingReport}
                        applicationConfiguration={props.applicationConfiguration}
                        reportName={reportName}
                        onBack={() => setModalPage(CreateReportModalPage.Details)}
                        onCreate={(selectedTemplateId) => createReportFromTemplate(selectedTemplateId)}
                    />
                )
            default:
                return null;
        }
    };

    return (
        <Modal isOpen={props.isCreateNewReportModalVisible} centered={true} className="report-modal" keyboard={false} autoFocus={false}>
            <ModalBody>
                <button onClick={closeModal} className="modal-close-button">
                    <i className="material-symbols-outlined">close</i>
                </button>
                <div className="header">
                    Create new report
                </div>
                {getBodyContent()}
            </ModalBody>
        </Modal>
    );
};

export default CreateNewReportModal;