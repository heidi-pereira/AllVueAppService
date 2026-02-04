import React, {useEffect} from "react";
import {ButtonDropdown, DropdownItem, DropdownMenu, DropdownToggle, Popover} from "reactstrap";
import * as BrandVueApi from "../../../../BrandVueApi";
import {
    ErrorMessageLevel,
    ExportRespondentWeightsRequest,
    Factory,
    Message,
    Subset,
    UiWeightingConfigurationRoot,
    UiWeightingPlanConfiguration
} from "../../../../BrandVueApi";
import {toast} from 'react-hot-toast';
import {saveFile} from "../../../../helpers/FileOperations";
import {MetricSet} from "../../../../metrics/metricSet";
import {AverageIds} from "../../../helpers/PeriodHelper";
import style from "./WeightingSettingsPage.module.less"

interface ISettingsCardProps {
    averages: BrandVueApi.IAverageDescriptor[]
    key: string;
    metrics: MetricSet;
    root: UiWeightingConfigurationRoot;
    subsetsWithoutWeighting: Subset[];
    allSubsets: Subset[];
    onCopyClick: (e: React.MouseEvent, weightingPlan: UiWeightingConfigurationRoot) => void;
    onDeleteClick: (e: React.MouseEvent, weightingPlan: UiWeightingConfigurationRoot) => void;
    navigateToWeightingPlan: (newWeightingPlan: UiWeightingConfigurationRoot) => void;
    isExportWeightsAvailable: boolean;
}

