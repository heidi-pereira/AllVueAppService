import { FeatureCode, MainQuestionType, PartDescriptor, Report, ReportWaveConfiguration, ReportWavesOptions } from "../../../../BrandVueApi";
import { PartWithExtraData } from "../ReportsPageDisplay";
import ReportWavesPicker from "../Components/ReportWavesPicker";
import { PartType } from "../../../panes/PartType";
import { isFeatureEnabled } from "../../../helpers/FeaturesHelper";
import { selectCurrentReport } from "client/state/reportSelectors";
import { useAppSelector } from "client/state/store";

export interface IConfigureReportPartOverTimeTabProps {
    reportPart: PartWithExtraData;
    reportWaves: ReportWaveConfiguration | undefined;
    questionTypeLookup: { [key: string]: MainQuestionType };
    savePartChanges(newPart: PartDescriptor): void;
}

const ConfigureReportPartOverTimeTab = (props: IConfigureReportPartOverTimeTabProps) => {
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    const part = props.reportPart.part;
    const useReportWaves = !part.waves;
    const isUsingReportOverTimeSetting = part.showOvertimeData == undefined;
    const chartTypeIsCompatible = part.partType !== PartType.ReportsCardDoughnut;
    const reportBreaksInUse = report.breaks.length > 0 && !props.reportPart.part.overrideReportBreaks;
    const reportPartBreaksInUse = props.reportPart.part.breaks.length > 0 && props.reportPart.part.overrideReportBreaks;
    const breaksInUse = reportBreaksInUse || reportPartBreaksInUse;
    const wavesInUse = props.reportPart.part.waves?.waves !== undefined;
    const isFunnelPart = props.reportPart.part.partType === PartType.ReportsCardFunnel;

    const isOvertimeFeatureEnabled = isFeatureEnabled(FeatureCode.Overtime_data);
    const reportOvertimeInUse = isOvertimeFeatureEnabled && isUsingReportOverTimeSetting && report.overTimeConfig != undefined;
    const partOvertimeInUse = isOvertimeFeatureEnabled && part.showOvertimeData == true;
    const overtimeInUse = reportOvertimeInUse || partOvertimeInUse;

    const chartTypeDoesNotSupportWaveBreakCombination = breaksInUse && !(wavesInUse || overtimeInUse) && isFunnelPart;

    const updateUseReportWaves = (shouldUseReportSettings: boolean) => {
        const newPart = new PartDescriptor(props.reportPart.part);
        if (shouldUseReportSettings) {
            newPart.waves = undefined;
        } else {
            newPart.waves = new ReportWaveConfiguration({
                waves: undefined,
                wavesToShow: ReportWavesOptions.SelectedWaves,
                numberOfRecentWaves: 3
            });
            newPart.breaks = newPart.breaks.length > 1 ? [newPart.breaks[0]] : [];
        }
        props.savePartChanges(newPart);
    }

    const updateWaves = (waves: ReportWaveConfiguration | undefined) => {
        const newPart = new PartDescriptor(props.reportPart.part);
        if (!waves) {
            newPart.waves = new ReportWaveConfiguration({
                waves: undefined,
                wavesToShow: newPart.waves?.wavesToShow ?? ReportWavesOptions.SelectedWaves,
                numberOfRecentWaves: newPart.waves?.numberOfRecentWaves ?? 3
            });
        } else {
            newPart.waves = waves;
            newPart.breaks = newPart.breaks.length > 1 ? [newPart.breaks[0]] : [];
        }
        props.savePartChanges(newPart);
    }

    const updateUseReportOvertime = (shouldUseReportSettings: boolean) => {
        const newPart = new PartDescriptor(props.reportPart.part);
        if (shouldUseReportSettings) {
            newPart.showOvertimeData = undefined;
        } else {
            newPart.showOvertimeData = false;
        }
        props.savePartChanges(newPart);
    };

    const updateShowOvertimeData = (showOvertime: boolean) => {
        const newPart = new PartDescriptor(props.reportPart.part);
        newPart.showOvertimeData = showOvertime;
        props.savePartChanges(newPart);
    };

    const selectedWaves = overtimeInUse ? undefined :
        useReportWaves ? props.reportWaves :
        part.waves;
    const hasWavesSelected = selectedWaves?.waves != undefined;

    const functionalityName = isOvertimeFeatureEnabled ? "Over time data" : "Waves";
    const plural = isOvertimeFeatureEnabled ? "is" : "are";

    return (
        <div className="configure-breaks">
            {!chartTypeIsCompatible &&
                <div className="warning-box">
                    <i className="material-symbols-outlined">warning</i>{functionalityName} {plural} not compatible with this chart type
                </div>
            }
            {chartTypeDoesNotSupportWaveBreakCombination &&
                <div className="warning-box">
                    <i className="material-symbols-outlined">warning</i>{functionalityName} cannot be used in combination with breaks with this chart type
                </div>
            }
            {chartTypeIsCompatible && !chartTypeDoesNotSupportWaveBreakCombination && isOvertimeFeatureEnabled &&
                <div className="section bordered">
                    <label className="category-label">Time series</label>
                    <input type="checkbox"
                        className="checkbox"
                        id="use-report-overtime-checkbox"
                        checked={hasWavesSelected ? false : isUsingReportOverTimeSetting}
                        onChange={() => updateUseReportOvertime(!isUsingReportOverTimeSetting)}
                        disabled={hasWavesSelected} />
                    <label htmlFor="use-report-overtime-checkbox" className="use-report-settings-label">Use report settings</label>
                    <input type="checkbox"
                        className="checkbox"
                        id="show-overtime-data-checkbox"
                        checked={hasWavesSelected ? false : isUsingReportOverTimeSetting ? reportOvertimeInUse : partOvertimeInUse}
                        onChange={() => updateShowOvertimeData(!partOvertimeInUse)}
                        disabled={isUsingReportOverTimeSetting || hasWavesSelected} />
                    <label htmlFor="show-overtime-data-checkbox" className="show-overtime-data-label">Show time series data</label>
                    <div className="hintText">Pick date ranges and moving averages</div>
                </div>
            }
            <div className="section bordered">
                {chartTypeIsCompatible && !chartTypeDoesNotSupportWaveBreakCombination && <>
                    <label className="category-label">Waves</label>
                    <input type="checkbox"
                        className="checkbox"
                        id="use-report-waves-checkbox"
                        checked={overtimeInUse ? false : useReportWaves}
                        onChange={() => updateUseReportWaves(!useReportWaves)}
                        disabled={overtimeInUse} />
                    <label htmlFor="use-report-waves-checkbox" className="use-report-settings-label">Use report settings</label>
                </>}
                {!chartTypeDoesNotSupportWaveBreakCombination && <ReportWavesPicker
                    isDisabled={useReportWaves || !chartTypeIsCompatible || chartTypeDoesNotSupportWaveBreakCombination || overtimeInUse}
                    questionTypeLookup={props.questionTypeLookup}
                    waveConfig={selectedWaves}
                    updateWaves={updateWaves}
                    selectedPart={part.spec2}
                />}
            </div>
        </div>
    )
}

export default ConfigureReportPartOverTimeTab;