import React from 'react';
import {
    AverageType,
    BaseExpressionDefinition,
    CrossMeasure,
    CrosstabAverageResults,
    IEntityType,
    MainQuestionType,
    ReportOrder,
    ReportType,
} from '../../../../BrandVueApi';
import { EntitySet } from "../../../../entity/EntitySet";
import { CuratedFilters } from '../../../../filter/CuratedFilters';
import { IGoogleTagManager } from '../../../../googleTagManager';
import { PrimaryAndSecondaryFilterInstances, baseExpressionDefinitionsAreEqual } from '../../../helpers/SurveyVueUtils';
import { PageHandler } from '../../../PageHandler';
import { PartType } from '../../../panes/PartType';
import HeatmapImageCard from '../../shared/HeatmapImageCard';
import { PageCardState } from '../../shared/SharedEnums';
import TextCard from '../../shared/TextCard';
import ReportsPageBarCard from '../Charts/ReportsPageBarCard';
import ReportsPageColumnChart from '../Charts/ReportsPageColumnChart';
import ReportsPageDoughnutCard from '../Charts/ReportsPageDoughnutCard';
import ReportsPageDoughnutChart from '../Charts/ReportsPageDoughnutChart';
import ReportsPageFunnelCard from '../Charts/ReportsPageFunnelCard';
import ReportsPageFunnelCardMulti from '../Charts/ReportsPageFunnelCardMulti';
import ReportsPageFunnelChart from '../Charts/ReportsPageFunnelChart';
import ReportsPageFunnelChartMulti from '../Charts/ReportsPageFunnelChartMulti';
import ReportsPageLineCard from '../Charts/ReportsPageLineCard';
import ReportsPageLineChart from '../Charts/ReportsPageLineChart';
import ReportsPageMultiBreakBarCard from "../Charts/ReportsPageMultiBreakBarCard";
import ReportsPageMultiBreakColumnChart from '../Charts/ReportsPageMultiBreakColumnChart';
import ReportsPageOvertimeColumnChart from '../Charts/ReportsPageOvertimeColumnChart';
import ReportsPageOvertimeBarCard from '../Charts/ReportsPageOvertimeBarCard';
import ReportsPageOvertimeLineCard from '../Charts/ReportsPageOvertimeLineCard';
import ReportsPageOvertimeLineChart from '../Charts/ReportsPageOvertimeLineChart';
import ReportsPageOvertimeStackedColumnChart from '../Charts/ReportsPageOvertimeStackedColumnChart';
import ReportsPageOvertimeStackedBarCard from '../Charts/ReportsPageOvertimeStackedBarCard';
import ReportsPageSplitBarCard from '../Charts/ReportsPageSplitBarCard';
import ReportsPageSplitColumnChart from '../Charts/ReportsPageSplitColumnChart';
import ReportsPageSplitStackedBarCard from '../Charts/ReportsPageSplitStackedBarCard';
import ReportsPageSplitStackedColumnChart from '../Charts/ReportsPageSplitStackedColumnChart';
import ReportsPageStackedBarCard from '../Charts/ReportsPageStackedBarCard';
import ReportsPageStackedColumnChart from '../Charts/ReportsPageStackedColumnChart';
import { PartWithExtraData } from "../ReportsPageDisplay";
import { getPartTypeForMetric } from '../Utility/ReportPageBuilder';
import { ReportsPageCardType } from './ReportsPageCard';
import ReportsPageOvertimeFunnelChart from 'client/components/visualisations/Reports/Charts/ReportsPageOvertimeFunnelChart';
import BrandVueOnlyLowSampleHelper from '../../BrandVueOnlyLowSampleHelper';
import ReportsPageOvertimeFunnelCard from '../Charts/ReportsPageOvertimeFunnelCard';

interface IReportsPageCardChartContentProps {
    reportPart: PartWithExtraData;
    questionTypeLookup: { [key: string]: MainQuestionType };
    splitByEntityType: IEntityType | undefined;
    filterInstances: PrimaryAndSecondaryFilterInstances;
    getDescriptionNode: (isLowSample: boolean, hideFilterInstances?: boolean) => JSX.Element;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    curatedFilters: CuratedFilters;
    overTimeFilters: CuratedFilters;
    cardType: ReportsPageCardType;
    reportOrder: ReportOrder;
    showTop: number | undefined;
    setDataState(state: PageCardState): void;
    breaks?: CrossMeasure[];
    waves?: CrossMeasure;
    baseExpressionOverride?: BaseExpressionDefinition;
    setIsLowSample?(isLowSample: boolean): void;
    showWeightedCounts: boolean;
    averageTypes: AverageType[];
    setAverageMentions(result: CrosstabAverageResults): void;
    updateBreak(b: CrossMeasure[]): void;
    updateWave(w: CrossMeasure): void;
    updatePart(colours: string[]): void;
    isUsingOverTime: boolean;
}

