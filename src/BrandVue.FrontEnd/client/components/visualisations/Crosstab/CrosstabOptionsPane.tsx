import React from "react";
import { useState } from "react";
import {
    ReportOrder,
    BaseDefinitionType,
    CrosstabSignificanceType,
    AverageType,
    CalculationType,
    ReportType,
    MainQuestionType,
    DisplaySignificanceDifferences,
    SigConfidenceLevel} from "../../../BrandVueApi";
import * as BrandVueApi from "../../../BrandVueApi";
import {IGoogleTagManager} from "../../../googleTagManager";
import BrandVueOnlyLowSampleHelper from "../BrandVueOnlyLowSampleHelper";
import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import { descriptionOfOrder, getAnalyticsAverageAddedEvent, getAnalyticsAverageRemovedEvent, hasSingleEntityInstance, getMixPanelAddAverageEvent,
    getMixPanelRemoveAverageEvent } from "../../../components/helpers/SurveyVueUtils";
import { Metric } from "../../../metrics/metric";
import WarningBanner from "../WarningBanner";
import { SortAverages } from "../AverageHelper";
import { PageHandler } from "../../PageHandler";
import BaseOptionsSelector from "../Reports/Components/BaseOptionsSelector";
import { getDefaultBaseStateForMetric, useCrosstabPageStateContext } from "./CrosstabPageStateContext";
import { EntitySet } from "../../../entity/EntitySet";
import { FilterInstance } from "../../../entity/FilterInstance";
import { ProductConfiguration } from "../../../ProductConfiguration";
import AverageTypeSelector from "../Reports/Configuration/Options/AverageTypeSelector";
import { MixPanel } from "../../mixpanel/MixPanel";
import { VueEventName } from "../../../components/mixpanel/MixPanelHelper";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import { useAppDispatch } from "../../../state/store";
import { metricSupportsWeighting } from "../../../metrics/metricHelper";
import HeatMapConfiguration from "../Reports/Configuration/Options/HeatMapConfiguration";
import SubsetSelector from "./SubsetSelector";
import SigDiffSelector from "../Reports/Components/SigDiffSelector";
import { getNewShownSignificanceDifferences } from "../Reports/Components/SigDiffHelper";
import { EntityInstance } from "../../../entity/EntityInstance";
import { setFilterBy } from "../../../state/entitySelectionSlice";
import Tooltip from "../../../components/Tooltip";
import FilterInstancePicker from "../Reports/Configuration/Options/FilterInstancePicker";
import LowSampleSelector from "../Reports/Components/LowSampleSelector";

interface ICrosstabOptionsPaneProps {
    metric: Metric | undefined;
    googleTagManager: IGoogleTagManager;
    productConfiguration: ProductConfiguration;
    pageHandler: PageHandler;
    weightingStatus: BrandVueApi.WeightingStatus;
    isSurveyVueDataWeightable: boolean;
    canIncludeCounts: boolean;
    canCreateNewBase: boolean;
    activeEntitySet: EntitySet | undefined;
    filterInstances: FilterInstance[] | undefined;
    selectedPart?: string;
    subsetId: string;
    hasBreaksApplied: boolean;
}

