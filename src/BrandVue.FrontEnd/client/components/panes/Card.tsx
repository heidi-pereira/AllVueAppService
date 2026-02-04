import React from "react";
import { useMemo } from 'react';
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { IAverageDescriptor, CategorySortKey, ComparisonPeriodSelection} from "../../BrandVueApi";
import { PageHandler } from "../PageHandler";
import MetricChangeOnPeriod from "../visualisations/Cards/MetricChangeOnPeriod";
import PageLink from "../visualisations/PageLink";
import TwitterWidget from "../visualisations/Cards/TwitterWidget";
import OpenAssociationsCard from "../visualisations/OpenAssociationsCard";
import { getMetricOrThrow } from "../../metrics/metricHelper";
import ScatterPlotCard from "../visualisations/Cards/ScatterPlotCard";
import FunnelCard from "../visualisations/Cards/FunnelCard";
import SimplifiedScorecard from "../visualisations/Cards/SimplifiedScorecard";
import RankingScorecard from "../visualisations/BrandPerformance/RankingScorecard";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { EntitySet } from "../../entity/EntitySet";
import { PartType } from "./PartType";
import { getEndOfLastMonthWithData } from "../helpers/DateHelper";
import RankingOvertimeCard from "../visualisations/Cards/RankingOvertimeCard";
import moment from 'moment';
import { convertToUrl, getCardLinkByMetricOrPageName, getCuratedFiltersForAverageId } from "../helpers/PagesHelper";
import { selectMonthlyAverage } from "../helpers/AveragesHelper";
import { IPart } from "../../parts/IPart";
import { useLocation } from "react-router-dom";
import { useReadVueQueryParams } from "../helpers/UrlHelper";

export interface ICardProps {
    selectedSubsetId: string;
    partConfig: IPart;
    metrics: Metric[];
    averages: IAverageDescriptor[];
    pageHandler: PageHandler;
    entitySet: EntitySet;
    entityConfiguration: IEntityConfiguration;
    dateOfLastDataPoint: Date;
    getAverageById(averageId: string): IAverageDescriptor;
    dateOfFirstDataPoint: Date;
    baseVariableId1: number;
    baseVariableId2: number;
    title: string;
    paneIndex: number;
    updateBaseVariableNames: (firstName: string | undefined, secondName: string | undefined) => void;
}