const ReportsPageCardChartContent = React.memo((props: IReportsPageCardChartContentProps) => {
    return (<ReportsPageCardChartContentComponent {...props} />);
}, (prevProps, nextProps) => {
    return prevProps.reportPart.part.partType === nextProps.reportPart.part.partType &&
        prevProps.reportPart.metric!.name === nextProps.reportPart.metric!.name &&
        prevProps.splitByEntityType?.identifier === nextProps.splitByEntityType?.identifier &&
        JSON.stringify(prevProps.filterInstances) === JSON.stringify(nextProps.filterInstances) &&
        prevProps.reportOrder === nextProps.reportOrder &&
        prevProps.curatedFilters === nextProps.curatedFilters &&
        prevProps.overTimeFilters === nextProps.overTimeFilters &&
        prevProps.showTop === nextProps.showTop &&
        JSON.stringify(prevProps.averageTypes) == JSON.stringify(nextProps.averageTypes) &&
        baseExpressionDefinitionsAreEqual(prevProps.baseExpressionOverride, nextProps.baseExpressionOverride) &&
        JSON.stringify(prevProps.breaks) === JSON.stringify(nextProps.breaks) &&
        JSON.stringify(prevProps.waves) === JSON.stringify(nextProps.waves) &&
        entitySetsAreEqual(prevProps.reportPart.selectedEntitySet, nextProps.reportPart.selectedEntitySet) &&
        prevProps.reportPart.part.multiBreakSelectedEntityInstance === nextProps.reportPart.part.multiBreakSelectedEntityInstance &&
        JSON.stringify(prevProps.reportPart.part.customConfigurationOptions) === JSON.stringify(nextProps.reportPart.part.customConfigurationOptions) &&
        prevProps.reportPart.part.displayMeanValues === nextProps.reportPart.part.displayMeanValues &&
        prevProps.isUsingOverTime === nextProps.isUsingOverTime
}
);
export default ReportsPageCardChartContent;

export const BarColour = "#535B97";
export const responsiveBreakpoints = [[775, 500], [600, 400], [500, 300], [400, 225], [300, 150]];
export const BarPointWidth = 32;

function entitySetsAreEqual(a: EntitySet | undefined, b: EntitySet | undefined) {
    if (a && b) {
        const aIds = a?.getInstances().getAll().map(entitySet => entitySet.id)
        const bIds = b?.getInstances().getAll().map(entitySet => entitySet.id)
        return (JSON.stringify(aIds.sort()) === JSON.stringify(bIds.sort()))
    }
    return a === b;
}