const CrosstabOptionsPane = (props: ICrosstabOptionsPaneProps) => {
    const [isDecimalDropdownOpen, setIsDecimalDropdownOpen] = useState<boolean>(false);
    const [isOrderDropdownOpen, setIsOrderDropdownOpen] = useState<boolean>(false);
    const [selectedSubset, setSelectedSubset] = useState(props.subsetId);
    const { crosstabPageState, crosstabPageDispatch } = useCrosstabPageStateContext();
    const { selectableMetricsForUser: metrics, questionTypeLookup } = useMetricStateContext();
    const dispatch = useAppDispatch();
    const isSurveyVue = props.productConfiguration.isSurveyVue();

    React.useEffect(() => {
        if (selectedSubset !== props.subsetId) {
            window.location.reload();
        }
    }, [selectedSubset, props.subsetId]);

    const baseState = crosstabPageState.metricBaseLookup[props.metric?.name ?? ''] ?? getDefaultBaseStateForMetric(props.metric);

    const canPickFilterInstance = props.metric != null && props.activeEntitySet != null &&
        props.metric.calcType == CalculationType.Text &&
        props.metric.entityCombination.length == 1;

    const handleIncludeCountsChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.checked) {
            props.googleTagManager.addEvent("showCrosstabCounts", props.pageHandler);
            MixPanel.track("enabledIncludeCounts");
        }
        else {
            props.googleTagManager.addEvent("hideCrosstabCounts", props.pageHandler);
            MixPanel.track("disabledIncludeCounts");
        }
        crosstabPageDispatch({ type: 'SET_INCLUDE_COUNTS', data: { includeCounts: e.target.checked } });
    }

    const handleCalculateIndexScoresChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.checked) {
            MixPanel.track("enabledCalculateIndexScores");
        }
        else {
            MixPanel.track("disabledCalculateIndexScores");
        }
        crosstabPageDispatch({ type: 'SET_CALCULATE_INDEX_SCORES', data: { calculateIndexScores: e.target.checked } });
    }

    const handleHighlightLowSampleChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.googleTagManager.addEvent(e.target.checked ? "showCrosstabLowSample" : "hideCrosstabLowSample", props.pageHandler);
        MixPanel.track(e.target.checked ? "enabledHighlightLowSample" : "disabledHighlightLowSample");
        crosstabPageDispatch({ type: 'SET_HIGHLIGHT_LOW_SAMPLE', data: { highlightLowSample: e.target.checked } });
    }

    const handleHighlightSignificanceChanged = (e: boolean) => {
        if (e) {
            MixPanel.track("enabledHighlightSignificantVsTotal");
        }
        else {
            MixPanel.track("disabledHighlightSignificant");
        }

        crosstabPageDispatch({ type: 'SET_HIGHLIGHT_SIGNFICANCE', data: { highlightSignificance: e } });
    }

    const handleLowSampleThresholdChange = (e: React.ChangeEvent<HTMLInputElement>) => {
       const parsed = Number(e.target.value);
       // Only allow positive integers as threshold
       const threshold = Number.isNaN(parsed) || parsed <= 0 || !Number.isInteger(parsed) ? 0 : parsed;
       crosstabPageDispatch({ type: 'SET_LOW_SAMPLE_THRESHOLD', data: { lowSampleThreshold: threshold } });
    }
    
    const handleDisplayMeanValuesChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.checked) {
            MixPanel.track("enabledDisplayMeanValues");
        }
        else {
            MixPanel.track("disabledDisplayMeanValues");
            // Disable standard deviation when mean values are disabled
            crosstabPageDispatch({ type: 'SET_DISPLAY_STANDARD_DEVIATION', data: { displayStandardDeviation: false } });
        }
        crosstabPageDispatch({ type: 'SET_DISPLAY_MEAN_VALUES', data: { displayMeanValues: e.target.checked } });
    }

    const handleDisplayStandardDeviationChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.checked) {
            MixPanel.track("enabledDisplayStandardDeviation");
        }
        else {
            MixPanel.track("disabledDisplayStandardDeviation");
        }
        crosstabPageDispatch({ type: 'SET_DISPLAY_STANDARD_DEVIATION', data: { displayStandardDeviation: e.target.checked } });
    }

    const handleSelectFilterInstance = (instance: EntityInstance): void => {
        dispatch(setFilterBy({
            filterBy: instance,
            filterByType: props.activeEntitySet!.type
        }));
    }

    const setResultSortingOrder = (order: ReportOrder) => {
        MixPanel.track(getResultSortingOrderMixPanelEventName(order));
        crosstabPageDispatch({ type: 'SET_RESULT_SORTING_ORDER', data: { resultSortingOrder: order } });
    }

    const setDecimalPlaces = (decimalPlaces: number) => {
        crosstabPageDispatch({ type: 'SET_DECIMAL_PLACES', data: { decimalPlaces: decimalPlaces } });
    }

    const setSignificanceType = (signficanceType: CrosstabSignificanceType) => {
        if (signficanceType === CrosstabSignificanceType.CompareWithinBreak) {
            MixPanel.track("enabledHighlightSignificantWithinEachGroup");
        }
        if (signficanceType === CrosstabSignificanceType.CompareToTotal) {
            MixPanel.track("enabledHighlightSignificantVsTotal");
        }
        crosstabPageDispatch({ type: 'SET_SIGNIFICANCE_TYPE', data: { significanceType: signficanceType } });
    }

    const setSignificanceLevel = (sigConfidenceLevel: SigConfidenceLevel) => {
        MixPanel.track("sigConfidenceLevel");
        
        crosstabPageDispatch({ type: 'SET_SIGNIFICANCE_LEVEL', data: { sigConfidenceLevel: sigConfidenceLevel } });
    }

    const updateDisplaySigDiff = (toggledDisplaySignificanceDifferences: DisplaySignificanceDifferences) => {
        const newDisplayedSigDiff = getNewShownSignificanceDifferences(
            toggledDisplaySignificanceDifferences,
            crosstabPageState.displaySignificanceDifferences
        );

        MixPanel.track("toggleDisplaySignificantDifferences");
        
        crosstabPageDispatch({ type: 'SET_DISPLAYED_SIG_DIFF', 
            data: { displaySignificanceDifferences: newDisplayedSigDiff } });
    }

    const updateHideTotalColumn = (hideTotalColumn: boolean) => {
        MixPanel.track(hideTotalColumn ? "enabledHideTotalColumn" : "disabledHideTotalColumn");
        crosstabPageDispatch({ type: 'SET_HIDE_TOTAL_COLUMN', data: { hideTotalColumn: hideTotalColumn } });
    };

    const updateShowMultipleTablesAsSingle = (showMultipleTablesAsSingle: boolean) => {
        MixPanel.track(showMultipleTablesAsSingle ? 'enabledShowMultipleTablesAsSingle' : 'disabledShowMultipleTablesAsSingle');
        crosstabPageDispatch({ type: 'SET_SHOW_MULTIPLE_TABLES_AS_SINGLE', data: { showMultipleTablesAsSingle: showMultipleTablesAsSingle }});
    }

    const setBaseProperties = (baseType: BaseDefinitionType | undefined, baseVariableId: number | undefined) => {
        if (props.metric) {
            if (baseType !== baseState.baseType || baseVariableId !== baseState.baseVariableId) {
                MixPanel.track("changedBaseTypeOverride");
                props.googleTagManager.addEvent("changeCrosstabBaseType", props.pageHandler);
            }
            crosstabPageDispatch({
                type: 'SET_METRIC_BASE', data: {
                    metricName: props.metric.name,
                    baseTypeOverride: baseType ?? getDefaultBaseStateForMetric(props.metric).baseType,
                    baseVariableId: baseVariableId,
                }
            });
        }
    }

    const setHeatMapOptions = (options: BrandVueApi.HeatMapOptions) => {
        crosstabPageDispatch({ type: 'SET_HEATMAP_OPTIONS', data: { heatMapOptions: options }});
    }

    const selectDefaultBase = () => setBaseProperties(BaseDefinitionType.SawThisQuestion, props.metric?.baseVariableConfigurationId);

    const toggleAverage = (averageType: AverageType) => {
        const newAverages = [...crosstabPageState.selectedAverages];
        const index = newAverages.indexOf(averageType);
        if (index < 0) {
            props.googleTagManager.addEvent(getAnalyticsAverageAddedEvent(averageType, ReportType.Table), props.pageHandler);
            MixPanel.track(getMixPanelAddAverageEvent(averageType));
            newAverages.push(averageType);
        } else {
            props.googleTagManager.addEvent(getAnalyticsAverageRemovedEvent(averageType, ReportType.Table), props.pageHandler);
            MixPanel.track(getMixPanelRemoveAverageEvent(averageType));
            newAverages.splice(index, 1);
        }
        newAverages.sort((a, b) => SortAverages(a, b));

        crosstabPageDispatch({ type: 'SET_SELECTED_AVERAGES', data: { selectedAverages: newAverages } });
    }

    const handleIsWeightedChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.googleTagManager.addEvent(e.target.checked ? "weightingEnabled" : "weightingDisabled", props.pageHandler);
        crosstabPageDispatch({ type: 'SET_WEIGHTING', data: { weightingEnabled: e.target.checked } });
    }

    function getResultSortingOrderMixPanelEventName(order: ReportOrder): VueEventName {
        switch (order) {
            case ReportOrder.ResultOrderAsc: return "crossTabInResultOrderAscending";
            case ReportOrder.ResultOrderDesc: return "crossTabInResultOrderDescending";
            case ReportOrder.ScriptOrderDesc: return "crossTabInScriptOrderDescending";
            case ReportOrder.ScriptOrderAsc: return "crossTabInScriptOrderAscending";
            default: return "crossTabInResultOrderDescending";
        }
    }

    const disabledMessage = hasSingleEntityInstance(props.metric, props.activeEntitySet?.getInstances().getAll().map(i => i.id))
        ? 'Averages are disabled if there is only a single series'
        : undefined
    const isSystemAdministrator = props.productConfiguration.user.isSystemAdministrator;
    const metricDoesNotSupportWeighting = !metricSupportsWeighting(props.metric, questionTypeLookup);
    const isHeatMap = (props.metric && questionTypeLookup[props.metric.name] == MainQuestionType.HeatmapImage) ?? false;

    const onSubsetChange = (subsetId: string) => {
        setSelectedSubset(subsetId);
    }

    const getHideTotalColumnOption = () => {
        const isDisabled = !props.hasBreaksApplied;
        const option = (
            <div className="option">
                <input type="checkbox"
                    className="checkbox"
                    id="hide-total-checkbox"
                    checked={crosstabPageState.hideTotalColumn}
                    onChange={e => updateHideTotalColumn(e.target.checked)}
                    disabled={isDisabled} />
                <label htmlFor="hide-total-checkbox">Hide 'Total' column</label>
            </div>
        );
        if (isDisabled) {
            return (
                <Tooltip placement="top" title="Only available when breaks are applied">{option}</Tooltip>
            );
        }
        return option;
    };

    return (
        <div className="crosstab-options">
            {props.activeEntitySet && canPickFilterInstance && props.filterInstances &&
                <div>
                    <div className="option-section-label">Show results for</div>
                    <div className="option">
                        <FilterInstancePicker
                            entityType={props.activeEntitySet.type}
                            selectedInstances={props.filterInstances?.map(f => f.instance)}
                            allInstances={props.activeEntitySet.getInstances().getAll()}
                            updateState={(instance: EntityInstance) => handleSelectFilterInstance(instance)} />
                    </div>
                </div>
            }
            {isHeatMap && <HeatMapConfiguration
                saveOptionChanges={setHeatMapOptions}
                options={crosstabPageState.heatMapOptions}
            />
            }
            {(props.productConfiguration.isSurveyVue() || isSystemAdministrator) &&
                <BaseOptionsSelector
                    metric={props.metric}
                    baseType={baseState.baseType}
                    baseVariableId={baseState.baseVariableId}
                    selectDefaultBase={selectDefaultBase}
                    setBaseProperties={setBaseProperties}
                    canCreateNewBase={props.canCreateNewBase}
                    selectedPart={props.selectedPart}
                    updateLocalMetricBase={(variableId: number) => setBaseProperties(undefined, variableId)}
                    productConfiguration={props.productConfiguration}
                    showPadlock={!props.productConfiguration.isSurveyVue()}
                />
            }
            <SubsetSelector
                subsetId={selectedSubset}
                updateUrlOnChange={true}
                onSubsetChange={onSubsetChange}
            />
            <div>
                <div className="option-section-label">Values</div>
                <div className="option">
                    <input type="checkbox" disabled={!props.canIncludeCounts} className="checkbox" id="counts-checkbox" checked={crosstabPageState.includeCounts && props.canIncludeCounts} onChange={handleIncludeCountsChanged} />
                    <label htmlFor="counts-checkbox">
                        Include counts
                    </label>
                    {!props.canIncludeCounts &&
                        <WarningBanner message={"Counts cannot be shown for questions that are aggregates of other questions"}
                            materialIconName={""}
                        />
                    }
                </div>
                <div className="option">
                    <input type="checkbox" className="checkbox" id="index-score-checkbox" checked={crosstabPageState.calculateIndexScores} onChange={handleCalculateIndexScoresChanged} />
                    <label htmlFor="index-score-checkbox">
                        Calculate index scores
                    </label>
                </div>
                {props.weightingStatus != BrandVueApi.WeightingStatus.NoWeightingConfigured &&
                    <div className="option">
                        <input type="checkbox" disabled={props.weightingStatus == BrandVueApi.WeightingStatus.WeightingConfiguredInvalid || metricDoesNotSupportWeighting}
                            className="checkbox" id="weighting-checkbox"
                            checked={props.isSurveyVueDataWeightable && crosstabPageState.weightingEnabled} onChange={handleIsWeightedChanged} />
                        <label htmlFor="weighting-checkbox">
                            Weight data
                        </label>
                        {props.weightingStatus == BrandVueApi.WeightingStatus.WeightingConfiguredInvalid &&
                            <WarningBanner
                                message={"Weighting has validation errors - please check weighting"}
                                materialIconName=""
                            />
                        }
                        {metricDoesNotSupportWeighting &&
                            < WarningBanner
                                message={"This question does not support weighting"}
                                materialIconName=""
                            />
                        }
                    </div>
                }
                {getHideTotalColumnOption()}
                <div className="option">
                    <input type="checkbox"
                        className="checkbox"
                        id="show-multiple-tables-as-single-checkbox"
                        checked={crosstabPageState.showMultipleTablesAsSingle}
                        disabled={!props.metric || props.metric.entityCombination.length < 2 || !props.hasBreaksApplied}
                        onChange={e => updateShowMultipleTablesAsSingle(e.target.checked)} />
                    <label htmlFor="show-multiple-tables-as-single-checkbox">Show as single table</label>
                </div>
                <div className="rounding">
                    <label>Round to</label>
                    <ButtonDropdown isOpen={isDecimalDropdownOpen} toggle={() => setIsDecimalDropdownOpen(!isDecimalDropdownOpen)} className="rounding-dropdown">
                        <DropdownToggle className="toggle-button">
                            <span>{crosstabPageState.decimalPlaces}</span>
                            <i className="material-symbols-outlined">arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu>
                            <DropdownItem onClick={() => setDecimalPlaces(0)}>0</DropdownItem>
                            <DropdownItem onClick={() => setDecimalPlaces(1)}>1</DropdownItem>
                            <DropdownItem onClick={() => setDecimalPlaces(2)}>2</DropdownItem>
                        </DropdownMenu>
                    </ButtonDropdown>
                    <label>decimal places</label>
                </div>
            </div>
            <div>
                <div className="option-section-label">Sort order</div>
                <ButtonDropdown isOpen={isOrderDropdownOpen} toggle={() => setIsOrderDropdownOpen(!isOrderDropdownOpen)} className="configure-option-dropdown" >
                    <DropdownToggle className="toggle-button">
                        <span>{descriptionOfOrder(crosstabPageState.resultSortingOrder)}</span>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        <DropdownItem onClick={() => setResultSortingOrder(ReportOrder.ScriptOrderDesc)}>{descriptionOfOrder(BrandVueApi.ReportOrder.ScriptOrderDesc)}</DropdownItem>
                        <DropdownItem onClick={() => setResultSortingOrder(ReportOrder.ScriptOrderAsc)}>{descriptionOfOrder(BrandVueApi.ReportOrder.ScriptOrderAsc)}</DropdownItem>
                        <DropdownItem onClick={() => setResultSortingOrder(ReportOrder.ResultOrderDesc)}>{descriptionOfOrder(BrandVueApi.ReportOrder.ResultOrderDesc)}</DropdownItem>
                        <DropdownItem onClick={() => setResultSortingOrder(ReportOrder.ResultOrderAsc)}>{descriptionOfOrder(BrandVueApi.ReportOrder.ResultOrderAsc)}</DropdownItem>
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
            <div>
                <div className="option-section-label">Highlighting</div>
                <LowSampleSelector
                    highlightLowSample={crosstabPageState.highlightLowSample}
                    handleHighlightLowSampleChanged={handleHighlightLowSampleChanged}
                    lowSampleThreshold={crosstabPageState.lowSampleThreshold}
                    handleLowSampleThresholdChange={handleLowSampleThresholdChange}
                    allowLowSampleThresholdEditing={isSurveyVue}
                />
                <SigDiffSelector
                    highlightSignificance={crosstabPageState.highlightSignificance}
                    updateHighlightSignificance={handleHighlightSignificanceChanged}
                    displaySignificanceDifferences={crosstabPageState.displaySignificanceDifferences}
                    updateDisplaySignificanceDifferences={updateDisplaySigDiff}
                    significanceType={crosstabPageState.significanceType}
                    setSignificanceType={setSignificanceType}
                    disableSignificanceTypeSelector={!crosstabPageState.highlightSignificance}
                    downIsGood={props.metric?.downIsGood ?? false}
                    significanceLevel={crosstabPageState.sigConfidenceLevel}
                    setSignificanceLevel={setSignificanceLevel}
                    isAllVue={props.productConfiguration.isSurveyVue()}
                />
            </div>
            <div>
                <AverageTypeSelector
                    selectedAverages={crosstabPageState.selectedAverages}
                    toggleAverage={toggleAverage}
                    disabledMessage={disabledMessage}
                    metric={props.metric}
                    displayMeanValues={crosstabPageState.displayMeanValues}
                    displayStandardDeviation={crosstabPageState.displayStandardDeviation}
                    toggleDisplayMeanValues={handleDisplayMeanValuesChanged}
                    toggleStandardDeviation={handleDisplayStandardDeviationChanged}
                    metrics={metrics}
                    supportsStandardDeviation={true}
                />
            </div>
        </div>
    );
}

export default CrosstabOptionsPane;