const WeightingSettingsCard = (props: ISettingsCardProps) => {

    const weightingPlansClient = Factory.WeightingPlansClient(error => error());
    const [dropdownOpen, setDropdownOpen] = React.useState<boolean>(false);
    const [isLoading, setIsLoading] = React.useState(true);
    const [planValidation, setPlanValidation] = React.useState<BrandVueApi.DetailedPlanValidation | null>(null);
    const [sampleSizeError, setSampleSizeError] = React.useState<string | null>(null);
    const [popoverOpen, setPopoverOpen] = React.useState(false);

    const toastError = (error: Error, userFriendlyText: string) => {
        toast.error(userFriendlyText);
        console.log(error);
        setIsLoading(false);
    }
    const toggle = (e: React.MouseEvent) => {
        e.stopPropagation();
        setDropdownOpen(!dropdownOpen);
    }
    const onExportResponseWeightings = (e: React.MouseEvent, subsetDisplayName: string, averageId: string) => {
        const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());
        e.stopPropagation();
        return weightingAlgorithmsClient.exportRespondentWeights(
                new ExportRespondentWeightsRequest(
                    { subsetIds:[props.root.subsetId], averageId: averageId }))
            .then(r => saveFile(r, `Weightings- ${subsetDisplayName}- (${averageId})- Private.csv`))
            .catch(error => {
                toast.error("Export failed");
            });
    }
    const onDownloadWeightingsIntegrityReport = (e: React.MouseEvent, subsetDisplayName: string) => {
        const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());
        e.stopPropagation();
        return weightingAlgorithmsClient.respondentWeightsReport(props.root.subsetId, props.root.uiWeightingPlans.map(x => x.variableIdentifier))
            .then(r => saveFile(r, `Integrity Weighting Report- ${subsetDisplayName}- Private.csv`))
            .catch(error => {
                toast.error("Report failed");
            });
    }

    const getPlans = () => {

        weightingPlansClient.isWeightingPlanDefinedAndValid(props.root.subsetId).then(validation => {
            setPlanValidation(validation);

            if (validation.isValid) {
                const weightingAlgorithmsClient = Factory.WeightingAlgorithmsClient(error => error());
                weightingAlgorithmsClient.getTotalSampleSizeWithFilters(props.root.subsetId, [])
                    .then(populationSize => {
                        setIsLoading(false);
                        if (populationSize <= 0.0) {
                            setSampleSizeError(`Survey segment ${props.root.subsetId} has no data`);
                        }
                    }).catch((e: Error) => {
                        setSampleSizeError(`Failed to get respondents for survey segment`);
                        setIsLoading(false)
                    });
            }
            else {
                setIsLoading(false);
            }

        }).catch((e: Error) => toastError(e, "An error occurred trying to get weightings"));
    }

    useEffect(() => {
        getPlans();
    }, [props.root]);

    const GetQuestionNameForIdentifier = (identifier: string): string => {
        const planMetric = props.metrics.metrics.find(x => x.name == identifier);
        const title = planMetric ? planMetric.varCode : identifier;
        return title;
    };
    const isPercentageWeighted = (plan: UiWeightingPlanConfiguration): Boolean => {
        let isPlanPercentageWeighted = false;
        if (plan.uiChildTargets.reduce((partialSum, a) => partialSum + (a.target == undefined ? 0 : a.target), 0) == 1.0) {
            isPlanPercentageWeighted = plan.uiChildTargets.find(x => x.uiChildPlans.length > 0) == undefined;
        }
        return isPlanPercentageWeighted;
    }

    const summaryOfPlan = (weightingConfiguration: UiWeightingConfigurationRoot) => {
        if ((weightingConfiguration.uiWeightingPlans.length == 1) && (!isPercentageWeighted(weightingConfiguration.uiWeightingPlans[0]))) {
            const wavePlan = weightingConfiguration.uiWeightingPlans[0];
            return (<>
                <span className={style.bold}>Wave based ({wavePlan.uiChildTargets.length}): </span>
                {GetQuestionNameForIdentifier(wavePlan.variableIdentifier)}
            </>);
        }
        return (<>
            <span className={style.bold}>Weighted on: </span>
            {weightingConfiguration.uiWeightingPlans.map(x => GetQuestionNameForIdentifier(x.variableIdentifier)).join(',')}
        </>);
    }

    const validationMessages = (subsetId: string, subsetDisabledOrDeleted: boolean, sampleSizeError: string | null) => {
        const validation = planValidation;
        if (validation) {
            const errors = validation.messages.filter(x => x.errorLevel == BrandVueApi.ErrorMessageLevel.Error);
            const warnings = validation.messages.filter(x => x.errorLevel == BrandVueApi.ErrorMessageLevel.Warning);

            if (subsetDisabledOrDeleted) {
                warnings.unshift(new Message({ path: "", messageText: "Survey segment disabled or deleted", errorLevel: ErrorMessageLevel.Warning }));
            }

            if (sampleSizeError != null && sampleSizeError.length > 0) {
                errors.unshift(new Message({ path: "", messageText: sampleSizeError, errorLevel: ErrorMessageLevel.Error }));
            }

            const errorMessage = errors.length == 0 ? "" : `${errors.length} ${errors.length == 1 ? 'Error' : 'Errors'} ${warnings.length > 0 ? ' and ' : ''}`;
            const warningMessage = warnings.length == 0 ? "" : `${warnings.length} ${warnings.length == 1 ? 'Warning' : 'Warnings'}`;

            const totalMessage = errorMessage + warningMessage;
            if (totalMessage.length) {
                const colorClass = errorMessage.length ? style.errorColor : style.warningColor

                const validationSummary = () => {

                    const messageSummary = (messages: Message[], errorMessageLevel: ErrorMessageLevel) => {
                        const maxMessagesToList = 5
                        const errorLevelTitleClass = errorMessageLevel == ErrorMessageLevel.Error ? style.validationPopoverContentTitleError : style.validationPopoverContentTitleWarning;
                        const errorLevelTitleSymbolClass = errorMessageLevel == ErrorMessageLevel.Error ? style.validationPopoverContentTitleSymbolError : style.validationPopoverContentTitleSymbolWarning;
                        const errorLevelText = ErrorMessageLevel[errorMessageLevel];
                        const hiddenMessageCount = messages.length > maxMessagesToList && messages.length - maxMessagesToList;

                        const messageFooter = (errorLevelText: string, hiddenMessageCount: number) => {
                            const footerText = `... ${hiddenMessageCount} more ${errorLevelText.toLowerCase()}${hiddenMessageCount !== 1 ? "s" : ""}`;

                            return (
                                <div className={style.validationPopoverContentItemsFooter}>{footerText}</div>
                            );
                        }

                        if (messages.length > 0) {
                            return (
                                <div>
                                    <div className={`${style.validationPopoverContentTitle} ${errorLevelTitleClass}`}>
                                        <i className={`${style.symbol} ${errorLevelTitleSymbolClass} material-symbols-outlined`}>{errorLevelText.toLowerCase()}</i><h4>{`${errorLevelText}s`}</h4>
                                    </div>
                                    <div className={style.validationPopoverContentItems}>
                                        {messages.slice(0, maxMessagesToList).map((m, i) => <div key={i} className={style.validationPopoverContentItemsItem}>{m.messageText}</div>)}
                                    </div>
                                    {hiddenMessageCount && messageFooter(errorLevelText, hiddenMessageCount)}
                                </div>
                            );
                        };
                    }

                    return (
                        <div className={style.validationPopoverContent}>
                            {messageSummary(errors, ErrorMessageLevel.Error)}
                            {messageSummary(warnings, ErrorMessageLevel.Warning)}
                        </div>
                    )
                };

                return (
                    <div>
                        <div
                            id={`pop-${subsetId}`}
                            className={`${style.banner} ${colorClass}`}
                            onMouseEnter={() => setPopoverOpen(true)}
                            onMouseLeave={() => setPopoverOpen(false)}
                        >
                            <i className={`${style.symbol} ${style.materialSymbolsOutlined} material-symbols-outlined`}>{errorMessage.length > 0 ? "error" : "warning"}</i>{totalMessage}
                        </div>
                        <Popover
                            popperClassName={style.validationPopover}
                            placement="bottom"
                            isOpen={popoverOpen}
                            hideArrow={false}
                            target={`pop-${subsetId}`}>
                            {validationSummary()}
                        </Popover>
                    </div>
                )
            }
        }
    }
    const ExportForAverages = (subsetDisplayName: string, averages: BrandVueApi.IAverageDescriptor[]) => {
        const customPeriodAverage = averages.find(x =>
            !x.disabled &&
            x.weightingMethod === BrandVueApi.WeightingMethod.QuotaCell &&
            x.averageId === AverageIds.CustomPeriod
        );
        const otherAverages = averages.filter(x =>
            !x.disabled &&
            x.weightingMethod === BrandVueApi.WeightingMethod.QuotaCell &&
            x.averageId !== AverageIds.CustomPeriod
        );
        const defaultAverage = customPeriodAverage ?? otherAverages.find(a => a.isDefault) ?? otherAverages[0];
        return (
            <ButtonDropdown isOpen={dropdownOpen} toggle={toggle} id="dropdown-button-drop-up" drop="start" key="up" className={`averageSelector ${style.exportButton}`} >
                <DropdownToggle caret className="btn-menu styled-dropnone" aria-label="Select a period">
                    <i className={`${style.symbol} material-symbols-outlined`}>download</i>
                </DropdownToggle>
                <div className="exportWeightsDropdown">
                    <DropdownMenu>
                        <DropdownItem header>Export weights for</DropdownItem>
                        {defaultAverage &&
                            <DropdownItem key={-1} onClick={(e) => onExportResponseWeightings(e, subsetDisplayName, defaultAverage.averageId)}>
                                <i className={`${style.symbolIcon} material-symbols-outlined menu-icon`}>Weight</i>{`Default (${subsetDisplayName})`}
                            </DropdownItem>
                        }
                        {otherAverages.map((v, i) =>
                            <DropdownItem key={i} onClick={(e) => onExportResponseWeightings(e, subsetDisplayName, v.averageId)}>
                                <i className={`${style.symbolIcon} material-symbols-outlined menu-icon`}>Weight</i>{v.displayName}
                            </DropdownItem>
                        )}
                        <DropdownItem header>Reports</DropdownItem>
                        <DropdownItem key={100} onClick={(e) => onDownloadWeightingsIntegrityReport(e, subsetDisplayName)}><i className={`${style.symbolIcon} material-symbols-outlined menu-icon`}>report</i>Weighting integrity</DropdownItem>
                    </DropdownMenu>
                </div>
            </ButtonDropdown>
        );
    }

    const weightingPlanRootItem = (weightingPlan: UiWeightingConfigurationRoot) => {
        const weightingPlanSubset = props.allSubsets.find(s => s.id === weightingPlan.subsetId);
        const subsetHasNoData = sampleSizeError != null;

        const subsetDisabledOrDeleted = !weightingPlanSubset || weightingPlanSubset.disabled;
        const subsetDisplayName = weightingPlanSubset ? weightingPlanSubset.displayName : weightingPlan.subsetId;
        const classNames: string[] = [style.card, "card"];
        if (subsetDisabledOrDeleted) {
            classNames.push(style.disabled);
        }
        if (subsetHasNoData) {
            classNames.push("no-data");
        }
        return (
            <div className={classNames.join(" ")}
                key={weightingPlan.subsetId} onClick={() => !subsetHasNoData && !subsetDisabledOrDeleted && props.navigateToWeightingPlan(weightingPlan)}>
                <div className={style.cardName}>Segment: {subsetDisplayName}</div>
                <div className={style.actions}>
                    {props.subsetsWithoutWeighting.length !== 0 && !subsetDisabledOrDeleted && <div onClick={(e) => props.onCopyClick(e, weightingPlan)} title="Copy to new segment"><i className={`${style.symbol} material-symbols-outlined`}>file_copy</i></div>}

                    {props.isExportWeightsAvailable &&
                        ExportForAverages(subsetDisplayName, props.averages)
                    }
                    <div className={style.deleteButton} onClick={(e) => props.onDeleteClick(e, weightingPlan)} title="Delete weighting"><i className={`${style.symbol} material-symbols-outlined`}>delete</i></div>
                </div>

                <p>{summaryOfPlan(weightingPlan)}</p>
                {props.isExportWeightsAvailable &&
                    <p className={style.additionalInfo}>Identifier: {weightingPlan.subsetId}</p>
                }

                {validationMessages(weightingPlan.subsetId.replaceAll(" ", "-"), subsetDisabledOrDeleted, sampleSizeError)}
            </div>
        );
    }

    if (isLoading) {
        const weightingPlanSubset = props.allSubsets.find(s => s.id === props.root.subsetId);
        const subsetDisabledOrDeleted = !weightingPlanSubset || weightingPlanSubset.disabled;
        const subsetDisplayName = weightingPlanSubset ? weightingPlanSubset.displayName : props.root.subsetId;
        return (
            <div className={`${style.card} card ${subsetDisabledOrDeleted && style.disabled}`} key={props.root.subsetId}>
                <div className={style.cardName}>Segment: {subsetDisplayName}</div>
                <p>Loading...</p>
                {props.isExportWeightsAvailable &&
                    <p className={style.additionalInfo}>Identifier: {props.root.subsetId}</p>
                }
            </div>
        );
    }
    return weightingPlanRootItem(props.root)
}

        export default WeightingSettingsCard;