import * as BrandVueApi from "../../BrandVueApi";
import { DataSubsetManager } from "../../DataSubsetManager";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { MetricSet } from "../../metrics/metricSet";
import { ViewHelper } from "../visualisations/ViewHelper";
import { EntitySet } from "../../entity/EntitySet";
import { FilterInstance } from "../../entity/FilterInstance";
import PaneDescriptor = BrandVueApi.PaneDescriptor;
import PartDescriptor = BrandVueApi.PartDescriptor;
import RequestMeasureForEntity = BrandVueApi.RequestMeasureForEntity;
import AverageTotalRequestModel = BrandVueApi.AverageTotalRequestModel;
import {getViewTypeEnum, ViewTypeEnum} from "../helpers/ViewTypeHelper";
import { isMetricComparisonPage } from "../helpers/PagesHelper";
import { saveFile } from "../../helpers/FileOperations";
import { CrossMeasure, PageDescriptor, MeasureFilterRequestModel } from "../../BrandVueApi";
import { viewBase } from "../../core/viewBase";
import Comparison from "../visualisations/MetricComparison/Comparison";
import { ExcelDownloadBase, IExcelBaseDownloadProps } from "./ExcelDownloadBase";
import { EntityInstance } from "../../entity/EntityInstance";
import { useState } from "react";
import React from "react";
import { PageHandler } from "../PageHandler";
import { useActiveBrandSetWithDefault } from "../../state/entitySelectionHooks";
import { useAppSelector } from '../../state/store';
import { selectSubsetId } from "../../state/subsetSlice";

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

type MetricBrandModelData = {
    metrics: Metric[];
    leadVisualization: string;
    entitySet: EntitySet | null;
    entityInstanceIds: number[];
    activeBrandId: number;
    curatedFilters: CuratedFilters;
    continuousPeriod: boolean;
    measuresForEntity: RequestMeasureForEntity[];
    averageRequests: AverageTotalRequestModel[] | null;
    filterInstance?: FilterInstance;
    breaks?: CrossMeasure;
};

interface IExcelDownloadProps extends IExcelBaseDownloadProps  {
    coreViewType: number;
    activeDashPage: PageDescriptor;
    comparisons: Comparison[];
    metrics: MetricSet;
    averageRequests: AverageTotalRequestModel[] | null
    entitySet: EntitySet;
    filterInstance?: FilterInstance;
    breaks?: CrossMeasure;
    pageHandler: PageHandler;
    activeView: viewBase;
}

function parseFilters(filters: string[]): MeasureFilterRequestModel[] {
    // Assume filter takes form: {label}:{measure}=[!]{value,value,value}
    return filters
        .map(f => f.split(':')[1])
        .map(f => f.split('='))
        .map(f => {

            const invert = f[1].startsWith('!');
            if (invert) {
                f[1] = f[1].substring(1);
            }
            return new MeasureFilterRequestModel({
                entityInstances: { "brand": [EntityInstance.AllInstancesId]},
                measureName: f[0],
                values: f[1].split(',').map(v => +v),
                invert: invert,
                treatPrimaryValuesAsRange: false
            });
        });
}

