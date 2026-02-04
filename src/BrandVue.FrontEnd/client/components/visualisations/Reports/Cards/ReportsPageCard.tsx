import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import { CrossMeasure, 
    PartDescriptor, 
    ReportOrder} from "../../../../BrandVueApi";
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { useCombinedRefs } from '../../../../helpers/RefHelper';
import useOnOffScreen from '../../../../helpers/UseOnOffScreen';
import { Metric } from '../../../../metrics/metric';
import {
    getFilterInstancesForPart,
    getReportPartBaseExpressionOverride,
    getReportPartDisplayText,
    getSplitByAndFilterByEntityTypesForPart,
    hasSingleEntityInstance
} from '../../../helpers/SurveyVueUtils';
import Separator from '../../../helpers/Separator';
import { PartType } from '../../../panes/PartType';
import TileTemplate from '../../shared/TileTemplate';
import { mainQuestionTypeToString } from '../Utility/MainQuestionTypeHelpers';
import { PageCardPlaceholder } from '../../shared/PageCardPlaceholder';
import { PageCardState } from '../../shared/SharedEnums';
import ReportsPageCardContextMenu from './ReportsPageCardContextMenu';
import { PartWithExtraData } from '../ReportsPageDisplay';
import { FilterInstance } from '../../../../entity/FilterInstance';
import ReportsPageCardChartContent from './ReportsPageCardChartContent';
import ReportCardLowSampleWarning from '../Components/ReportCardLowSampleWarning';
import { hasNoAnswersSelected, NothingSelectedCard } from "../../Cards/NothngSelectedCard";
import { PageHandler } from '../../../PageHandler';
import { useEntityConfigurationStateContext } from '../../../../entity/EntityConfigurationStateContext';
import { SortAverages } from '../../AverageHelper';
import { useAppDispatch, useAppSelector } from '../../../../state/store';
import { setReportErrorState, setIsSettingsChange } from '../../../../state/reportSlice';
import { initialReportErrorState } from '../../shared/ReportErrorState';
import { getCurrentChartType } from '../Charts/ReportsChartHelper';
import { selectCurrentReport } from 'client/state/reportSelectors';

interface IReportsPageCardProps {
    reportPart: PartWithExtraData;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    curatedFilters: CuratedFilters;
    overTimeFilters: CuratedFilters;
    questionTypeLookup: { [key: string]: BrandVueApi.MainQuestionType };
    cardType: ReportsPageCardType;
    canEditReport: boolean;
    reportOrder: ReportOrder;
    removeFromReport(): void;
    viewChartPage?(): void;
    breaks?: CrossMeasure[];
    waves?: CrossMeasure;
    setIsLowSample?(isLowSample: boolean): void;
    showWeightedCounts: boolean;
    updateBreaks(b: CrossMeasure[]): void;
    updateWave(w: CrossMeasure): void;
    duplicatePart(partDescriptor: PartDescriptor): void;
    updatePart(colours: string[]): void;
    isUsingOverTime: boolean;
}

export enum ReportsPageCardType {
    Tile,
    FullChart,
}

