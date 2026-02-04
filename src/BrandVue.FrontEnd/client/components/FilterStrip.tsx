import { PageHandler } from "./PageHandler";
import ExcelDownload from "./ExcelDownload/ExcelDownload"
import { FilterPopup } from "./filters/FilterPopup";
import React from "react";
import SaveChart from "./SaveChart";
import { EntitySet } from "../entity/EntitySet";
import { FilterInstance } from "../entity/FilterInstance";
import { ViewTypeEnum } from "./helpers/ViewTypeHelper";
import { isMetricComparisonPage, isAudiencePage } from "./helpers/PagesHelper";
import { PaneType } from "./panes/PaneType";
import {
    AverageTotalRequestModel,
    CrossMeasure,
    PageDescriptor
} from "../BrandVueApi";
import { IGoogleTagManager } from "../googleTagManager";
import { viewBase } from "../core/viewBase";
import { filterSet } from "../filter/filterSet";
import { MetricSet } from "../metrics/metricSet";
import { IEntityConfiguration } from "../entity/EntityConfiguration";
import ExcelDownloadCategory from './ExcelDownload/ExcelDownloadCategory';
import { BaseVariableContext } from './visualisations/Variables/BaseVariableContext';
import { ApplicationConfiguration } from '../ApplicationConfiguration';
import { CategoryContext } from "./helpers/CategoryContext";
import { ComparisonContext } from "./helpers/ComparisonContext";

interface IFilterStripProps {
    activeDashPage: PageDescriptor;
    filters: filterSet;
    metrics: MetricSet;
    entityConfiguration: IEntityConfiguration;
    averageRequests: AverageTotalRequestModel[] | null;
    coreViewType: number;
    activeView: viewBase;
    overridingPaneType: string;
    googleTagManager: IGoogleTagManager;
    pageHandler: PageHandler;
    entitySet: EntitySet;
    filterInstance?: FilterInstance;
    breaks?: CrossMeasure;
    showFilterButton: boolean;
    saveImageButtonText?: string;
    applicationConfiguration: ApplicationConfiguration;
}

export default class FilterStrip extends React.Component<IFilterStripProps, { filterDescriptions: { name: string, filter: string }[] }> {
    constructor(props) {
        super(props);
    }

    private shouldShowExcelDownload(): boolean {
        const entityCombination = this.props.activeView.getEntityCombination();
        const viewType = this.props.coreViewType;

        const exportEnabledForView = entityCombination.length <= 1 ||
            viewType === ViewTypeEnum.Competition ||
            viewType === ViewTypeEnum.OverTime;

        return exportEnabledForView &&
            this.props.overridingPaneType !== PaneType.scorecard &&
            this.props.overridingPaneType !== PaneType.iFrame &&
            this.props.overridingPaneType !== PaneType.analysisScorecard &&
            !PaneType.BrandAnalysisPanes.includes(this.props.overridingPaneType);
    }

    private shouldShowSaveChart(): boolean {
        return !isMetricComparisonPage(this.props.activeDashPage) && !isAudiencePage(this.props.activeDashPage) && this.props.overridingPaneType !== PaneType.iFrame;
    }

    private getExcelDownload = () => {
        if (isAudiencePage(this.props.activeDashPage)) {
            return <BaseVariableContext.Consumer>{(b) => (
                <CategoryContext.Consumer>{(c) => (
                    <ExcelDownloadCategory
                        resultCards={c.getCategoryExportResultCards()}
                        baseVariables={b.baseVariables}
                        activeDashPage={this.props.activeDashPage}
                        googleTagManager={this.props.googleTagManager}
                        pageHandler={this.props.pageHandler}
                        activeView={this.props.activeView}
                        applicationConfiguration={this.props.applicationConfiguration}
                        entitySet={this.props.entitySet}
                    />)
                }</CategoryContext.Consumer>)}
            </BaseVariableContext.Consumer>
        }
        return <ComparisonContext.Consumer>
            {(c) => (
                <ExcelDownload
                    metrics={this.props.metrics}
                    activeView={this.props.activeView}
                    coreViewType={this.props.coreViewType}
                    activeDashPage={this.props.activeDashPage}
                    comparisons={c.getComparisons()}
                    averageRequests={this.props.averageRequests}
                    googleTagManager={this.props.googleTagManager}
                    pageHandler={this.props.pageHandler}
                    entitySet={this.props.entitySet}
                    filterInstance={this.props.filterInstance}
                    breaks={this.props.breaks} />)}
        </ComparisonContext.Consumer>
    }

    render() {
        return (
            <div id="filter-strip-controls" className="not-exported">
                {this.props.showFilterButton ?
                    <FilterPopup
                        filters={this.props.filters}
                        activeView={this.props.activeView}
                        metrics={this.props.metrics}
                        entityConfiguration={this.props.entityConfiguration}
                        pageHandler={this.props.pageHandler}
                        googleTagManager={this.props.googleTagManager}
                    /> : null
                }
                {this.shouldShowExcelDownload() ? this.getExcelDownload() : null}
                {this.shouldShowSaveChart() &&
                    <SaveChart activeDashPage={this.props.activeDashPage}
                        googleTagManager={this.props.googleTagManager}
                        coreViewType={this.props.coreViewType}
                        pageHandler={this.props.pageHandler}
                        label={this.props.saveImageButtonText}
                    />
                }
            </div>
        );
    }
}