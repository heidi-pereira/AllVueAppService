import React, { useState } from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import { ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle, Modal} from "reactstrap";
import { ModalBody } from 'react-bootstrap';
import { ReportWithPage } from '../ReportsPage';
import { useSavedReportsContext } from '../SavedReportsContext';
import toast from 'react-hot-toast';
import { handleError } from 'client/components/helpers/SurveyVueUtils';
import DeleteReportModal from './DeleteReportModal';
import ReportSettingsModalDetails from './ReportSettingsModalDetail';
import ReportSettingsModalOptions from './ReportSettingsModalOptions';
import { TabContent, TabPane, Nav, NavItem, NavLink} from 'reactstrap';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { IApplicationUser, MainQuestionType, CrossMeasure, ReportType, ReportOrder,
    CrosstabSignificanceType, BaseDefinitionType, ReportWaveConfiguration, UpdateReportSettingsRequest,
    ReportOverTimeConfiguration, FeatureCode, IAverageDescriptor, SigConfidenceLevel,
    DisplaySignificanceDifferences ,PermissionFeaturesOptions} from '../../../../BrandVueApi';
import CopyReportModal from './CopyReportModal';
import ReportSettingsModalFilters from './ReportSettingsModalFilters';
import { getMetricFilterFromDefault, IFilterAndDefaultInstances } from '../Filtering/FilterHelper';
import { getUrlSafePageName } from '../../../helpers/UrlHelper';
import ReportWavesPicker from '../Components/ReportWavesPicker';
import { PageHandler } from '../../../PageHandler';
import { hasAllVuePermissionsOrSystemAdmin , isFeatureEnabled } from '../../../helpers/FeaturesHelper';
import { ProductConfigurationContext } from '../../../../ProductConfigurationContext';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import BreaksPicker from "../../BreakPicker/BreaksPicker";
import { useMetricStateContext } from '../../../../metrics/MetricStateContext';
import ReportOverTimeSettingsPicker from '../Components/ReportOverTimeSettingsPicker';
import { ApplicationConfiguration } from '../../../../ApplicationConfiguration';
import { BreakPickerParent } from '../../BreakPicker/BreaksDropdownHelper';
import { getNewShownSignificanceDifferences } from '../Components/SigDiffHelper';
import { useAppSelector } from '../../../../state/store';
import { selectSubsetId } from '../../../../state/subsetSlice';
import ReportTemplateModal from './ReportTemplateModal';
import FeatureGuard from "../../../../components/FeatureGuard/FeatureGuard";
import BrandVueOnlyLowSampleHelper from '../../BrandVueOnlyLowSampleHelper';
import { selectDefaultReportId } from 'client/state/reportSlice';

interface IReportSettingsModalProps {
    isOpen: boolean;
    setIsOpen(isOpen: boolean): void;
    currentReportPage?: ReportWithPage;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    user: IApplicationUser | null;
    questionTypeLookup: {[key: string]: MainQuestionType};
    reportsPageUrl: string;
    activeTab: ReportSettingsModalTabSelection;
    setActiveTab(activeTab: ReportSettingsModalTabSelection): void;
    applicationConfiguration: ApplicationConfiguration;
    averages: IAverageDescriptor[];
    reportPartsHaveBreaks: boolean;
}

export enum ReportSettingsModalTabSelection {
    Details,
    Filters,
    Breaks,
    Options,
    OverTime,
};