const ReportsPageCard = React.forwardRef<HTMLDivElement, IReportsPageCardProps>((props: IReportsPageCardProps, ref) => {
    const dispatch = useAppDispatch();
    const currentReportPage = useAppSelector(selectCurrentReport);
    const report = currentReportPage.report;
    const reportErrorState = useAppSelector(state => state.report.errorState);
    const isSettingsChange = useAppSelector(state => state.report.isSettingsChange);    
    const [dataState, setDataState] = React.useState(PageCardState.NotOnScreen);
    //Need to combine refs here as we can't access the "current" property of the forwared ref
    const onScreenRef = React.useRef<HTMLDivElement>(null);
    const combinedRef = useCombinedRefs(ref, onScreenRef);
    const isOnScreen = useOnOffScreen({ ref: onScreenRef, initialState: false, useDebounce: true, disconnectAfterOnScreen: true });
    const { entityConfiguration } = useEntityConfigurationStateContext();
    const isDoubleSpanCard = props.reportPart.part.partType === PartType.ReportsCardFunnel && (props.waves || props.breaks || props.isUsingOverTime) && !(props.waves && props.breaks);

    const canShowCard = () => {
        if (!props.reportPart.metric) {
            return false;
        }

        const validCalcType = props.reportPart.metric.calcType === BrandVueApi.CalculationType.YesNo
            || props.reportPart.metric.calcType === BrandVueApi.CalculationType.NetPromoterScore
            || props.reportPart.metric.calcType === BrandVueApi.CalculationType.Average
            || props.reportPart.metric.calcType === BrandVueApi.CalculationType.Text;

        return validCalcType;
    }

    React.useEffect(() => {
        if (!props.reportPart.metric) {
            setDataState(PageCardState.Error);
            return;
        }

        if (!isOnScreen) {
            setDataState(PageCardState.NotOnScreen);
            return;
        }

        if (!canShowCard()) {
            setDataState(PageCardState.InvalidQuestion);
            return;
        }

        if (hasNoAnswersSelected(props.reportPart)) {
            setDataState(PageCardState.NothingSelected)
            return;
        }

        if (reportErrorState.isError && !isSettingsChange) {
            return;
        }

        setDataState(PageCardState.Show);
        dispatch(setIsSettingsChange(false));
        dispatch(setReportErrorState(initialReportErrorState));
    }, [isOnScreen, props.curatedFilters, props.reportPart, reportErrorState]);

    const getCardDescription = (isLowSample?: boolean, filterInstances?: FilterInstance[], hideFilterInstances?: boolean) => {
        const metricName = props.reportPart.metric ? props.reportPart.metric.displayName : props.reportPart.part.spec1;
        const questionType = props.reportPart.metric ? questionTypeText(props.reportPart.metric) : "";
        const helptext = props.reportPart.metric?.isAutoGeneratedNumeric() ?
            "Auto grouped: " + getReportPartDisplayText(props.reportPart) :
            getReportPartDisplayText(props.reportPart);
        return (
            <>
                <div>
                    {(!props.reportPart.metric?.eligibleForCrosstabOrAllVue || props.reportPart.metric?.disableMeasure) &&
                        <div className="hidden-disabled">
                            <i className="material-symbols-outlined no-symbol-fill">{props.reportPart.metric?.disableMeasure ? "error" : "visibility_off"}</i>
                            &nbsp; Metric has been {props.reportPart.metric?.disableMeasure ? "disabled" : "hidden"}
                        </div>
                    }
                    <div className="name-and-options">
                        <div className="name-and-type">
                            <div className="question-name-text" title={metricName}>{metricName}</div>
                            <Separator />
                            <div className="question-type-text">{questionType}</div>
                        </div>
                        <div className="card-options">
                            <ReportsPageCardContextMenu
                                metric={props.reportPart.metric}
                                googleTagManager={props.googleTagManager}
                                pageHandler={props.pageHandler}
                                canEditReport={props.canEditReport}
                                canExploreData={true}
                                removeFromReport={props.removeFromReport}
                                duplicatePart={props.duplicatePart}
                                currentPart={props.reportPart.part}
                            />
                            <ReportCardLowSampleWarning
                                id={props.reportPart.part.spec2.toString()}
                                isLowSample={report.highlightLowSample && isLowSample}
                                shrink={true}
                                isLineChart={props.reportPart.part.partType === PartType.ReportsCardLine} />
                        </div>
                    </div>
                    <div className="question-text">{helptext}</div>
                </div>
                {!hideFilterInstances && filterInstances && filterInstances.length > 0 &&
                    <div className="filter-instance-container">
                        {filterInstances.map(instance =>
                            <div className="filter-instance" key={instance.instance.name}>
                                {instance.type.displayNameSingular}: {instance.instance.name}
                            </div>
                        )}
                    </div>
                }
            </>
        );
    }

    const questionTypeText = (metric: Metric): string => {
        const questionTypeForMetric = props.questionTypeLookup[metric.name];
        return mainQuestionTypeToString(questionTypeForMetric);
    };

    const isUsingWaves = props.waves !== undefined;

    const getTileContent = () => {
        switch (dataState) {
            case PageCardState.NotOnScreen:
                return <TileTemplate descriptionNode={getCardDescription()}>
                    <PageCardPlaceholder />
                </TileTemplate>;
            case PageCardState.NoData:
                return <TileTemplate descriptionNode={getCardDescription()}>
                    <div className="card-error no-data">
                        <div>No results</div>
                    </div>
                </TileTemplate>;
            case PageCardState.Error:
                return <TileTemplate descriptionNode={getCardDescription()}>
                    <div className="card-error error">
                        <i className="material-symbols-outlined no-symbol-fill">info</i>
                        <div>There was an error loading results</div>
                    </div>
                </TileTemplate>;
            case PageCardState.InvalidQuestion:
                return <TileTemplate descriptionNode={getCardDescription()}>
                    <div className="card-error invalid-question">
                        <i className="material-symbols-outlined">speaker_notes_off</i>
                        <div>Results can't be shown for this question type</div>
                    </div>
                </TileTemplate>;
            case PageCardState.NothingSelected:
                return <NothingSelectedCard descriptionNode={getCardDescription()} />
            case PageCardState.ChartTypeNotSupported:
                return <TileTemplate descriptionNode={getCardDescription()}>
                    <div className="card-error error">
                        <i className="material-symbols-outlined no-symbol-fill">info</i>
                        <div>The selected chart type is not supported</div>
                    </div>
                </TileTemplate>;
            case PageCardState.Show:
                return getContentForPartType();
            case PageCardState.NotSupportedOverlap:
                return <TileTemplate descriptionNode={getCardDescription()}>
                    <div className="card-error error">
                        <i className="material-symbols-outlined no-symbol-fill">warning</i>
                        <div>More than one overlap found between net results or more than one overlap found between net and non-net results are not supported</div>
                    </div>
                </TileTemplate>
            case PageCardState.UnsupportedVariable:
                return <TileTemplate descriptionNode={getCardDescription()}>
                    <div className="card-error error">
                        <i className="material-symbols-outlined no-symbol-fill">warning</i>
                        <div>Variable configuration not supported for {getCurrentChartType(props.reportPart.part.partType, isUsingWaves, props.isUsingOverTime, props.reportPart.metric!, props.questionTypeLookup)} chart</div>
                        <div>{reportErrorState.errorMessage}</div>
                    </div>
                </TileTemplate>
        }
    }

    const getContentForPartType = () => {
        const entityTypes = getSplitByAndFilterByEntityTypesForPart(props.reportPart.part, props.reportPart.metric, entityConfiguration);

        if (!entityTypes) {
            throw new Error("Could not get entity types for part");
        }

        const filterInstances = getFilterInstancesForPart(props.reportPart.part, entityTypes, entityConfiguration);
        const baseExpressionOverride = getReportPartBaseExpressionOverride(props.reportPart, report.baseTypeOverride, report.baseVariableId);

        const showTop = props.reportPart.part.showTop;
        const sortingOrder = props.reportPart.part.reportOrder ?? report.reportOrder;
        const selectedInstances = props.reportPart.selectedEntitySet?.getInstances().getAll().map(i => i.id);
        const enabledAverages = hasSingleEntityInstance(props.reportPart.metric, selectedInstances) ?
            [] : [...props.reportPart.part.averageTypes].sort((a, b) => SortAverages(a, b));

        return (
            <ReportsPageCardChartContent
                reportPart={props.reportPart}
                waves={props.waves}
                questionTypeLookup={props.questionTypeLookup}
                splitByEntityType={entityTypes.splitByEntityType}
                filterInstances={filterInstances}
                getDescriptionNode={(isLowSample: boolean, hideFilterInstances?: boolean) =>
                    getCardDescription(isLowSample, filterInstances.secondaryFilterInstances, hideFilterInstances)}
                googleTagManager={props.googleTagManager}
                pageHandler={props.pageHandler}
                curatedFilters={props.curatedFilters}
                overTimeFilters={props.overTimeFilters}
                cardType={props.cardType}
                reportOrder={sortingOrder}
                showTop={showTop}
                setDataState={setDataState}
                breaks={props.breaks}
                baseExpressionOverride={baseExpressionOverride}
                setIsLowSample={props.setIsLowSample}
                showWeightedCounts={props.showWeightedCounts}
                averageTypes={enabledAverages}
                setAverageMentions={() => { }} //mentions are not visible for cards
                updateBreak={(b) => { props.updateBreaks(b) }}
                updateWave={props.updateWave}
                updatePart={props.updatePart}
                isUsingOverTime={props.isUsingOverTime}
            />
        );
    }

    const getCardClassName = () => {
        let className = '';

        if (props.cardType === ReportsPageCardType.Tile) {
            className += 'page-card';
        } else if (props.cardType === ReportsPageCardType.FullChart) {
            className += 'page-card-full';
        }

        if (props.cardType === ReportsPageCardType.Tile && isDoubleSpanCard) {
            className += ' double-span';
        }

        if (dataState === PageCardState.Show) {
            if (props.reportPart.part.partType == PartType.ReportsCardText) {
                className += ' is-text'
            } else {
                className += ' has-data';
            }
        }

        if (canLinkToChartPage()) {
            className += ' has-link';
        }

        if (!props.reportPart.metric?.eligibleForCrosstabOrAllVue || props.reportPart.metric?.disableMeasure) {
            className += ' disabled';
        }

        return className;
    };

    const canLinkToChartPage = () => (dataState === PageCardState.Show || props.canEditReport) && props.viewChartPage != null;

    return (
        <div ref={combinedRef} className={getCardClassName()} onClick={canLinkToChartPage() ? props.viewChartPage : undefined} tabIndex={500}>
            {getTileContent()}
        </div>
    )
});

export default ReportsPageCard;