function ReportsPageCardChartContentComponent(props: IReportsPageCardChartContentProps) {

    const getOvertimeLineChart = () => {
        const commonProps = {
            reportPart: props.reportPart,
            curatedFilters: props.overTimeFilters,
            questionTypeLookup: props.questionTypeLookup,
            setDataState: props.setDataState,
            splitByType: props.splitByEntityType,
            filterInstances: props.filterInstances.secondaryFilterInstances,
            baseExpressionOverride: props.baseExpressionOverride,
            order: props.reportOrder,
        };
        if (props.cardType === ReportsPageCardType.FullChart) {
            return <ReportsPageOvertimeLineChart
                {...commonProps}
                averageTypes={props.averageTypes}
                setIsLowSample={props.setIsLowSample}
            />;
        } else {
            return <ReportsPageOvertimeLineCard
                {...commonProps}
                getDescriptionNode={props.getDescriptionNode}
            />;
        }
    };

    const getLineChart = () => {
        const singleBreak = props.breaks ? props.breaks[0] : undefined;
        if (props.isUsingOverTime) {
            return getOvertimeLineChart();
        }
        if (props.waves) {
            if (props.cardType == ReportsPageCardType.FullChart) {
                return <ReportsPageLineChart
                    reportPart={props.reportPart}
                    waves={props.waves}
                    curatedFilters={props.curatedFilters}
                    questionTypeLookup={props.questionTypeLookup}
                    setDataState={props.setDataState}
                    breaks={singleBreak}
                    splitByType={props.splitByEntityType}
                    filterInstances={props.filterInstances.secondaryFilterInstances}
                    baseExpressionOverride={props.baseExpressionOverride}
                    setIsLowSample={props.setIsLowSample}
                    averageTypes={props.averageTypes}
                    updateWave={props.updateWave}
                    
                />
            } else {
                return <ReportsPageLineCard
                    reportPart={props.reportPart}
                    waves={props.waves}
                    getDescriptionNode={props.getDescriptionNode}
                    curatedFilters={props.curatedFilters}
                    questionTypeLookup={props.questionTypeLookup}
                    setDataState={props.setDataState}
                    breaks={singleBreak}
                    splitByType={props.splitByEntityType}
                    filterInstances={props.filterInstances.secondaryFilterInstances}
                    baseExpressionOverride={props.baseExpressionOverride}
                    showWeightedCounts={!!props.showWeightedCounts}
                    averageTypes={props.averageTypes}
                />
            }
        }
        const alternatePartType = getPartTypeForMetric(props.reportPart.metric!, props.questionTypeLookup, ReportType.Chart, false);
        return getContent(alternatePartType);
    }

    const getContent = (partType: string) => {
        if (props.cardType == ReportsPageCardType.FullChart) {
            switch (partType) {
                case PartType.ReportsCardChart:
                case PartType.ReportsCardMultiEntityMultipleChoice:
                    if (props.isUsingOverTime) {
                        return <ReportsPageOvertimeColumnChart
                            reportPart={props.reportPart}
                            questionTypeLookup={props.questionTypeLookup}
                            curatedFilters={props.overTimeFilters}
                            splitByType={props.splitByEntityType}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            baseExpressionOverride={props.baseExpressionOverride}
                            order={props.reportOrder}
                            setDataState={props.setDataState}
                            setIsLowSample={props.setIsLowSample}
                            averageTypes={props.averageTypes}
                        />;
                    }
                        if (props.breaks && props.breaks.length > 0) {
                        if (props.breaks.length > 1) {
                            return <ReportsPageMultiBreakColumnChart
                                reportPart={props.reportPart}
                                curatedFilters={props.curatedFilters}
                                questionTypeLookup={props.questionTypeLookup}
                                setDataState={props.setDataState}
                                splitByType={props.splitByEntityType}
                                primaryFilterInstance={props.filterInstances.primaryFilterInstance!}
                                filterInstances={props.filterInstances.secondaryFilterInstances}
                                showTop={props.showTop}
                                breaks={props.breaks}
                                baseExpressionOverride={props.baseExpressionOverride}
                                setIsLowSample={props.setIsLowSample}
                                averageTypes={props.averageTypes}
                                updateBreak={props.updateBreak}
                                order={props.reportOrder}
                            />
                        }
                        return <ReportsPageSplitColumnChart
                                reportPart={props.reportPart}
                                curatedFilters={props.curatedFilters}
                                questionTypeLookup={props.questionTypeLookup}
                                setDataState={props.setDataState}
                                splitByType={props.splitByEntityType}
                                filterInstances={props.filterInstances.secondaryFilterInstances}
                                showTop={props.showTop}
                                breaks={props.breaks[0]}
                                baseExpressionOverride={props.baseExpressionOverride}
                                setIsLowSample={props.setIsLowSample}
                                averageTypes={props.averageTypes}
                                updateBreak={props.updateBreak}
                                order={props.reportOrder}
                            />;
                    }
                    return <ReportsPageColumnChart
                        reportPart={props.reportPart}
                        questionTypeLookup={props.questionTypeLookup}
                        curatedFilters={props.curatedFilters}
                        splitByType={props.splitByEntityType}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        showTop={props.showTop}
                        baseExpressionOverride={props.baseExpressionOverride}
                        setDataState={props.setDataState}
                        setIsLowSample={props.setIsLowSample}
                        averageTypes={props.averageTypes}
                        setAverageMentions={props.setAverageMentions}
                        order={props.reportOrder}
                    />;
                case PartType.ReportsCardStackedMulti:
                    if (props.isUsingOverTime) {
                        return <ReportsPageOvertimeStackedColumnChart
                            reportPart={props.reportPart}
                            curatedFilters={props.overTimeFilters}
                            questionTypeLookup={props.questionTypeLookup}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            splitByType={props.splitByEntityType!}
                            baseExpressionOverride={props.baseExpressionOverride}
                            order={props.reportOrder}
                            averageTypes={props.averageTypes}
                            setDataState={props.setDataState}
                            setIsLowSample={props.setIsLowSample}
                        />;
                    }
                    if (props.breaks && props.breaks.length > 0) {
                        if (props.breaks.length > 1) {
                            return <div>Multiple breaks unhandled</div>
                        }
                        if (props.breaks.length > 0 
                            && props.filterInstances.secondaryFilterInstances.length > 1
                            && (props.isUsingOverTime || props.waves)) {
                            return <div>Selected chart cannot display carousel questions with breaks over time</div>
                        }
                        return <ReportsPageSplitStackedColumnChart
                            reportPart={props.reportPart}
                            curatedFilters={props.curatedFilters}
                            questionTypeLookup={props.questionTypeLookup}
                            splitByType={props.splitByEntityType!}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            breaks={props.breaks[0]}
                            baseExpressionOverride={props.baseExpressionOverride}
                            showWeightedCounts={!!props.showWeightedCounts}
                            setDataState={props.setDataState}
                            setIsLowSample={props.setIsLowSample}
                            averageTypes={props.averageTypes}
                            updateBreak={(b) => props.updateBreak(b)}
                            
                        />;
                    }
                        return <ReportsPageStackedColumnChart
                        reportPart={props.reportPart}
                        metric={props.reportPart.metric!}
                        curatedFilters={props.curatedFilters}
                        questionTypeLookup={props.questionTypeLookup}
                        order={props.reportOrder}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        splitByType={props.splitByEntityType!}
                        baseExpressionOverride={props.baseExpressionOverride}
                        showWeightedCounts={!!props.showWeightedCounts}
                        averageTypes={props.averageTypes}
                        setDataState={props.setDataState}
                        setIsLowSample={props.setIsLowSample}
                        selectedEntityInstances={props.reportPart.part.selectedEntityInstances}
                    />;
                case PartType.ReportsCardText:
                    return <TextCard
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        metric={props.reportPart.metric!}
                        getDescriptionNode={props.getDescriptionNode}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        curatedFilters={props.curatedFilters}
                        baseExpressionOverride={props.baseExpressionOverride}
                        setDataState={props.setDataState}
                        setIsLowSample={props.setIsLowSample}
                        lowSampleThreshold={BrandVueOnlyLowSampleHelper.lowSampleForEntity}
                        fullWidth
                    />;
                case PartType.ReportsCardLine:
                    return getLineChart()
                case PartType.ReportsCardDoughnut:
                    return <ReportsPageDoughnutChart
                        reportPart={props.reportPart}
                        questionTypeLookup={props.questionTypeLookup}
                        curatedFilters={props.curatedFilters}
                        splitByType={props.splitByEntityType}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        order={props.reportOrder}
                        showTop={props.showTop}
                        baseExpressionOverride={props.baseExpressionOverride}
                        setDataState={props.setDataState}
                        setIsLowSample={props.setIsLowSample}
                        averageTypes={props.averageTypes}
                        setAverageMentions={props.setAverageMentions}
                        updatePart={props.updatePart}
                    />;
                case PartType.ReportsCardHeatmapImage:
                    return <HeatmapImageCard
                        metric={props.reportPart.metric!}
                        curatedFilters={props.curatedFilters}
                        setDataState={props.setDataState}
                        baseExpressionOverride={props.baseExpressionOverride}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        displayFooter={true}
                        heatmapOptions={props.reportPart.part.customConfigurationOptions}
                        user={null}
                    />;
                case PartType.ReportsCardFunnel:
                    if (props.isUsingOverTime) {
                        return <ReportsPageOvertimeFunnelChart
                            reportPart={props.reportPart}
                            curatedFilters={props.overTimeFilters}
                            questionTypeLookup={props.questionTypeLookup}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            splitByType={props.splitByEntityType!}
                            baseExpressionOverride={props.baseExpressionOverride}
                            averageTypes={props.averageTypes}
                            order={props.reportOrder}
                            setDataState={props.setDataState}
                            setIsLowSample={props.setIsLowSample}
                        />;
                    }
                    if ((props.waves || props.breaks) && !(props.waves && props.breaks)) {
                        return <ReportsPageFunnelChartMulti
                                    reportPart={props.reportPart}
                                    waves={props.waves}
                                    breaks={props.breaks}
                                    questionTypeLookup={props.questionTypeLookup}
                                    curatedFilters={props.curatedFilters}
                                    filterInstances={props.filterInstances.secondaryFilterInstances}
                                    baseExpressionOverride={props.baseExpressionOverride}
                                    setDataState={props.setDataState}
                                    setIsLowSample={props.setIsLowSample}
                                    order={props.reportOrder}
                                />;
                    }

                    return <ReportsPageFunnelChart
                        reportPart={props.reportPart}
                        questionTypeLookup={props.questionTypeLookup}
                        curatedFilters={props.curatedFilters}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        order={props.reportOrder}
                        baseExpressionOverride={props.baseExpressionOverride}
                        setDataState={props.setDataState}
                        setIsLowSample={props.setIsLowSample}
                    />;
                default:
                    props.setDataState(PageCardState.Error);
                    return null;
            }
        }
        else {
            switch (partType) {
                case PartType.ReportsCardChart:
                case PartType.ReportsCardMultiEntityMultipleChoice:
                    if (props.isUsingOverTime) {
                        return <ReportsPageOvertimeBarCard
                            reportPart={props.reportPart}
                            getDescriptionNode={props.getDescriptionNode}
                            curatedFilters={props.overTimeFilters}
                            setDataState={props.setDataState}
                            splitByType={props.splitByEntityType}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            order={props.reportOrder}
                            questionTypeLookup={props.questionTypeLookup}
                            baseExpressionOverride={props.baseExpressionOverride}
                        />;
                    }
                    if (props.breaks && props.breaks.length > 0) {
                        if (props.breaks.length > 1) {
                            return <ReportsPageMultiBreakBarCard
                                reportPart={props.reportPart}
                                getDescriptionNode={props.getDescriptionNode}
                                curatedFilters={props.curatedFilters}
                                setDataState={props.setDataState}
                                splitByType={props.splitByEntityType}
                                primaryFilterInstance={props.filterInstances.primaryFilterInstance!}
                                filterInstances={props.filterInstances.secondaryFilterInstances}
                                order={props.reportOrder}
                                showTop={props.showTop}
                                breaks={props.breaks}
                                baseExpressionOverride={props.baseExpressionOverride}
                                showWeightedCounts={!!props.showWeightedCounts}
                                averageTypes={props.averageTypes}
                            />
                        }
                        return <ReportsPageSplitBarCard
                            reportPart={props.reportPart}
                            getDescriptionNode={props.getDescriptionNode}
                            curatedFilters={props.curatedFilters}
                            setDataState={props.setDataState}
                            splitByType={props.splitByEntityType}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            order={props.reportOrder}
                            showTop={props.showTop}
                            breaks={props.breaks[0]}
                            baseExpressionOverride={props.baseExpressionOverride}
                            showWeightedCounts={!!props.showWeightedCounts}
                            averageTypes={props.averageTypes}
                            updateBreak={props.updateBreak}
                        />;
                    }
                    return <ReportsPageBarCard
                        reportPart={props.reportPart}
                        getDescriptionNode={props.getDescriptionNode}
                        curatedFilters={props.curatedFilters}
                        setDataState={props.setDataState}
                        splitByType={props.splitByEntityType}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        order={props.reportOrder}
                        showTop={props.showTop}
                        baseExpressionOverride={props.baseExpressionOverride}
                        
                        showWeightedCounts={!!props.showWeightedCounts}
                    />;
                case PartType.ReportsCardStackedMulti:
                    if (props.isUsingOverTime) {
                        return <ReportsPageOvertimeStackedBarCard
                            reportPart={props.reportPart}
                            getDescriptionNode={props.getDescriptionNode}
                            curatedFilters={props.overTimeFilters}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            splitByType={props.splitByEntityType!}
                            baseExpressionOverride={props.baseExpressionOverride}
                            questionTypeLookup={props.questionTypeLookup}
                            setDataState={props.setDataState}
                            order={props.reportOrder}
                        />;
                    }
                    if (props.breaks && props.breaks.length > 0) {
                        if (props.breaks.length > 1) {
                            return <div>Multiple breaks unhandled</div>
                        }
                        return <ReportsPageSplitStackedBarCard
                            reportPart={props.reportPart}
                            getDescriptionNode={props.getDescriptionNode}
                            curatedFilters={props.curatedFilters}
                            splitByType={props.splitByEntityType!}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            breaks={props.breaks[0]}
                            baseExpressionOverride={props.baseExpressionOverride}
                            showWeightedCounts={!!props.showWeightedCounts}
                            setDataState={props.setDataState}
                            averageTypes={props.averageTypes}
                        />;
                    }
                    return <ReportsPageStackedBarCard
                        reportPart={props.reportPart}
                        metric={props.reportPart.metric!}
                        getDescriptionNode={props.getDescriptionNode}
                        curatedFilters={props.curatedFilters}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        splitByEntityType={props.splitByEntityType!}
                        baseExpressionOverride={props.baseExpressionOverride}
                        showWeightedCounts={!!props.showWeightedCounts}
                        averageTypes={props.averageTypes}
                        setDataState={props.setDataState}
                        order={props.reportOrder}
                        selectedEntityInstances={props.reportPart.part.selectedEntityInstances}
                    />;
                case PartType.ReportsCardText:
                    return <TextCard
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.pageHandler}
                        metric={props.reportPart.metric!}
                        getDescriptionNode={props.getDescriptionNode}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        baseExpressionOverride={props.baseExpressionOverride}
                        curatedFilters={props.curatedFilters}
                        setDataState={props.setDataState}
                        lowSampleThreshold={BrandVueOnlyLowSampleHelper.lowSampleForEntity}
                    />;
                case PartType.ReportsCardLine:
                    return getLineChart()
                case PartType.ReportsCardDoughnut:
                    return <ReportsPageDoughnutCard
                        reportPart={props.reportPart}
                        curatedFilters={props.curatedFilters}
                        splitByType={props.splitByEntityType}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        order={props.reportOrder}
                        showTop={props.showTop}
                        baseExpressionOverride={props.baseExpressionOverride}
                        showWeightedCounts={!!props.showWeightedCounts}
                        setDataState={props.setDataState}
                        getDescriptionNode={props.getDescriptionNode}
                        updatePart={props.updatePart}
                    />;
                case PartType.ReportsCardHeatmapImage:
                    return <HeatmapImageCard
                        metric={props.reportPart.metric!}
                        curatedFilters={props.curatedFilters}
                        setDataState={props.setDataState}
                        baseExpressionOverride={props.baseExpressionOverride}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        displayFooter={false}
                        heatmapOptions={props.reportPart.part.customConfigurationOptions}
                        getDescriptionNode={props.getDescriptionNode}
                        user={null }
                    />;
                case PartType.ReportsCardFunnel:
                    if (props.isUsingOverTime) {
                        return <ReportsPageOvertimeFunnelCard
                            reportPart={props.reportPart}
                            curatedFilters={props.overTimeFilters}
                            setDataState={props.setDataState}
                            splitByType={props.splitByEntityType}
                            filterInstances={props.filterInstances.secondaryFilterInstances}
                            order={props.reportOrder}
                            questionTypeLookup={props.questionTypeLookup}
                            baseExpressionOverride={props.baseExpressionOverride}
                            getDescriptionNode={props.getDescriptionNode}
                        />;
                    }
                    if ((props.waves || props.breaks) && !(props.waves && props.breaks)) {
                        return <ReportsPageFunnelCardMulti
                                reportPart={props.reportPart}
                                waves={props.waves}
                                breaks={props.breaks}
                                getDescriptionNode={props.getDescriptionNode}
                                curatedFilters={props.curatedFilters}
                                setDataState={props.setDataState}
                                filterInstances={props.filterInstances.secondaryFilterInstances}
                                order={props.reportOrder}
                                baseExpressionOverride={props.baseExpressionOverride}
                                showWeightedCounts={!!props.showWeightedCounts}
                            />;
                    }

                    return <ReportsPageFunnelCard
                        reportPart={props.reportPart}
                        getDescriptionNode={props.getDescriptionNode}
                        curatedFilters={props.curatedFilters}
                        setDataState={props.setDataState}
                        filterInstances={props.filterInstances.secondaryFilterInstances}
                        order={props.reportOrder}
                        baseExpressionOverride={props.baseExpressionOverride}
                        showWeightedCounts={!!props.showWeightedCounts}
                    />;
                default:
                    props.setDataState(PageCardState.Error);
                    return null;
            }
        }
    }

    return getContent(props.reportPart.part.partType)
}