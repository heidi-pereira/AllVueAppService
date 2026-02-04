import React from "react";
import * as BrandVueApi from "../../BrandVueApi";
import { FilterOperator, SigConfidenceLevel } from "../../BrandVueApi";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import {Link, useLocation} from 'react-router-dom';
import * as PageHandler from "../PageHandler";
import ScorecardNextSteps, { willRenderNextStep } from "./ScorecardNextSteps";
import ScorecardKey from "./ScorecardKey";
import { ChartFooterInformation } from "../visualisations/ChartFooterInformation";
import { DateFormattingHelper } from "../../helpers/DateFormattingHelper";
import { EntitySet } from "../../entity/EntitySet";
import DataSortOrder = BrandVueApi.DataSortOrder;
import Tooltip from "../Tooltip";
import { getUrlForMetricOrPageDisplayName } from "../helpers/PagesHelper";
import { getSignificanceMeaning, SignificanceMeaning } from "../../metrics/metricHelper";
import { EntityInstance } from "../../entity/EntityInstance";
import {useEffect, useState} from "react";
import {toast} from "react-hot-toast";
import { useReadVueQueryParams } from "../helpers/UrlHelper";
import { useAppSelector } from "client/state/store";
import { selectSubsetId } from "client/state/subsetSlice";

import {selectTimeSelection} from "../../state/timeSelectionStateSelectors";

interface IScorecardVsKeyCompetitorsProps {
    title: string;
    height: number;
    entitySet: EntitySet;
    curatedFilters: CuratedFilters;
    metrics: Metric[];
    nextSteps: string;
    pageHandler: PageHandler.PageHandler;
}

