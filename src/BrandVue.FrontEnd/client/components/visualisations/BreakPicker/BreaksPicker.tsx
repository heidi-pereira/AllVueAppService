import React from "react";
import { useState } from "react";
import {
    CrossMeasure,
    IApplicationUser,
    MainQuestionType,
    PermissionFeaturesOptions,
    ReportType,
    SavedBreakCombination
} from "../../../BrandVueApi";
import NoBreaksMessage from "./NoBreaksMessage";
import { Metric } from "../../../metrics/metric";
import AddBreakDropdown from "./AddBreakDropdown";
import { IGoogleTagManager } from "../../../googleTagManager";
import { PageHandler } from "../../PageHandler";
import MultipleBreaksPicker from "./MultipleBreaksPicker";
import { getAvailableCrossMeasureFilterInstances } from "../../helpers/SurveyVueUtils";
import { useEntityConfigurationStateContext } from "../../../entity/EntityConfigurationStateContext";
import { doBreaksMatch } from "../Crosstab/CrossMeasureUtils";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import { MixPanel } from "../../mixpanel/MixPanel";
import { useSavedBreaksContext } from "../Crosstab/SavedBreaksContext";
import SaveEditBreaksModal from "./SaveEditBreaksModal";
import { BreakPickerParent } from "./BreaksDropdownHelper";
import { getActiveBreaksFromSelection, setBreaksAndPeriod } from "../../helpers/AudienceHelper";
import { useAppDispatch } from "../../../state/store";
import { useWriteVueQueryParams } from "../../helpers/UrlHelper";
import { useLocation, useNavigate } from "react-router-dom";
import FeatureGuard from "../../FeatureGuard/FeatureGuard";

export interface IBreaksPickerProps {
    reportType?: ReportType;
    isDisabled?: boolean;
    isPrimaryAction?: boolean;
    selectedBreaks: CrossMeasure[];
    setSelectedBreaks: (breaks: CrossMeasure[]) => void;
    user: IApplicationUser | null;
    canSaveAndLoad: boolean;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    groupCustomVariables: boolean;
    isReportSettings?: boolean;
    isReportLevelChecked?: boolean;
    setIsReportLevelChecked?: (isReportLevelChecked: boolean) => void;
    supportMultiBreaks: boolean;
    displayBreakInstanceSelector: boolean;
    parentComponent: BreakPickerParent;
}