const ReportSettingsModal = (props: IReportSettingsModalProps) => {
    const [reportName, setReportName] = React.useState<string>("");
    const [shareReport, setShareReport] = React.useState<boolean>(false);
    const [isDefault, setIsDefault] = React.useState<boolean>(false);
    const [order, setOrder] = React.useState<ReportOrder>(ReportOrder.ResultOrderDesc);
    const [isDeleteModalOpen, setDeleteModalOpen] = React.useState<boolean>(false);
    const [isCopyModalOpen, setCopyModalOpen] = React.useState<boolean>(false);
    const [decimalPlaces, setDecimalPlaces] = React.useState<number>(0);
    const [overTimeConfig, setOverTimeConfig] = React.useState<ReportOverTimeConfiguration | undefined>(undefined);
    const [waves, setWaves] = React.useState<ReportWaveConfiguration | undefined>(undefined);
    const [breaks, setBreaks] = React.useState<CrossMeasure[]>([]);
    const [includeCounts, setIncludeCounts] = React.useState<boolean>(false);
    const [lowSampleThreshold, setLowSampleThreshold] = React.useState<number>(BrandVueOnlyLowSampleHelper.lowSampleForEntity);
    const [calculateIndexScores, setCalculateIndexScores] = React.useState<boolean>(false);
    const [isWeighted, setIsDataWeighted] = React.useState<boolean>(true);
    const [weightingStatus, setWeightingStatus] = React.useState<BrandVueApi.WeightingStatus>(BrandVueApi.WeightingStatus.NoWeightingConfigured);
    const [hideEmptyRows, setHideEmptyRows] = React.useState<boolean>(true);
    const [hideEmptyColumns, setHideEmptyColumns] = React.useState<boolean>(true);
    const [hideTotalColumn, setHideTotalColumn] = React.useState<boolean>(false);
    const [hideDataLabels, setHideDataLabels] = React.useState<boolean>(false);
    const [showMultipleTablesAsSingle, setShowMultipleTablesAsSingle] = React.useState<boolean>(false);
    const [highlightLowSample, setHighlightLowSample] = React.useState<boolean>(false);
    const [highlightSignificance, setHighlightSignificance] = React.useState<boolean>(false);
    const [displaySignificanceDifferences, setDisplaySignificanceDifferences] = React.useState<DisplaySignificanceDifferences>(DisplaySignificanceDifferences.ShowBoth);
    const [significanceType, setSignificanceType] = React.useState<CrosstabSignificanceType>(CrosstabSignificanceType.CompareToTotal);
    const [singlePageExport, setSinglePageExport] = React.useState<boolean>(false);
    const [baseTypeOverride, setBaseTypeOverride] = React.useState<BaseDefinitionType>(BaseDefinitionType.SawThisChoice);
    const [baseVariableId, setBaseVariableId] = React.useState<number | undefined>(undefined);
    const [defaultFilters, setDefaultFilters] = React.useState<IFilterAndDefaultInstances[]>([]);
    const [selectedSubsetId, setSelectedSubsetId] = React.useState<string>(useAppSelector(selectSubsetId));
    const weightingsConfigClient = BrandVueApi.Factory.WeightingPlansClient(error => error());
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const [significanceLevel, setSignificanceLevel] = React.useState<SigConfidenceLevel>(SigConfidenceLevel.NinetyFive);
    const [isHamburgerOpen, setIsHamburgerOpen] = useState<boolean>(false);
    const [isTemplateModalOpen, setTemplateModalOpen] = React.useState<boolean>(false);

    const { reportsDispatch } = useSavedReportsContext();
    const defaultReportId = useAppSelector(selectDefaultReportId);
    
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { metricsForReports } = useMetricStateContext();

    const convertDefaultFiltersToMetricFilters = (filters: BrandVueApi.DefaultReportFilter[]): IFilterAndDefaultInstances[] => {
        return filters.map(f => getMetricFilterFromDefault(f, metricsForReports, entityConfiguration));
    }

    const convertMetricFiltersToDefaultFilters = (filter: IFilterAndDefaultInstances): BrandVueApi.DefaultReportFilter => {
        if (!filter.metric) {
            throw new Error("Cannot save filter without a metric defined");
        }

        if (!filter.filters) {
            throw new Error("Cannot save filter without instances defined");
        }

        var defaultFilter = new BrandVueApi.DefaultReportFilter();
        defaultFilter.measureName = filter.metric.name;
        defaultFilter.filters = [];

        filter.filters.forEach(filter => {
            defaultFilter.filters.push(new BrandVueApi.DefaultReportFilterInstance({
                entityInstances: filter.entityInstances,
                values: filter.values!,
                invert: filter.invert,
                treatPrimaryValuesAsRange: filter.treatPrimaryValuesAsRange
            }));
        });

        return defaultFilter;
    }

    React.useEffect(() => {
        setWeightingStatus(BrandVueApi.WeightingStatus.NoWeightingConfigured);
        if (selectedSubsetId?.trim()) {
            weightingsConfigClient.isWeightingPlanDefinedAndValid(selectedSubsetId).then(
                details => {
                    setWeightingStatus(details.status);
                }
            ).catch((e: Error) => handleError(e));
        }
    }, [selectedSubsetId]);

    const reduxSubsetId = useAppSelector(selectSubsetId);

    React.useEffect(() => {
        if(!props.currentReportPage?.page) {
            return;
        }
        if (props.isOpen) {
            setReportName(props.currentReportPage.page.displayName);
            setShareReport(props.currentReportPage.report.isShared);
            setIsDefault(props.currentReportPage.report.savedReportId === defaultReportId);
            setOrder(props.currentReportPage.report.reportOrder);
            setDecimalPlaces(props.currentReportPage.report.decimalPlaces);
            setOverTimeConfig(props.currentReportPage.report.overTimeConfig);
            setWaves(props.currentReportPage.report.waves);
            setBreaks(props.currentReportPage.report.breaks);
            setSinglePageExport(props.currentReportPage.report.singlePageExport);
            setIsDataWeighted(props.currentReportPage.report.isDataWeighted);
            setHideEmptyRows(props.currentReportPage.report.hideEmptyRows);
            setHideEmptyColumns(props.currentReportPage.report.hideEmptyColumns);
            setHideTotalColumn(props.currentReportPage.report.hideTotalColumn);
            setHideDataLabels(props.currentReportPage.report.hideDataLabels);
            setShowMultipleTablesAsSingle(props.currentReportPage.report.showMultipleTablesAsSingle);
            setIncludeCounts(props.currentReportPage.report.includeCounts);
            setCalculateIndexScores(props.currentReportPage.report.calculateIndexScores);
            setHighlightLowSample(props.currentReportPage.report.highlightLowSample);
            setHighlightSignificance(props.currentReportPage.report.highlightSignificance);
            setDisplaySignificanceDifferences(props.currentReportPage.report.displaySignificanceDifferences);
            //Historically chart reports have not stored significance type but they MUST be comparetotal
            setSignificanceType(props.currentReportPage.report.reportType == ReportType.Table ? props.currentReportPage.report.significanceType : CrosstabSignificanceType.CompareToTotal);
            setBaseTypeOverride(props.currentReportPage.report.baseTypeOverride);
            setBaseVariableId(props.currentReportPage.report.baseVariableId);
            setDefaultFilters(convertDefaultFiltersToMetricFilters(props.currentReportPage.report.defaultFilters));
            setSelectedSubsetId(props.currentReportPage.report.subsetId ?? reduxSubsetId);
            setSignificanceLevel(props.currentReportPage.report.sigConfidenceLevel);
            setLowSampleThreshold(props.currentReportPage.report.lowSampleThreshold ?? BrandVueOnlyLowSampleHelper.lowSampleForEntity);
        }
    }, [JSON.stringify(props.currentReportPage), props.isOpen, reduxSubsetId]);

    const closeModal = () => {
        props.setIsOpen(false);
        setReportName("");
        setShareReport(false);
        setIsDefault(false);
        setDecimalPlaces(0);
        props.setActiveTab(ReportSettingsModalTabSelection.Details);
    }


    async function updateReport() {
        props.googleTagManager.addEvent("reportsPageUpdateReportSettings", props.pageHandler);
        reportsDispatch({type: "UPDATE_REPORT_SETTINGS", data: new UpdateReportSettingsRequest({
            pageDisplayName: reportName,
            pageName: getUrlSafePageName(reportName),
            savedReportId: props.currentReportPage!.report.savedReportId,
            isShared: shareReport,
            isDefault: isDefault,
            order : order,
            decimalPlaces: decimalPlaces,
            waves: waves,
            breaks: breaks,
            includeCounts: includeCounts,
            calculateIndexScores: calculateIndexScores,
            isDataWeighted: isWeighted,
            hideEmptyRows: hideEmptyRows,
            hideEmptyColumns: hideEmptyColumns,
            hideTotalColumn: hideTotalColumn,
            hideDataLabels: hideDataLabels,
            showMultipleTablesAsSingle: showMultipleTablesAsSingle,
            highlightLowSample: highlightLowSample,
            highlightSignificance: highlightSignificance,
            significanceType: significanceType,
            sigConfidenceLevel: significanceLevel,
            singlePageExport: singlePageExport,
            baseTypeOverride: baseTypeOverride,
            baseVariableId: baseVariableId,
            defaultFilters: defaultFilters.map(f => convertMetricFiltersToDefaultFilters(f)),
            modifiedGuid: props.currentReportPage!.report.modifiedGuid,
            overTimeConfig: overTimeConfig,
            displaySignificanceDifferences: displaySignificanceDifferences,
            subsetId: selectedSubsetId,
            lowSampleThreshold: lowSampleThreshold == productConfiguration.lowSampleForBrand ? undefined : lowSampleThreshold
        })}).then(() => {
            toast.success("Updated report");
            closeModal();
            if (selectedSubsetId !== props.currentReportPage?.report.subsetId) {
                window.location.reload();
            }
        }).catch(error => toast.error("Saving report changes failed, please try again"));
    }

    const canSaveChanges = (): boolean | undefined => {
        const breaksAreInvalid = props.currentReportPage?.report.reportType === ReportType.Chart && breaks.length > 0 && breaks[0].filterInstances.length === 0;
        const filtersAreInvalid = defaultFilters.some(f => !f.metric || !f.filters);
        return reportName != null && reportName.length > 0 && !breaksAreInvalid && !filtersAreInvalid;
    }

    const closeDeleteModal = (isDeleted: boolean) => {
        setDeleteModalOpen(false);
        if (isDeleted) {
            closeModal();
        }
    }

    const closeAllModals = () => {
        setTemplateModalOpen(false);
        closeModal();
    }

    const updateDisplaySigDiff = (toggledDisplaySignificanceDifferences: DisplaySignificanceDifferences) => {
        const newDisplayedSigDiff = getNewShownSignificanceDifferences(
            toggledDisplaySignificanceDifferences,
            displaySignificanceDifferences
        );
        setDisplaySignificanceDifferences(newDisplayedSigDiff);
    }

    const isOverTimeFeatureEnabled = isFeatureEnabled(FeatureCode.Overtime_data);
    const canShowOverTime = props.currentReportPage?.report.reportType === ReportType.Chart && isOverTimeFeatureEnabled;
    const canShowWaves = props.currentReportPage?.report.reportType === ReportType.Chart;
    const isUsingOvertime = isOverTimeFeatureEnabled && overTimeConfig != undefined;
    
    return (
        <Modal isOpen={props.isOpen} className="report-settings-modal" centered keyboard={false} autoFocus={false}>
            <DeleteReportModal
                isOpen={isDeleteModalOpen}
                reportPage={props.currentReportPage!}
                googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler}
                reportsPageUrl={props.reportsPageUrl}
                closeModal={closeDeleteModal}
            />
            <CopyReportModal
                reportPage={props.currentReportPage!}
                isOpen={isCopyModalOpen}
                setIsOpen={setCopyModalOpen}
            />
            <ReportTemplateModal
                isOpen={isTemplateModalOpen}
                setIsOpen={setTemplateModalOpen}
                closeAll={closeAllModals}
                selectedReportId={props.currentReportPage!.report.savedReportId}
                selectedReportName={props.currentReportPage!.page.displayName}
            />
            <ModalBody>
                <div className="modal-buttons">
                    <ButtonDropdown className="modal-burger" isOpen={isHamburgerOpen} toggle={() => setIsHamburgerOpen(!isHamburgerOpen)}>
                        <DropdownToggle className={"btn-menu styled-toggle"}>
                                <i className="material-symbols-outlined">more_vert</i>
                            </DropdownToggle>
                        <DropdownMenu end>
                            <FeatureGuard permissions={[PermissionFeaturesOptions.ReportsDelete]} >
                                <DropdownItem className="dropdown-item" onClick={() => setDeleteModalOpen(true)}>
                                    <i className="material-symbols-outlined" >delete</i>Delete report
                                </DropdownItem>
                            </FeatureGuard>
                            <DropdownItem onClick={() => setCopyModalOpen(true)}>
                                <i className="material-symbols-outlined" >content_copy</i>Copy as new report
                            </DropdownItem>
                            <DropdownItem onClick={(e) => {setTemplateModalOpen(true)}}>
                                <i className="material-symbols-outlined">addchart</i>Create template from report
                            </DropdownItem>
                        </DropdownMenu>
                    </ButtonDropdown>
                    <button onClick={closeModal} className="modal-close-button" title="Close">
                        <i className="material-symbols-outlined">close</i>
                    </button>
                </div>
                <div className="header">
                    Report settings
                </div>
                <Nav tabs>
                    <NavItem>
                        <NavLink className={props.activeTab === ReportSettingsModalTabSelection.Details ? 'tab-active' : 'tab-item'}
                            onClick={() => { props.setActiveTab(ReportSettingsModalTabSelection.Details); }}>
                            Details
                        </NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink className={props.activeTab === ReportSettingsModalTabSelection.Filters ? 'tab-active' : 'tab-item'}
                            onClick={() => { props.setActiveTab(ReportSettingsModalTabSelection.Filters) }}>
                            Filters
                        </NavLink>
                    </NavItem>
                    {(canShowWaves || canShowOverTime) &&
                        <NavItem>
                            <NavLink className={props.activeTab === ReportSettingsModalTabSelection.OverTime ? 'tab-active' : 'tab-item'}
                                onClick={() => { props.setActiveTab(ReportSettingsModalTabSelection.OverTime); }}>
                                {isOverTimeFeatureEnabled ? "Over time" : "Waves"}
                            </NavLink>
                        </NavItem>
                    }
                    <NavItem>
                        <NavLink className={props.activeTab === ReportSettingsModalTabSelection.Breaks ? 'tab-active' : 'tab-item'}
                            onClick={() => { props.setActiveTab(ReportSettingsModalTabSelection.Breaks); }}>
                            Breaks
                        </NavLink>
                    </NavItem>
                    <NavItem>
                        <NavLink className={props.activeTab === ReportSettingsModalTabSelection.Options ? 'tab-active' : 'tab-item'}
                            onClick={() => { props.setActiveTab(ReportSettingsModalTabSelection.Options); }}>
                            Options
                        </NavLink>
                    </NavItem>
                </Nav>
                <div className="content-and-buttons">
                    <TabContent activeTab={props.activeTab}>
                        <TabPane tabId={ReportSettingsModalTabSelection.Details}>
                            <ReportSettingsModalDetails
                                reportName={reportName}
                                isDefaultReport={isDefault}
                                shareReport={shareReport}
                                setReportName={setReportName}
                                setIsDefault={setIsDefault}
                                setShareReport={setShareReport}
                                idPrefix="report-settings-modal"
                                subsetId={selectedSubsetId}
                                onSubsetChange={(sId) => setSelectedSubsetId(sId)}
                            />
                        </TabPane>
                        <TabPane tabId={ReportSettingsModalTabSelection.Filters}>
                            <ReportSettingsModalFilters
                                questionTypeLookup={props.questionTypeLookup}
                                filtersForReport={defaultFilters}
                                setFiltersForReport={setDefaultFilters}
                            />
                        </TabPane>
                        <TabPane tabId={ReportSettingsModalTabSelection.Options}>
                            <ReportSettingsModalOptions
                                setOrder={setOrder}
                                setDecimalPlaces={setDecimalPlaces}
                                order={order}
                                decimalPlaces={decimalPlaces}
                                includeCounts={includeCounts}
                                isWeighted={isWeighted}
                                hideEmptyRows={hideEmptyRows}
                                hideEmptyColumns={hideEmptyColumns}
                                hideTotalColumn={hideTotalColumn}
                                hideDataLabels={hideDataLabels}
                                showMultipleTablesAsSingle={showMultipleTablesAsSingle}
                                weightingStatus={weightingStatus}
                                highlightLowSample={highlightLowSample}
                                highlightSignificance={highlightSignificance}
                                hiddenSignificanceDifferences={displaySignificanceDifferences}
                                significanceType={significanceType}
                                singlePageExport={singlePageExport}
                                baseTypeOverride={baseTypeOverride}
                                baseVariableId={baseVariableId}
                                lowSampleThreshold={lowSampleThreshold}
                                setLowSampleThreshold={setLowSampleThreshold}
                                setIncludeCounts={setIncludeCounts}
                                setIsDataWeighted={setIsDataWeighted}
                                setHideEmptyRows={setHideEmptyRows}
                                setHideEmptyColumns={setHideEmptyColumns}
                                setHideTotalColumn={setHideTotalColumn}
                                setHideDataLabels={setHideDataLabels}
                                setShowMultipleTablesAsSingle={setShowMultipleTablesAsSingle}
                                setHighlightLowSample={setHighlightLowSample}
                                setHighlightSignificance={setHighlightSignificance}
                                setHiddenSignificanceDifferences={updateDisplaySigDiff}
                                setSignificanceType={setSignificanceType}
                                setSinglePageExport={setSinglePageExport}
                                setBaseTypeOverride={setBaseTypeOverride}
                                setBaseVariableId={setBaseVariableId}
                                reportType={props.currentReportPage!.report.reportType}
                                canCreateNewBase={hasAllVuePermissionsOrSystemAdmin(productConfiguration, [BrandVueApi.PermissionFeaturesOptions.VariablesCreate])}
                                significanceLevel={significanceLevel}
                                setSignificanceLevel={setSignificanceLevel}
                                hasBreaksApplied={props.reportPartsHaveBreaks || breaks.length > 0}
                                selectedReportId={props.currentReportPage!.report.savedReportId}
                                calculateIndexScores={calculateIndexScores}
                                setCalculateIndexScores={(show: boolean) => setCalculateIndexScores(show)}
                            />
                        </TabPane>
                        {(canShowWaves || canShowOverTime) &&
                            <TabPane tabId={ReportSettingsModalTabSelection.OverTime}>
                                <div className={"overtime-tab"}>
                                    {canShowOverTime &&
                                        <div className="bordered-section">
                                            <label className="report-label">Time series</label>
                                            <ReportOverTimeSettingsPicker
                                                applicationConfiguration={props.applicationConfiguration}
                                                config={overTimeConfig}
                                                setConfig={setOverTimeConfig}
                                                isDataWeighted={isWeighted}
                                                unsavedSubsetId={selectedSubsetId}
                                                disabled={waves != undefined} />
                                        </div>
                                    }
                                    <div className="waves-wrapper bordered-section">
                                        <label className="report-label">Waves</label>
                                        <ReportWavesPicker
                                            isDisabled={isUsingOvertime}
                                            isReportSettings={true}
                                            questionTypeLookup={props.questionTypeLookup}
                                            waveConfig={waves}
                                            updateWaves={setWaves}
                                        />
                                    </div>
                                </div>
                            </TabPane>
                        }
                        <TabPane tabId={ReportSettingsModalTabSelection.Breaks}>
                            {isUsingOvertime &&
                                <div className="warning-box"><i className="material-symbols-outlined">warning</i>Breaks cannot currently be used in combination with time series</div>
                            }
                            <BreaksPicker 
                                selectedBreaks={breaks} 
                                setSelectedBreaks={setBreaks} 
                                googleTagManager={props.googleTagManager} 
                                pageHandler={props.pageHandler} 
                                groupCustomVariables={true}
                                user={props.user}
                                reportType={props.currentReportPage!.report.reportType}
                                isReportSettings={true}
                                supportMultiBreaks={false}
                                displayBreakInstanceSelector={true}
                                parentComponent={BreakPickerParent.Modal}
                                canSaveAndLoad={true}
                                isDisabled={isUsingOvertime}
                            />
                        </TabPane>
                    </TabContent>
                    <div className="modal-buttons">
                        <button className="modal-button secondary-button" onClick={closeModal}>Cancel</button>
                        <button className="modal-button primary-button" onClick={updateReport} disabled={!canSaveChanges()}>Save changes</button>
                    </div>
                </div>
            </ModalBody>
        </Modal>
    )
}

export default ReportSettingsModal;