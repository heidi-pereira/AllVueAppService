import { ButtonDropdown, DropdownToggle, DropdownMenu, DropdownItem } from 'reactstrap';
import * as BrandVueApi from "../../../../BrandVueApi";
import React from 'react';
import { ReportType, ReportOrder, CrosstabSignificanceType, BaseDefinitionType, DisplaySignificanceDifferences, SigConfidenceLevel } from '../../../../BrandVueApi';
import { descriptionOfOrder } from '../../../helpers/SurveyVueUtils';
import BaseOptionsSelector from "../Components/BaseOptionsSelector";
import WarningBanner from '../../WarningBanner';
import SigDiffSelector from '../Components/SigDiffSelector';
import Tooltip from '../../../../components/Tooltip';
import LowSampleSelector from '../Components/LowSampleSelector';

interface IReportSettingsModalOptionsProps {
    reportType: ReportType;
    includeCounts: boolean;
    isWeighted: boolean;
    hideEmptyRows: boolean;
    hideEmptyColumns: boolean;
    hideTotalColumn: boolean;
    hideDataLabels: boolean;
    showMultipleTablesAsSingle: boolean;
    weightingStatus: BrandVueApi.WeightingStatus;
    highlightLowSample: boolean;
    highlightSignificance: boolean;
    hiddenSignificanceDifferences: DisplaySignificanceDifferences;
    significanceType: CrosstabSignificanceType;
    order: ReportOrder;
    decimalPlaces: number;
    singlePageExport: boolean;
    baseTypeOverride: BaseDefinitionType;
    baseVariableId: number | undefined;
    lowSampleThreshold: number;
    setOrder(order: ReportOrder): void;
    setDecimalPlaces(places: number): void;
    setIncludeCounts(includeCounts: boolean): void;
    setIsDataWeighted(isWeighted: boolean): void;
    setHideEmptyRows(hideEmpty: boolean): void;
    setHideEmptyColumns(hideEmpty: boolean): void;
    setLowSampleThreshold(threshold: number): void;
    setHideTotalColumn(hideTotal: boolean): void;
    setHideDataLabels(hideDataLabels: boolean): void;
    setShowMultipleTablesAsSingle(showAsSingleTable: boolean): void;
    setHighlightLowSample(highlightLowSample: boolean): void;
    setHighlightSignificance(highlightSignificance: boolean): void;
    setHiddenSignificanceDifferences(highlightSignificance: DisplaySignificanceDifferences): void;
    setSignificanceType(significanceType: CrosstabSignificanceType): void;
    setSinglePageExport(singlePageExport: boolean): void;
    setBaseTypeOverride(baseType: BaseDefinitionType): void;
    setBaseVariableId(baseVariableId: number | undefined): void;
    canCreateNewBase: boolean | undefined;
    significanceLevel: SigConfidenceLevel;
    setSignificanceLevel(significanceLevel: SigConfidenceLevel): void;
    hasBreaksApplied: boolean;
    selectedReportId: number;
    setCalculateIndexScores(show: boolean): void;
    calculateIndexScores: boolean;
}