export const BreaksPicker = (props: IBreaksPickerProps) => {
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const { metricsForBreaks, questionTypeLookup } = useMetricStateContext();
    const { savedBreaks } = useSavedBreaksContext();
    const [saveBreaksModalOpen, setSaveBreaksModalOpen] = useState(false);
    const [savedBreakThatMatchesSelectedBreak, setSavedBreakThatMatchesSelectedBreak] = useState<SavedBreakCombination | undefined>(undefined);
    const isDisabled = props.isDisabled || props.isReportLevelChecked!!;
    const { setQueryParameter } = useWriteVueQueryParams(useNavigate(), useLocation());
    const dispatch = useAppDispatch();

    React.useEffect(() => {
        setSavedBreakThatMatchesSelectedBreak(savedBreaks.find((b) => doBreaksMatch(b.breaks, props.selectedBreaks)));
    }, [props.selectedBreaks, savedBreaks]);

    const getUseReportLevelBreaks = () => {
        return (
            <div className={"reportCheckboxWrapper"}>
                <input
                    type="checkbox"
                    className="checkbox"
                    id="use-report-breaks-checkbox"
                    checked={props.isReportLevelChecked}
                    onChange={() => props.setIsReportLevelChecked!(!props.isReportLevelChecked)}
                />
                <label htmlFor="use-report-breaks-checkbox" className="use-report-settings-label">
                    Use report settings
                </label>
            </div>
        );
    };

    const addMetricAsBreak = (metric: Metric) => {
        if (props.googleTagManager && props.pageHandler) {
            props.googleTagManager.addEvent("addCrosstabBreak", props.pageHandler, { value: metric?.name });
        }
        MixPanel.track("addedCrosstabBreak");
        const clonedBreaks = [...props.selectedBreaks];
        const isBasedOnSingleChoice = questionTypeLookup[metric.name] == MainQuestionType.SingleChoice;
        const newBreak = new CrossMeasure({
            measureName: metric.name,
            filterInstances: getAvailableCrossMeasureFilterInstances(
                metric,
                entityConfiguration,
                false,
                isBasedOnSingleChoice
            ),
            childMeasures: [],
            multipleChoiceByValue: false,
        });
        clonedBreaks.push(newBreak);
        props.setSelectedBreaks(clonedBreaks);
        updateActiveBreaks(clonedBreaks);
    };

    const updateActiveBreaks = (updatedBreaks: CrossMeasure[]) => {
        if (props.parentComponent === BreakPickerParent.Crosstab) {
            const matchingSavedBreak = savedBreaks.find((b) => doBreaksMatch(b.breaks, updatedBreaks));
            const breaks = !matchingSavedBreak
                ? getActiveBreaksFromSelection(undefined, undefined, undefined, questionTypeLookup)
                : getActiveBreaksFromSelection(matchingSavedBreak, undefined, undefined, questionTypeLookup);
            setBreaksAndPeriod(breaks, setQueryParameter, props.pageHandler.session.activeView.curatedFilters, props.pageHandler, dispatch);
        }
    }

    const addSavedBreak = (savedBreak: SavedBreakCombination) => {
        if (props.googleTagManager && props.pageHandler) {
            props.googleTagManager.addEvent("addCrosstabBreak", props.pageHandler, { value: savedBreak?.name });
        }
        MixPanel.track("addedCrosstabBreak");
        const clonedBreaks = props.selectedBreaks.concat(savedBreak.breaks);
        updateActiveBreaks(clonedBreaks);
        props.setSelectedBreaks(clonedBreaks);
    };

    const updateBreaks = (breaks: Metric | SavedBreakCombination | CrossMeasure[]) => {
        if (breaks instanceof Metric) {
            return addMetricAsBreak(breaks);
        } else if (breaks instanceof SavedBreakCombination) {
            return addSavedBreak(breaks);
        } else {
            updateActiveBreaks(breaks);
            props.setSelectedBreaks(breaks);
        }
    }

    const renderSaveEditButton = () => {
        if (props.selectedBreaks.length === 0 || props.isReportLevelChecked)
            return (<></>);

        return (<FeatureGuard permissions={[savedBreakThatMatchesSelectedBreak ? PermissionFeaturesOptions.BreaksEdit : PermissionFeaturesOptions.BreaksAdd]}>
                <div className={props.isDisabled ? "disabled" : ""}>
                    <div className="save-breaks-buttons">
                        <button
                            onClick={() => setSaveBreaksModalOpen(true)}
                            className={
                                props.isDisabled
                                    ? "hollow-button save-breaks disabled"
                                    : "hollow-button save-breaks"}
                            disabled={props.isDisabled}
                        >
                            {savedBreakThatMatchesSelectedBreak ? "Edit" : "Save"}
                        </button>
                    </div>
                </div>
            </FeatureGuard>
        );
    }

    const getHeaderButtons = () => {
        return (
            <div className={"breakPickerButtons"}>
                {(props.supportMultiBreaks || props.selectedBreaks.length == 0 || props.reportType == ReportType.Table) && (
                    <AddBreakDropdown
                        metrics={metricsForBreaks}
                        isPrimaryAction={props.isPrimaryAction}
                        isDisabled={isDisabled}
                        addBreak={updateBreaks}
                        groupCustomVariables={props.groupCustomVariables}
                        showCreateVariableButton={false}
                    />
                )}
                {props.canSaveAndLoad && (
                    <>
                        <SaveEditBreaksModal
                            isOpen={saveBreaksModalOpen}
                            breakForEditing={savedBreakThatMatchesSelectedBreak}
                            breaks={props.selectedBreaks}
                            user={props.user}
                            closeModal={() => setSaveBreaksModalOpen(false)}
                            parentComponent={props.parentComponent}
                        />
                        {renderSaveEditButton()}
                    </>
                )}
            </div>
        );
    };

    const getSelectedBreaksContent = () => {
        if (props.selectedBreaks.length === 0 || props.isReportLevelChecked) {
            return (
                <NoBreaksMessage
                    reportType={props.reportType === undefined ? ReportType.Table : props.reportType}
                    isDisabled={isDisabled}
                    isReportSettings={props.isReportSettings}
                />
            );
        } else {
            return (
                <MultipleBreaksPicker
                    metrics={metricsForBreaks}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.pageHandler}
                    categories={props.selectedBreaks}
                    onCategoriesChange={updateBreaks}
                    isDisabled={isDisabled}
                    disableNesting={props.reportType === ReportType.Chart}
                    displayBreakInstanceSelector={props.displayBreakInstanceSelector}
                    parent={props.parentComponent}
                />
            );
        }
    };

    return (
        <div className="breaksPicker">
            {props.reportType !== undefined && !props.isReportSettings && !props.isDisabled && getUseReportLevelBreaks()}
            {getHeaderButtons()}
            {getSelectedBreaksContent()}
        </div>
    );
};

export default BreaksPicker;