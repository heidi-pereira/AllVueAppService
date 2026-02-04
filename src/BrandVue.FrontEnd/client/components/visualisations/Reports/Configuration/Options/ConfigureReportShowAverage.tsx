import React from 'react';
import {AverageType, PartDescriptor, Report, ReportType} from "../../../../../BrandVueApi";
import { SortAverages } from '../../../../../components/visualisations/AverageHelper';
import { IGoogleTagManager } from '../../../../../googleTagManager';
import { Metric } from '../../../../../metrics/metric';
import { getAnalyticsAverageAddedEvent, getAnalyticsAverageRemovedEvent, hasSingleEntityInstance } from '../../../../helpers/SurveyVueUtils';
import { PageHandler } from '../../../../PageHandler';
import AverageTypeSelector from './AverageTypeSelector';
import { useMetricStateContext } from '../../../../../metrics/MetricStateContext';
import { MixPanel } from '../../../../mixpanel/MixPanel';
import { selectCurrentReport } from 'client/state/reportSelectors';
import { useAppSelector } from 'client/state/store';
import { PartType } from 'client/components/panes/PartType';

interface IConfigureReportPartDisplayNameProps {
    part: PartDescriptor;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    savePartChanges(newPart: PartDescriptor): void;
    averageTypes: AverageType[];
    displayMeanValues: boolean;
    displayStandardDeviation: boolean;
    metric: Metric | undefined;
    selectedInstanceIds: number[];
}

const ConfigureReportShowAverage = (props: IConfigureReportPartDisplayNameProps) => {
    const { selectableMetricsForUser: metrics } = useMetricStateContext();
    const currentReportPage = useAppSelector(selectCurrentReport);
    const reportBreaks = currentReportPage.report.breaks;
    const reportType = currentReportPage.report.reportType;    

    const getDisabledMessage = () => {
        if (hasSingleEntityInstance(props.metric, props.selectedInstanceIds)){
            return 'Averages are disabled if there is only a single series';
        }

        const hasMoreThanOneBreak = (props.part.overrideReportBreaks && props.part.breaks.length > 1) 
        || (!props.part.overrideReportBreaks && reportBreaks.length > 1);
        if (reportType == ReportType.Chart && hasMoreThanOneBreak){
            return 'Averages are disabled if there is more than one break'
        }
        
        return undefined
    }
    
    const toggleSelectedAverage = (toggledAverageType: AverageType) => {
        let newAverages = [...props.averageTypes];
        const index = newAverages.indexOf(toggledAverageType);
        if(index == -1){
            props.googleTagManager.addEvent(getAnalyticsAverageAddedEvent(toggledAverageType, reportType), props.pageHandler);
            newAverages = newAverages.concat(toggledAverageType);
        } else {
            props.googleTagManager.addEvent(getAnalyticsAverageRemovedEvent(toggledAverageType, reportType), props.pageHandler);
            newAverages.splice(index, 1);
        }
        newAverages.sort((a, b) => SortAverages(a, b));
        props.savePartChanges(new PartDescriptor({
            ...props.part,
            averageTypes: newAverages
        }));
    }

    const toggleDisplayMeanValues = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.checked) {
            MixPanel.track("reportsEnabledDisplayMeanValues");
        }
        else {
            MixPanel.track("reportsDisabledDisplayMeanValues");
        }

        props.savePartChanges(new PartDescriptor({
            ...props.part,
            displayMeanValues: e.target.checked
        }));
    }

    const toggleDisplayStandardDeviation = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.checked) {
            MixPanel.track("reportsEnabledDisplayStandardDeviation");
        }
        else {
            MixPanel.track("reportsDisabledDisplayStandardDeviation");
        }

        props.savePartChanges(new PartDescriptor({
            ...props.part,
            displayStandardDeviation: e.target.checked
        }));
    }

    return (
        <AverageTypeSelector
            selectedAverages={props.averageTypes}
            toggleAverage={toggleSelectedAverage}
            disabledMessage={getDisabledMessage()}
            metric={props.metric}
            displayMeanValues={props.displayMeanValues}
            displayStandardDeviation={props.displayStandardDeviation}
            toggleStandardDeviation={toggleDisplayStandardDeviation}
            toggleDisplayMeanValues={toggleDisplayMeanValues}
            metrics={metrics}
            supportsStandardDeviation={props.part.partType == PartType.ReportsTable}
        />
    )
}

export default ConfigureReportShowAverage