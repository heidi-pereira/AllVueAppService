import React from "react";
import { BasePart } from "./BasePart";
import { ICardProps } from "../components/panes/Card";
import CategoryComparison from "../components/visualisations/CategoryComparison";
import {
    convertToUrl,
    getCardLinkByMetricOrPageName,
    getCuratedFiltersForAverageId,
    getActivePage
} from "../components/helpers/PagesHelper";
import { ReactElement, useMemo } from "react";
import { CuratedFilters } from "../filter/CuratedFilters";
import { getEndOfLastMonthWithData } from "../components/helpers/DateHelper";
import { selectMonthlyAverageOver12Months } from "../components/helpers/AveragesHelper";
import {ComparisonPeriodSelection} from "../BrandVueApi";
import {Location} from "react-router-dom";
import { IReadVueQueryParams } from "../components/helpers/UrlHelper";

export class CategoryComparisonPart extends BasePart {

    getCardComponent(props: ICardProps, location: Location, readVueQueryParams: IReadVueQueryParams): ReactElement | null {
        const last12MonthsFilters = useMemo(() => CuratedFilters.createWithOptions({
                endDate: getEndOfLastMonthWithData(props.dateOfLastDataPoint),
                average: selectMonthlyAverageOver12Months(props.averages),
                comparisonPeriodSelection: ComparisonPeriodSelection.CurrentPeriodOnly
            }, props.entityConfiguration), []);
        const average = props.partConfig.descriptor.defaultAverageId != null ? props.getAverageById(props.partConfig.descriptor.defaultAverageId) : null;
        return <CategoryComparison
            isDetailedTile={props.partConfig.descriptor.spec3 == "detailed"}
            linkUrl={this.descriptor.spec2 ?
                getCardLinkByMetricOrPageName(this.descriptor.spec2, props.partConfig, location, readVueQueryParams, convertToUrl(last12MonthsFilters, props.dateOfFirstDataPoint, props.dateOfLastDataPoint, props.entityConfiguration)) :
                undefined}
            activeBrand={props.entitySet?.mainInstance!}
            metrics={props.metrics}
            curatedFilters={getCuratedFiltersForAverageId(average, last12MonthsFilters, props.dateOfLastDataPoint, props.entityConfiguration)}
            brandSet={props.entitySet}
            colorName={this.descriptor.colours[0]}
            entityConfiguration={props.entityConfiguration}
            questionText={this.descriptor.helpText}
            baseVariableId1={props.baseVariableId1}
            baseVariableId2={props.baseVariableId2}
            defaultBaseVariableIdentifier={getActivePage().defaultBase}
            title={props.title}
            paneIndex={props.paneIndex}
            updateBaseVariableNames={props.updateBaseVariableNames}
        />;
    }
}