const ExcelDownload: React.FC<IExcelDownloadProps> = (props) => {
    const [loading, setLoading] = useState(false);
    const excelSheetName = "export.xlsx";
    const requestAverageTotals = true;
    const fallbackSelectedBrand = useActiveBrandSetWithDefault();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    const getMetricAndLeadVisualization = (panes: PaneDescriptor[], coreViewType: number, metricList: MetricSet): { metrics: Metric[]; leadVisualization: string } => {
        const metrics: Metric[] = [];
        let leadVisualization: string = "default";
        if (panes && panes.length > 0) {
            panes.forEach((pane: PaneDescriptor) => {
                if (pane.view === coreViewType) {
                    pane.parts.forEach((part: PartDescriptor) => {
                        if (part.spec1 && part.partType !== "Text") {
                            leadVisualization = part.partType;
                            part.spec1.split("|").forEach((metricName: string) => {
                                const m = metricList.getMetrics(metricName)[0];
                                if (m) {
                                    metrics.push(m);
                                } else {
                                    console.error(
                                        `Metric '${metricName}' from part.spec1 '${part.spec1
                                        }' not found for pane with id '${pane.id}'`);
                                }
                            });
                        }
                    });
                }
            });
        }

        return {
            metrics: metrics,
            leadVisualization: leadVisualization
        };
    };

    const prepareMetricAndEntityData = (
        activeView: viewBase,
        coreViewType: number,
        comparisons: Comparison[],
        activeDashPage: PageDescriptor,
        metricSet: MetricSet,
        averageRequests: AverageTotalRequestModel[],
        activeEntityId: number | null
    ): MetricBrandModelData | null => {
        const continousPeriod = coreViewType === ViewTypeEnum.OverTime || coreViewType === ViewTypeEnum.ProfileOverTime;

        if (isMetricComparisonPage(activeDashPage)) {
            if (comparisons.length < 1) {
                return null;
            }
            const specificBrandsForMeasures = comparisons.map(c => new RequestMeasureForEntity({ measureName: c.metric!.name, entityInstanceIds: [c.brand.id] }));
            return {
                metrics: comparisons.map(c => c.metric!),
                leadVisualization: "",
                entitySet: null,
                entityInstanceIds: [],
                activeBrandId: activeEntityId ?? fallbackSelectedBrand?.getMainInstance().id!,
                curatedFilters: activeView.curatedFilters,
                continuousPeriod: continousPeriod,
                measuresForEntity: specificBrandsForMeasures,
                averageRequests: null,
                filterInstance: props.filterInstance,
                breaks: props.breaks
            };
        } else {
            const onlyIncludeActiveBrand = activeView.activeMetrics.some(m => m.calcType === BrandVueApi.CalculationType.Text);
            const entityInstanceIds = onlyIncludeActiveBrand ? [activeEntityId!] : props.entitySet.getInstances().getAll().map(i => i.id);
            const { metrics, leadVisualization } = getMetricAndLeadVisualization(activeDashPage.panes, coreViewType, metricSet);
            return {
                metrics: metrics,
                leadVisualization: leadVisualization,
                entitySet: props.entitySet,
                entityInstanceIds: entityInstanceIds,
                activeBrandId: activeEntityId!,
                curatedFilters: activeView.curatedFilters,
                continuousPeriod: continousPeriod,
                measuresForEntity: [],
                averageRequests: averageRequests,
                filterInstance: props.filterInstance,
                breaks: props.breaks
            };
        }
    };

    const handleExcelDownloadClick = (data: MetricBrandModelData) => {
        const map = data.curatedFilters.filterDescriptions;
        const fds = data.metrics.some(m => !m.isMetricFilterable()) ? [] : Object.keys(map).map(k => map[k].name + ": " + map[k].filter);
        let helpText = props.activeDashPage.pageSubsetConfiguration.find(p => p.subset == props.pageHandler.session.selectedSubsetId)?.helpText ?? props.activeDashPage.helpText;

        if (data.metrics.length > 0 && data.metrics[0].entityCombination.length > 1) {
            if (!data.entitySet) {
                throw new Error("No entityset provided for multi entity metric");
            }

            if (data.leadVisualization === "SplitMetricChart") {
                const multiEntityRequestModel = ViewHelper.createMultiEntityRequestModel({
                    curatedFilters: data.curatedFilters,
                    metric: data.metrics[0],
                    splitBySet: data.entitySet,
                    filterInstances: data.filterInstance ? [data.filterInstance] : [],
                    continuousPeriod: false,
                    focusEntityId: data.activeBrandId,
                    subsetId: subsetId,
                }, timeSelection);

                multiEntityRequestModel.additionalMeasureFilters = parseFilters(props.activeDashPage.panes[0].parts[0].filters);
                const model = new BrandVueApi.ExcelExportSplitMetricModel({
                    viewType: getViewTypeEnum(props.coreViewType),
                    multiEntityRequestModel: multiEntityRequestModel,
                    filterDescriptions: fds,
                    name: props.activeDashPage.name,
                    leadVisualization: data.leadVisualization,
                    measuresForEntity: data.measuresForEntity,
                    averageRequests: data.averageRequests == null || !requestAverageTotals ? [] : data.averageRequests,
                    breaks: data.breaks,
                    helpText: helpText ?? "",
                    measureNames: props.activeDashPage.panes[0].parts[0].filters.map(f => f.split(':')[0])
                });
                return BrandVueApi.Factory.DataClient(throwErr => setLoading(throwErr)).exportSplitMetricData(model);
            } else {
                const multiEntityRequestModel = ViewHelper.createMultiEntityRequestModel({
                    curatedFilters: data.curatedFilters,
                    metric: data.metrics[0],
                    splitBySet: data.entitySet,
                    filterInstances: data.filterInstance ? [data.filterInstance] : [],
                    continuousPeriod: data.continuousPeriod,
                    focusEntityId: data.activeBrandId,
                    subsetId: subsetId,
                }, timeSelection);

                const model = new BrandVueApi.ExcelExportMultipleEntitiesModel({
                    viewType: getViewTypeEnum(props.coreViewType),
                    multiEntityRequestModel: multiEntityRequestModel,
                    filterDescriptions: fds,
                    name: props.activeDashPage.name,
                    leadVisualization: data.leadVisualization,
                    measuresForEntity: data.measuresForEntity,
                    averageRequests: data.averageRequests == null || !requestAverageTotals ? [] : data.averageRequests,
                    breaks: data.breaks,
                    helpText: helpText ?? ""
                });
                return BrandVueApi.Factory.DataClient(throwErr => setLoading(throwErr)).exportMultiEntityData(model);
            }
        } else {
            const curatedResultsModel = ViewHelper.createCuratedRequestModel(
                data.entityInstanceIds,
                DataSubsetManager.filterMetricByCurrentSubset(data.metrics),
                data.curatedFilters,
                data.activeBrandId,
                {
                    continuousPeriod: data.continuousPeriod,
                    ordering: [],
                    orderingDirection: BrandVueApi.DataSortOrder.Ascending,
                    useScorecardDates: props.pageHandler.hasScorecardFilters() || false,
                },
                subsetId,
                timeSelection
            );

            const model = new BrandVueApi.ExcelExportModel({
                viewType: getViewTypeEnum(props.coreViewType),
                curatedResultsModel: curatedResultsModel,
                filterDescriptions: fds,
                name: props.activeDashPage.name,
                leadVisualization: data.leadVisualization,
                measuresForEntity: data.measuresForEntity,
                averageRequests: data.averageRequests == null || !requestAverageTotals ? [] : data.averageRequests,
                breaks: data.breaks,
                helpText: helpText ?? ""
            });

            return BrandVueApi.Factory.DataClient(throwErr => setLoading(throwErr)).exportData(model);
        }
    };

    const handleClick = () => {
        // Call base event tracking
        props.googleTagManager.addEvent("downloadExcel", props.pageHandler);

        const modelData = prepareMetricAndEntityData(
            props.activeView,
            props.coreViewType,
            props.comparisons,
            props.activeDashPage,
            props.metrics,
            props.averageRequests ?? [],
            props.entitySet?.getMainInstance().id ?? null
        );

        if (modelData) {
            setLoading(true);
            handleExcelDownloadClick(modelData)
                .then(r => {
                    saveFile(r, excelSheetName);
                })
                .finally(() => {
                    setLoading(false);
                });
        }
    };

    return (
        <ExcelDownloadBase
            {...props}
            loading={loading}
            onClick={handleClick}
        />
    );
};

export default ExcelDownload;
