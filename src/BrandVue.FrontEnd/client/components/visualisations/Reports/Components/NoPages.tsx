import React from 'react';
import * as BrandVueApi from "../../../../BrandVueApi";
import AddMetricsModal from '../Modals/AddMetricsModal';
import { Metric } from '../../../../metrics/metric';

interface INoPages {
    metricsForReports: Metric[];
    questionTypeLookup: {[key: string]: BrandVueApi.MainQuestionType};
    reportType: BrandVueApi.ReportType;
    chartsPaneId: string | undefined;
    addPartsToReport(metrics: Metric[]): void;
    getPrimaryButtonText(metrics: Metric[]): string;
    modalHeaderText: string;
}

const NoPages = (props: INoPages) => {
    const [isAddChartsModalVisible, setIsAddChartsModalVisible] = React.useState<boolean>(false);

    return (
        <div className="no-pages">
            <AddMetricsModal isOpen={isAddChartsModalVisible}
                metrics={props.metricsForReports}
                getPrimaryButtonText={props.getPrimaryButtonText}
                modalHeaderText={props.modalHeaderText}
                onMetricsSubmitted={props.addPartsToReport}
                setAddChartModalVisibility={setIsAddChartsModalVisible}
            />
            <div className="text">
                Choose questions / variables to add to the report
            </div>
            <button className="add-chart-toggle hollow-button" onClick={() => setIsAddChartsModalVisible(true)}>
                <i className="material-symbols-outlined">add</i>
                <div>{props.reportType == BrandVueApi.ReportType.Chart ? "Add charts" : "Add tables"}</div>
            </button>
        </div>
    )
}

export default NoPages;