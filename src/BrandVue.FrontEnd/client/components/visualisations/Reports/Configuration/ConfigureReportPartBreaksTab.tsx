import React from "react";
import { CrossMeasure, IApplicationUser, MainQuestionType, PartDescriptor, ReportType, ReportOrder, CalculationType, Report, MultipleEntitySplitByAndFilterBy } from "../../../../BrandVueApi";
import { IGoogleTagManager } from "../../../../googleTagManager";
import { PartWithExtraData } from "../ReportsPageDisplay";
import { PageHandler } from "../../../PageHandler";
import BreaksPicker from "../../BreakPicker/BreaksPicker";
import {useConfigureNets} from "./ConfigureNets";
import { useMetricStateContext } from "../../../../metrics/MetricStateContext";
import { PartType } from "../../../panes/PartType";
import { BreakPickerParent } from "../../BreakPicker/BreaksDropdownHelper";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";
import { selectCurrentReport } from "client/state/reportSelectors";

export interface IConfigureReportPartBreaksTabProps {
    reportType: ReportType;
    reportPart: PartWithExtraData;
    reportBreaks: CrossMeasure[];
    reportOrderBy: ReportOrder;
    questionTypeLookup: {[key: string]: MainQuestionType};
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    user: IApplicationUser | null;
    isUsingOverTime: boolean;
    savePartChanges(newPart: PartDescriptor): void;
}

const ConfigureReportPartBreaksTab = (props: IConfigureReportPartBreaksTabProps) => {
    const useReportBreaks = !props.reportPart.part.overrideReportBreaks;
    const { metricsForReports } = useMetricStateContext();
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;

    const isChartTypeCompatible = props.reportPart.part.partType != PartType.ReportsCardDoughnut;
    const wavesInUse = (report.waves !== undefined && props.reportPart.part.waves === undefined) || props.reportPart.part.waves?.waves !== undefined;
    const breaksInUse = props.reportPart.part.breaks?.length > 0;
    const isFunnelPart = props.reportPart.part.partType === PartType.ReportsCardFunnel;
    const chartTypeDoesNotSupportWaveBreakCombination = wavesInUse && !breaksInUse && isFunnelPart;
    const subsetId = useAppSelector(selectSubsetId);
    
    const netAPI = useConfigureNets(props.reportPart, metricsForReports, subsetId, props.googleTagManager, props.pageHandler)
    const availableEntityInstances = netAPI.availableEntityInstances;

    const updateBreaks = (breaks: CrossMeasure[]) => {
        const newPart = new PartDescriptor(props.reportPart.part);
        const wasMultiBreak = props.reportPart.part.breaks?.length > 1;
        newPart.breaks = breaks;
        if (!wasMultiBreak && breaks.length > 1) {
            const selectedInstances =
                props.reportPart.part.selectedEntityInstances?.selectedInstances
                    ? availableEntityInstances
                        .filter(entity => props.reportPart.part.selectedEntityInstances?.selectedInstances.some(i => entity.id === i))
                    : availableEntityInstances;
            if (props.reportType === ReportType.Chart ||
                (selectedInstances.length > 0 && props.reportType === ReportType.Table)) {
                newPart.multiBreakSelectedEntityInstance = selectedInstances[0].id;
            }
        } else if (wasMultiBreak && breaks.length <= 1){
            newPart.multiBreakSelectedEntityInstance = undefined;
        } else if(breaks.length === 0) {
            newPart.multiBreakSelectedEntityInstance = undefined;
            let multEntitySplitByAndFilterBy = new MultipleEntitySplitByAndFilterBy(newPart.multipleEntitySplitByAndFilterBy);

            multEntitySplitByAndFilterBy.filterByEntityTypes = multEntitySplitByAndFilterBy.filterByEntityTypes.length > 0
                ? [multEntitySplitByAndFilterBy.filterByEntityTypes[0]]
                : [];

            newPart.multipleEntitySplitByAndFilterBy = multEntitySplitByAndFilterBy;
        }

        props.savePartChanges(newPart);
    }

    const updateUseReportBreaks = (shouldUseReportBreaks: boolean) => {
        if (useReportBreaks != shouldUseReportBreaks) {
            const newPart = new PartDescriptor(props.reportPart.part);
            newPart.overrideReportBreaks = !shouldUseReportBreaks;
            props.savePartChanges(newPart);
        }
    }

    const doesMetricSupportBreaks = (): boolean => {
        return props.reportPart.metric == null ? false : props.reportPart.metric.calcType !== CalculationType.Text;
    }

    const getBreaksToEdit = () => {
        if (useReportBreaks) {
            return props.reportBreaks;
        }
        return props.reportPart.part.breaks ?? [];
    }

    const getWarning = () => {
        let warningMessage: string | undefined = undefined;

        if (!isChartTypeCompatible) {
            warningMessage = "Breaks are not compatible with this chart type";
        } else if (chartTypeDoesNotSupportWaveBreakCombination) {
            warningMessage = "Breaks cannot be used in combination with waves with this chart type";
        } else if (props.isUsingOverTime) {
            warningMessage = "Breaks cannot currently be used in combination with time series";
        }

        if (warningMessage) {
            return (
                <div className="warning-box"><i className="material-symbols-outlined">warning</i>{warningMessage}</div>
            );
        }
    }

    if (!doesMetricSupportBreaks()) {
        return (
            <div className="configure-breaks">Not available</div>
        );
    }

    return (
        <div className="configure-breaks">
            {getWarning()}
            <BreaksPicker
                selectedBreaks={getBreaksToEdit()}
                setSelectedBreaks={updateBreaks}
                googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler}
                groupCustomVariables={true}
                user={props.user}
                canSaveAndLoad={true}
                reportType={props.reportType}
                isReportLevelChecked={useReportBreaks}
                setIsReportLevelChecked={updateUseReportBreaks}
                supportMultiBreaks={!isFunnelPart}
                displayBreakInstanceSelector={true}
                isDisabled={!isChartTypeCompatible || chartTypeDoesNotSupportWaveBreakCombination || props.isUsingOverTime}
                parentComponent={BreakPickerParent.Report}
            />
        </div>
    )
}

export default ConfigureReportPartBreaksTab;