const ScorecardVsKeyCompetitors =(props: IScorecardVsKeyCompetitorsProps) => {
    const [results, setResults] = useState<BrandVueApi.ScorecardVsKeyCompetitorsResults>(new BrandVueApi.ScorecardVsKeyCompetitorsResults);
    const [isMobile, setIsMobile] = useState<boolean>(false);
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();
    const subsetId = useAppSelector(selectSubsetId);
    const timeSelection = useAppSelector(selectTimeSelection);
    
    useEffect(() => {
        if (timeSelection.scorecardAverage) {
            const sigDiffOptions = new BrandVueApi.SigDiffOptions({
                        highlightSignificance: false,
                        sigConfidenceLevel: SigConfidenceLevel.NinetyFive,
                        displaySignificanceDifferences: BrandVueApi.DisplaySignificanceDifferences.None,
                        significanceType: BrandVueApi.CrosstabSignificanceType.CompareToTotal, 
                    });
            BrandVueApi.Factory.DataClient((err) => toast.error(err))
                .getScorecardVsKeyCompetitorsResults(
                new BrandVueApi.CuratedResultsModel({
                    entityInstanceIds: props.entitySet.getInstances().getAll().map(b => b.id),
                    measureName: props.metrics.map(m => m.name),
                    subsetId: subsetId,
                    period: new BrandVueApi.Period({
                        average: timeSelection.scorecardAverage.averageId,
                        comparisonDates: props.curatedFilters.comparisonDates(true, timeSelection)
                    }),
                    demographicFilter: props.curatedFilters.demographicFilter,
                    activeBrandId: props.entitySet.mainInstance!.id,
                    filterModel: new BrandVueApi.CompositeFilterModel({
                        filterOperator: FilterOperator.And,
                        filters: props.curatedFilters.measureFilters,
                        compositeFilters: []
                    }),
                    ordering: [],
                    orderingDirection: DataSortOrder.Ascending,
                    additionalMeasureFilters: [],
                    includeSignificance: sigDiffOptions.highlightSignificance,
                    sigConfidenceLevel: sigDiffOptions.sigConfidenceLevel,
                    sigDiffOptions,
                })
            ).then(results => {
                setResults(results)
            });
        }
    }, [
        JSON.stringify(props.entitySet),
        props.metrics,
        props.height,
        props.curatedFilters,
    ])
    
    useEffect(()=>{
        const screenWidth = window.innerWidth;
        setIsMobile(screenWidth < 700);
    }, [window.innerWidth])
    
    if (!timeSelection.scorecardAverage) {
        return <>No valid scorecard average</>
    }
    const focusInstance = props.entitySet.mainInstance!;
    const getMetricByName = (name: string): Metric => props.metrics.find(m => m.name === name) || props.metrics[0];
    const renderResult = (r: BrandVueApi.ScorecardVsKeyCompetitorsMetricEntityResult, metricName: string) => {
        const metric = getMetricByName(metricName);
        const significanceMeaning = getSignificanceMeaning(r.current.significance!, metric.downIsGood);
        const delta = r.current.weightedResult - r.previous.weightedResult;
        const dateFrom = DateFormattingHelper.formatDatePoint(r.previous.date, timeSelection.scorecardAverage);
        const dateTo = DateFormattingHelper.formatDatePoint(r.current.date, timeSelection.scorecardAverage);
        const cellTooltip: React.ReactNode = (
            <div className="brandvue-tooltip">
                <div className="tooltip-header">{metricName} - {dateTo}</div>
                <div className="tooltip-label">{r.entityInstance.name}</div>
                <div className="tooltip-value">{metric.fmt(r.current.weightedResult)}</div>
                <div className="tooltip-label">N</div>
                <div className="tooltip-value">{r.current.unweightedSampleSize}</div>
            </div>
        );

        const changeTooltip: React.ReactNode = (
            <div className="brandvue-tooltip">
                <div className="tooltip-header">Change in {metricName}</div>
                <div className="tooltip-label">From</div><div className="tooltip-value">{dateFrom}</div>
                <div className="tooltip-label">To</div><div className="tooltip-value">{dateTo}</div>
                <div className="tooltip-label">Change</div><div className="tooltip-value">{metric.deltaFmt(delta)}</div>
            </div>
        );

        return (
            <td key={metricName + "_delta" + r.entityInstance.id}>
                <div className="value-cell">
                    <Tooltip placement="top" title={changeTooltip}>
                        <div className={(significanceMeaning === SignificanceMeaning.Good ? "sigPositive" : significanceMeaning === SignificanceMeaning.Bad ? "sigNegative" : "sigNone")}>{metric.deltaFmt(delta || Number.NaN)}</div>
                    </Tooltip>
                    <Tooltip placement="top" title={cellTooltip}>
                        <div className="text-end">{metric.fmt(r.current.weightedResult)}</div>
                    </Tooltip>
                </div>
            </td>
        );
    };

    if (results.metricResults.length === 0) {
        return null;
    }

    const getScorecardFooter = () => {
        return <>
            <div className="scorecardFooter mt-4">
                {willRenderNextStep(props.nextSteps) &&
                <ScorecardNextSteps nextSteps={props.nextSteps}/>
                }
                <ScorecardKey mainInstance={focusInstance} restrictor={["siginc", "sigdrop"]}
                    metrics={props.metrics} />
            </div>
            <ChartFooterInformation sampleSizeMeta={results.sampleSizeMetadata}
                                    activeBrand={focusInstance} metrics={props.metrics}
                                    average={timeSelection.scorecardAverage}/>
        </>;
    };

    const mobileColumnTitle = (entityInstance: BrandVueApi.EntityInstance, activeBrand: EntityInstance) => {
        const isActiveBrand = entityInstance.id === activeBrand.id;
            const instanceColor = props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(entityInstance));

        return <td className={`brand-name ${isActiveBrand ? "focus-brand" : ""}`}>
                <div className="brand-color-circle" style={{ background: instanceColor }}></div>
                    {entityInstance.name}
                </td>
    }

        const desktopColumnTitle = (entityInstance: BrandVueApi.EntityInstance, activeBrand: EntityInstance) => {
        const isActiveBrand = entityInstance.id === activeBrand.id;
            const instanceColor = props.entitySet.getInstanceColor(EntityInstance.convertInstanceFromApi(entityInstance));

        return <th key={entityInstance.id}>
            <div className={`brand-name ${isActiveBrand ? "focus-brand" : ""}`}>
                    <div className="brand-color-circle" style={{ background: instanceColor }}></div>
                {entityInstance.name}
            </div>
            </th>
    }


    const singlePageExportable = props.entitySet.getInstances().getAll().length <= 15;
    const getDesktopContent = () => {
        return (
            <div className={`col scorecardControl scorecardControl--vspeers pl-0${singlePageExportable ? " single-page-exportable" : ""}`}>
                <div className="vs-peers-table">
                    <div className="activeTable">
                        <table className="table">
                            <thead>
                            <tr>
                                    <th />
                                    {desktopColumnTitle(results.metricResults[0].activeEntityResult.entityInstance, focusInstance)}
                            </tr>
                            </thead>
                            <tbody>
                            {results.metricResults.map((m) =>
                                <tr key={m.metricName}>
                                    <td>
                                        <Link to={{
                                            pathname: getUrlForMetricOrPageDisplayName(m.metricName, location, readVueQueryParams, { ignoreQuery: true }),
                                            search: location.search
                                        }}>{m.metricName}</Link>
                                    </td>
                                    {renderResult(m.activeEntityResult, m.metricName)}
                                </tr>
                            )}
                            </tbody>
                        </table>
                    </div>
                    <div className="peerTable">
                        <table className="table">
                            <thead>
                            <tr>
                                    {results.metricResults[0].keyCompetitorResults.map(p => desktopColumnTitle(p.entityInstance, focusInstance)
                                )}
                            </tr>
                            </thead>
                            <tbody>
                            {results.metricResults.map((m) =>
                                <tr key={m.metricName}>{m.keyCompetitorResults.map(p => renderResult(p, m.metricName))}</tr>
                            )}
                            </tbody>
                        </table>
                    </div>
                </div>
                {getScorecardFooter()}
            </div>
        );
    };

    const getMobileContent = () => {
        return (
            <div className="scorecardControl scorecardControl--vspeers">
                {
                    results.metricResults.map((m) => {
                        return (
                            <table className="table mobile-table" key={m.metricName}>
                                <thead>
                                    <tr>
                                        <th colSpan={2}>
                                            <Link to={{
                                                pathname: getUrlForMetricOrPageDisplayName(m.metricName, location, readVueQueryParams),
                                                search: location.search
                                            }}>
                                                {m.metricName}
                                            </Link>
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        {mobileColumnTitle(results.metricResults[0].activeEntityResult.entityInstance, focusInstance)}
                                        {renderResult(m.activeEntityResult, m.metricName)}
                                    </tr>
                                    {
                                        m.keyCompetitorResults.map(p =>
                                            <tr key={p.entityInstance.id}>
                                               {mobileColumnTitle(p.entityInstance, focusInstance)}
                                               {renderResult(p, m.metricName)}
                                           </tr>
                                        )
                                    }
                                </tbody>
                            </table>
                        );
                    })
                }
                {getScorecardFooter()}
            </div>
        );
    };

    if (isMobile) {
        return getMobileContent();
    } else {
        return getDesktopContent();
    }
}
export default ScorecardVsKeyCompetitors