const Card = (props: ICardProps) => {
    const {
        selectedSubsetId,
        partConfig,
        metrics,
        averages,
        entitySet,
        entityConfiguration,
        dateOfLastDataPoint,
        getAverageById,
        dateOfFirstDataPoint,
    } = props;
    const location = useLocation();
    const readVueQueryParams = useReadVueQueryParams();
    const lastMonthFilters = useMemo(() => CuratedFilters.createWithOptions({
        endDate: getEndOfLastMonthWithData(dateOfLastDataPoint),
        average: selectMonthlyAverage(averages),
        comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly
    }, entityConfiguration), []);

    const last2MonthsFilters = useMemo(() => CuratedFilters.createWithOptions({
        endDate: getEndOfLastMonthWithData(dateOfLastDataPoint),
        average: selectMonthlyAverage(averages),
        comparisonPeriodSelection: ComparisonPeriodSelection.CurrentAndPreviousPeriod
    }, entityConfiguration), []);

    const last2MonthsFiltersKeyCompetitors = useMemo(() => CuratedFilters.createWithOptions({
        endDate: getEndOfLastMonthWithData(dateOfLastDataPoint),
        average: selectMonthlyAverage(averages),
        comparisonPeriodSelection: ComparisonPeriodSelection.CurrentAndPreviousPeriod,
    }, entityConfiguration), []);

    const last6MonthsContinuousFilters = useMemo(() => CuratedFilters.createWithOptions({
        startDate: moment.utc(getEndOfLastMonthWithData(dateOfLastDataPoint)).subtract(6, "months").endOf("month").toDate(),
        endDate: getEndOfLastMonthWithData(dateOfLastDataPoint),
        average: selectMonthlyAverage(averages),
        comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly,
    }, entityConfiguration), []);


    //Assume all metrics have the same entity combination
    const getEntityType = () => {
        if (!metrics[0]?.entityCombination?.length) {
            return undefined;
        }

        if (metrics[0].entityCombination.length === 1) {
            return metrics[0].entityCombination[0];
        }

        const splitByName = partConfig.descriptor.defaultSplitBy ? partConfig.descriptor.defaultSplitBy : entityConfiguration.defaultEntityType.identifier;
        return metrics[0].entityCombination.find(e => e.identifier === splitByName)!;
    }

    const getEntitySet = () => {
        var entityType = getEntityType();

        if (!entityType) {
            return undefined;
        }

        //If we're passed an entity set of the right type, use that. Otherwise get default set of the metric's type
        if (entitySet?.type?.identifier === entityType?.identifier) {
            return entitySet;
        }

        return entityConfiguration.getDefaultEntitySetFor(entityType);
    }
    
    const card = partConfig.getCardComponent(props, location, readVueQueryParams);
    if(card != null)
    {
        return card;
    }
    
    const entitySetToUse = getEntitySet();
    const entityInstance = entitySet?.mainInstance;
    const average = partConfig.descriptor.defaultAverageId ? getAverageById(partConfig.descriptor.defaultAverageId) : null;
    let curatedFilters : CuratedFilters;
    switch (partConfig.descriptor.partType) {
        case PartType.MetricChangeOnPeriod:
            curatedFilters = getCuratedFiltersForAverageId(average, last2MonthsFiltersKeyCompetitors, dateOfLastDataPoint, props.entityConfiguration);
            const metricUrl = partConfig.descriptor.spec2 ?
                getCardLinkByMetricOrPageName(partConfig.descriptor.spec2, partConfig, location, readVueQueryParams,convertToUrl(curatedFilters, dateOfFirstDataPoint, dateOfLastDataPoint, props.entityConfiguration)) :
                getCardLinkByMetricOrPageName(metrics[0].name, partConfig, location, readVueQueryParams, convertToUrl(curatedFilters, dateOfFirstDataPoint, dateOfLastDataPoint, props.entityConfiguration));
            return <MetricChangeOnPeriod
                metric={metrics[0]}
                nextPageUrl={metricUrl}
                entitySet={entitySetToUse}
                curatedFilters={curatedFilters}
            />;
        case 'TwitterWidget':
            return <TwitterWidget selectedSubsetId={selectedSubsetId} twitterHandle={partConfig.descriptor.spec2}/>;
        case PartType.PageLink:
            return <PageLink to={getCardLinkByMetricOrPageName(partConfig.descriptor.spec1, partConfig, location, readVueQueryParams)}
                             cssClass={partConfig.descriptor.spec2} text={partConfig.descriptor.spec3}/>;
        case PartType.OpenAssociationsCard:
            curatedFilters = getCuratedFiltersForAverageId(average, lastMonthFilters, dateOfLastDataPoint, props.entityConfiguration);
            const pageUrl = partConfig.descriptor.spec2 ?
                getCardLinkByMetricOrPageName(partConfig.descriptor.spec2, partConfig, location, readVueQueryParams, convertToUrl(curatedFilters, dateOfFirstDataPoint, dateOfLastDataPoint, props.entityConfiguration)) :
                getCardLinkByMetricOrPageName(partConfig.descriptor.spec1, partConfig, location, readVueQueryParams, convertToUrl(curatedFilters, dateOfFirstDataPoint, dateOfLastDataPoint, props.entityConfiguration));
            return <OpenAssociationsCard
                openAssociationMetric={getMetricOrThrow(metrics, partConfig.descriptor.spec1)}
                openAssociationPageUrl={pageUrl}
                activeBrand={entityInstance}
                filters={curatedFilters}
            />;
        case PartType.ScatterPlotCard:
            curatedFilters = getCuratedFiltersForAverageId(average, lastMonthFilters, dateOfLastDataPoint, props.entityConfiguration);
            return <ScatterPlotCard
                linkText={partConfig.descriptor.spec2}
                entitySet={entitySet}
                entityInstance={entityInstance}
                xMetric={metrics[0]}
                yMetric={metrics[1]}
                nextPageUrl={getCardLinkByMetricOrPageName(partConfig.descriptor.spec3, partConfig, location, readVueQueryParams, convertToUrl(curatedFilters, dateOfFirstDataPoint, dateOfLastDataPoint, props.entityConfiguration))}
                xAxisRange={partConfig.descriptor.xAxisRange}
                yAxisRange={partConfig.descriptor.yAxisRange}
                sections={partConfig.descriptor.sections}
                curatedFilters={curatedFilters}
            />;
        case PartType.FunnelCard:
            curatedFilters = getCuratedFiltersForAverageId(average, lastMonthFilters, dateOfLastDataPoint, props.entityConfiguration);
            return <FunnelCard
                metrics={metrics}
                nextPageUrl={getCardLinkByMetricOrPageName(partConfig.descriptor.spec3, partConfig, location, readVueQueryParams, convertToUrl(curatedFilters, dateOfFirstDataPoint, dateOfLastDataPoint, props.entityConfiguration))}
                descriptionText={partConfig.descriptor.helpText}
                entityInstance={entityInstance}
                curatedFilters={curatedFilters}
            />;
        case PartType.SimplifiedScorecard:
            curatedFilters = getCuratedFiltersForAverageId(average, last6MonthsContinuousFilters, dateOfLastDataPoint, props.entityConfiguration);
            return <SimplifiedScorecard
                metrics={metrics}
                nextPageUrl={getCardLinkByMetricOrPageName(partConfig.descriptor.spec3, partConfig, location, readVueQueryParams, curatedFilters)}
                descriptionText={partConfig.descriptor.helpText}
                entityInstance={entityInstance}
                curatedFilters={curatedFilters}
            />;
        case PartType.RankingScorecard:
            curatedFilters = getCuratedFiltersForAverageId(average, last2MonthsFilters, dateOfLastDataPoint, props.entityConfiguration);
            const rankingScorecardUrl = partConfig.descriptor.spec2 ?
                getCardLinkByMetricOrPageName(partConfig.descriptor.spec2, partConfig, location, readVueQueryParams, convertToUrl(curatedFilters, dateOfFirstDataPoint, dateOfLastDataPoint, props.entityConfiguration)) :
                getCardLinkByMetricOrPageName(metrics[0].name, partConfig, location, readVueQueryParams, convertToUrl(curatedFilters, dateOfFirstDataPoint, dateOfLastDataPoint, props.entityConfiguration));

            return <RankingScorecard
                metric={metrics[0]}
                nextPageUrl={rankingScorecardUrl}
                descriptionText={partConfig.descriptor.helpText}
                entityInstance={entityInstance}
                curatedFilters={curatedFilters}
                entityConfiguration={entityConfiguration}
                numberOfScoresToShow={partConfig.descriptor.yAxisRange.max}
            />
        case PartType.RankingOvertimeCard:
            curatedFilters = getCuratedFiltersForAverageId(average, last6MonthsContinuousFilters, dateOfLastDataPoint, props.entityConfiguration);
            return <RankingOvertimeCard
                linkText={partConfig.descriptor.spec2}
                metric={metrics[0]}
                nextPageUrl={getCardLinkByMetricOrPageName(metrics[0].name, partConfig, location, readVueQueryParams, curatedFilters, partConfig.descriptor.spec3 || "ranking")}
                entityInstance={entityInstance}
                entitySet={entitySetToUse}
                curatedFilters={curatedFilters}
            />
        default:
            return null;
    }
}
export default Card;