const ReportSettingsModalOptions = (props: IReportSettingsModalOptionsProps) => {
    const [isDecimalDropdownOpen, setIsDecimalDropdownOpen] = React.useState(false);
    const [isOrderDropdownOpen, setIsOrderDropdownOpen] = React.useState<boolean>(false);

    const toggle = () => {
        setIsDecimalDropdownOpen(!isDecimalDropdownOpen)
    }

    const handleIncludeCountsChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setIncludeCounts(e.target.checked);
    }

    const handleCalculateIndexScoresChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setCalculateIndexScores(e.target.checked);
    }

    const handleHighlightLowSampleChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setHighlightLowSample(e.target.checked);
    }

    const handleIsWeightedChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setIsDataWeighted(e.target.checked);
    }

    const handleHideEmptyRowsChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setHideEmptyRows(e.target.checked);
    }

    const handleHideEmptyColumnsChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setHideEmptyColumns(e.target.checked);
    }

    const handleLowSampleThresholdChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setLowSampleThreshold(parseInt(e.target.value));
    }

    const handleHideTotalColumnChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        props.setHideTotalColumn(e.target.checked);
    };

    const toggleOrder = () => {
        setIsOrderDropdownOpen(!isOrderDropdownOpen);
    }

    const setBaseProperties = (baseType: BaseDefinitionType | undefined, baseVariableId: number | undefined) => {
        if (baseVariableId) {
            props.setBaseTypeOverride(BaseDefinitionType.SawThisQuestion);
            props.setBaseVariableId(baseVariableId);
        } else {
            props.setBaseTypeOverride(baseType ?? BaseDefinitionType.SawThisChoice);
            props.setBaseVariableId(undefined);
        }
    }

    const getHideTotalColumnOption = () => {
        const isDisabled = !props.hasBreaksApplied;
        const option = (
            <div className="option">
                <input type="checkbox"
                    className="checkbox"
                    id="hide-total-column-checkbox"
                    checked={props.hideTotalColumn}
                    onChange={handleHideTotalColumnChanged}
                    disabled={isDisabled}
                />
                <label htmlFor="hide-total-column-checkbox">Hide 'Total' column</label>
            </div>
        );
        if (isDisabled) {
            return (<Tooltip placement="top-start" title="Only available when breaks are applied">{option}</Tooltip>)
        }
        return option;
    };

    return (
        <div className="report-options-container">
            <BaseOptionsSelector
                metric={undefined}
                baseType={props.baseTypeOverride}
                baseVariableId={props.baseVariableId}
                selectDefaultBase={() => {}}
                setBaseProperties={setBaseProperties}
                canCreateNewBase={props.canCreateNewBase}
                updateLocalMetricBase={() => {}}
            />
            <div>
                <label className="report-label">Values</label>
                {props.weightingStatus != BrandVueApi.WeightingStatus.NoWeightingConfigured &&
                    <div className="option">
                        <input type="checkbox" disabled={props.weightingStatus == BrandVueApi.WeightingStatus.WeightingConfiguredInvalid} className="checkbox" id="weighting-checkbox" checked={props.isWeighted} onChange={handleIsWeightedChanged} />
                        <label htmlFor="weighting-checkbox">
                            Weight data
                        </label>
                        {props.weightingStatus == BrandVueApi.WeightingStatus.WeightingConfiguredInvalid &&
                            <WarningBanner
                                message={"Weighting has validation errors - please check weighting"}
                                materialIconName=""
                            />
                        }
                    </div>
                }
                {props.reportType == ReportType.Table &&
                <>
                    <div className="option">
                        <input type="checkbox" className="checkbox" id="counts-checkbox" checked={props.includeCounts} onChange={handleIncludeCountsChanged} />
                        <label htmlFor="counts-checkbox">
                            Include counts
                        </label>
                    </div>
                    <div className="option">
                        <input type="checkbox" className="checkbox" id="highlight-index-scores-checkbox" checked={props.calculateIndexScores} onChange={handleCalculateIndexScoresChanged} />
                        <label htmlFor="highlight-index-scores-checkbox">
                            Calculate index scores
                        </label>
                    </div>
                    <div className="option">
                        <input type="checkbox" className="checkbox" id="hide-zeros-checkbox" checked={props.hideEmptyRows} onChange={handleHideEmptyRowsChanged} />
                        <label htmlFor="hide-zeros-checkbox">
                            Hide blank rows
                        </label>
                    </div>
                    <div className="option">
                        <input type="checkbox" className="checkbox" id="hide-columns-checkbox" checked={props.hideEmptyColumns} onChange={handleHideEmptyColumnsChanged} />
                        <label htmlFor="hide-columns-checkbox">
                            Hide blank columns
                        </label>
                    </div>
                    {getHideTotalColumnOption()}
                    <div className="option">
                        <input type="checkbox"
                            className="checkbox"
                            id="show-as-single-table-checkbox"
                            checked={props.showMultipleTablesAsSingle}
                            onChange={e => props.setShowMultipleTablesAsSingle(e.target.checked)} />
                        <label htmlFor="show-as-single-table-checkbox">
                            Show as single table
                        </label>
                    </div>
                </>
                }
                <div className="report-decimals">
                    <label>Round to</label>
                    <ButtonDropdown isOpen={isDecimalDropdownOpen} toggle={toggle} className="rounding-dropdown">
                        <DropdownToggle className="toggle-button">
                            <span>{props.decimalPlaces}</span>
                            <i className="material-symbols-outlined">arrow_drop_down</i>
                        </DropdownToggle>
                        <DropdownMenu>
                            <DropdownItem onClick={() => props.setDecimalPlaces(0)}>0</DropdownItem>
                            <DropdownItem onClick={() => props.setDecimalPlaces(1)}>1</DropdownItem>
                            <DropdownItem onClick={() => props.setDecimalPlaces(2)}>2</DropdownItem>
                        </DropdownMenu>
                    </ButtonDropdown>
                    <label>decimal places</label>
                </div>
            </div>
            {props.reportType === ReportType.Chart &&
                <div>
                    <label className="report-label">Export options</label>
                    <div className="option">
                        <input type="checkbox"
                            className="checkbox"
                            id="hide-data-labels-checkbox"
                            checked={props.hideDataLabels}
                            onChange={e => props.setHideDataLabels(e.target.checked)} />
                        <label htmlFor="hide-data-labels-checkbox">
                            Hide data labels
                        </label>
                    </div>
                </div>
            }
            <div>
                <label className="report-label">Sort order</label>
                <ButtonDropdown isOpen={isOrderDropdownOpen} toggle={toggleOrder} className="configure-option-dropdown" >
                    <DropdownToggle className="toggle-button">
                        <span>{descriptionOfOrder(props.order)}</span>
                        <i className="material-symbols-outlined">arrow_drop_down</i>
                    </DropdownToggle>
                    <DropdownMenu>
                        <DropdownItem onClick={() => props.setOrder(BrandVueApi.ReportOrder.ScriptOrderDesc)}>{descriptionOfOrder(BrandVueApi.ReportOrder.ScriptOrderDesc)}</DropdownItem>
                        <DropdownItem onClick={() => props.setOrder(BrandVueApi.ReportOrder.ScriptOrderAsc)}>{descriptionOfOrder(BrandVueApi.ReportOrder.ScriptOrderAsc)}</DropdownItem>
                        <DropdownItem onClick={() => props.setOrder(BrandVueApi.ReportOrder.ResultOrderDesc)}>{descriptionOfOrder(BrandVueApi.ReportOrder.ResultOrderDesc)}</DropdownItem>
                        <DropdownItem onClick={() => props.setOrder(BrandVueApi.ReportOrder.ResultOrderAsc)}>{descriptionOfOrder(BrandVueApi.ReportOrder.ResultOrderAsc)}</DropdownItem>
                    </DropdownMenu>
                </ButtonDropdown>
            </div>
            <div>
                <label className="report-label">Highlighting</label>
                <LowSampleSelector
                    highlightLowSample={props.highlightLowSample}
                    handleHighlightLowSampleChanged={handleHighlightLowSampleChanged}
                    lowSampleThreshold={props.lowSampleThreshold}
                    handleLowSampleThresholdChange={handleLowSampleThresholdChange}
                    allowLowSampleThresholdEditing={true}
                />
                <SigDiffSelector
                    highlightSignificance={props.highlightSignificance}
                    displaySignificanceDifferences={props.hiddenSignificanceDifferences}
                    updateHighlightSignificance={props.setHighlightSignificance}
                    updateDisplaySignificanceDifferences={props.setHiddenSignificanceDifferences}
                    significanceType={props.significanceType}
                    setSignificanceType={props.setSignificanceType}
                    disableSignificanceTypeSelector={props.reportType == BrandVueApi.ReportType.Chart || !props.highlightSignificance}
                    downIsGood={false}
                    significanceLevel={props.significanceLevel}
                    setSignificanceLevel={props.setSignificanceLevel}
                    isAllVue={true}
                />
            </div>
            {props.reportType == BrandVueApi.ReportType.Table &&
                <div>
                    <label className="report-label">Export to Excel</label>
                    <div className="report-properties">
                        <div className="radio-button-container">
                            <input type="radio" className="radio-input" id="MultipleSheets" name="multipleSheets"
                                value={Number(false)} checked={!props.singlePageExport}
                                onChange={() => props.setSinglePageExport(false)} />
                            <label htmlFor="MultipleSheets" className="radio-label">Multiple sheets</label>
                        </div>
                        <div className="radio-button-container">
                            <input type="radio" className="radio-input" id="SingleSheet" name="singleSheet"
                                value={Number(true)} checked={props.singlePageExport}
                                onChange={() => props.setSinglePageExport(true)} />
                            <label htmlFor="SingleSheet" className="radio-label">Single sheet</label>
                        </div>
                    </div>
                </div>
            }
        </div>
    )
}

export default ReportSettingsModalOptions;