import { ApplicationConfiguration } from "../../../../ApplicationConfiguration";
import { IAverageDescriptor, CustomDateRange, ReportOverTimeConfiguration } from "../../../../BrandVueApi";
import AverageSelector from "../../../filters/AverageSelector";
import style from "./ReportOverTimeSettingsPicker.module.less";
import { customDateRangeToText, getUserVisibleAverages } from "client/components/helpers/SurveyVueUtils";
import { useAppSelector } from "client/state/store";
import { selectAllAverages } from "client/state/averageSlice";
import AllVueDateRangePicker from "client/components/visualisations/Reports/Components/AllVueDateRangePicker";

interface IReportOverTimeSettingsPickerProps {
    applicationConfiguration: ApplicationConfiguration;
    config: ReportOverTimeConfiguration | undefined;
    setConfig: (config: ReportOverTimeConfiguration | undefined) => void;
    isDataWeighted: boolean;
    unsavedSubsetId: string;
    disabled?: boolean;
}

function customDateRangesAreEqual(a: CustomDateRange, b: CustomDateRange): boolean {
    return a.numberOfPeriods === b.numberOfPeriods && a.periodType === b.periodType;
}

const ReportOverTimeSettingsPicker = (props: IReportOverTimeSettingsPickerProps) => {
    const allAverages = useAppSelector(selectAllAverages);

    const isOverTimeEnabled = props.config != undefined;
    const disableInputs = props.disabled || !isOverTimeEnabled;

    const userVisibleAverages = getUserVisibleAverages(props.applicationConfiguration,
        allAverages,
        props.isDataWeighted,
        props.unsavedSubsetId);

    let selectedAverage: IAverageDescriptor | undefined = undefined;

    if(userVisibleAverages && userVisibleAverages.length > 0) {
        selectedAverage = userVisibleAverages.find(a => a.averageId === props.config?.averageId)
            ?? userVisibleAverages[0];
    }

    const toggleEnableOverTime = () => {
        if (props.config) {
            props.setConfig(undefined);
        } else {
            props.setConfig(new ReportOverTimeConfiguration());
        }
    };

    const onDateRangeSelected = (range: string, start: Date, end: Date) => {
        props.setConfig(new ReportOverTimeConfiguration({
            range: range,
            customRange: undefined,
            savedRanges: props.config?.savedRanges ?? [],
            averageId: props.config?.averageId
        }));
    };

    const onCustomDateRangeSelected = (customRange: CustomDateRange, start: Date, end: Date) => {
        const savedRanges = props.config?.savedRanges.filter(r => !customDateRangesAreEqual(r, customRange)) ?? [];
        props.setConfig(new ReportOverTimeConfiguration({
            range: undefined,
            customRange: customRange,
            savedRanges: [customRange, ...savedRanges],
            averageId: props.config?.averageId
        }));
    }

    const onAverageChanged = (average: IAverageDescriptor) => {
        props.setConfig(new ReportOverTimeConfiguration({
            ...props.config,
            savedRanges: props.config?.savedRanges ?? [],
            averageId: average?.averageId ?? undefined
        }));
    };

    const onSavedRangeDeleted = (customRange: CustomDateRange) => {
        const savedRanges = props.config?.savedRanges.filter(r => !customDateRangesAreEqual(r, customRange)) ?? [];
        props.setConfig(new ReportOverTimeConfiguration({
            ...props.config,
            savedRanges: savedRanges,
        }));
    }

    const capitalize = (s: string | undefined) => s && s[0].toUpperCase() + s.slice(1);

    const getDateRangeTitle = (): string => {
        if (!disableInputs) {
            if (props.config?.customRange) {
                return customDateRangeToText(props.config.customRange);
            } else if (props.config?.range) {
                return capitalize(props.config.range)!;
            }
        }
        return "No default dates selected";
    }

    const getAverageTitleOverride = (): string | undefined => {
        if (disableInputs || !props.config?.averageId) {
            return "No default average selected";
        }
    }

    return (
        <div className={style.overtimeSettingsPicker}>
            <div>
                <input type="checkbox"
                    className="checkbox"
                    id="enable-overtime-data-checkbox"
                    checked={isOverTimeEnabled && !props.disabled}
                    onChange={toggleEnableOverTime}
                    disabled={props.disabled} />
                <label htmlFor="enable-overtime-data-checkbox">Show time series data</label>
                <div className="option-hint">Pick date ranges and moving averages</div>
            </div>
            <div>
                <AllVueDateRangePicker
                    applicationConfiguration={props.applicationConfiguration}
                    overtimeConfig={props.config}
                    dropdownTitle={getDateRangeTitle()}
                    onRangeSelected={onDateRangeSelected}
                    onCustomRangeSelected={onCustomDateRangeSelected}
                    onSavedRangeDeleted={onSavedRangeDeleted}
                    disabled={disableInputs}
                />
            </div>
            <div>
                <AverageSelector
                    average={selectedAverage}
                    userVisibleAverages={userVisibleAverages}
                    updateFilterAverage={onAverageChanged}
                    disabled={disableInputs}
                    titleOverride={getAverageTitleOverride()} />
            </div>
        </div>
    );
};

export default ReportOverTimeSettingsPicker;