import {
    CrossbreakCompetitionResults,
    CrossMeasure,
    CuratedResultsModelWithCrossbreaks,
    Factory,
    MainQuestionType
} from "../../BrandVueApi";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { EntityInstance } from "../../entity/EntityInstance";
import { EntitySet } from "../../entity/EntitySet";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { ViewHelper } from "../visualisations/ViewHelper";
import { getAvailableCrossMeasureFilterInstances } from "./SurveyVueUtils";
import { AnalysisSubPageEntityType, AnalysisSubPageRequestType } from "./AnalysisHelper";
import { NoDataError } from "../../NoDataError";
import { ITimeSelectionOptions } from "../../state/ITimeSelectionOptions";

export interface IBrandAnalysisRequestProps {
    brand: EntityInstance;
    primaryMetric: Metric;
    activeEntitySet: EntitySet;
    curatedFilters: CuratedFilters,
    entityConfiguration: IEntityConfiguration,
    pageMetricConfiguration: IBrandAnalysisPageMetricConfiguration,
}

export interface IBrandAnalysisPageMetricConfiguration {
    metrics: {
        key: AnalysisSubPageEntityType,
        metricName: string,
        requestType: AnalysisSubPageRequestType,
        includePrimaryMetricFilter?: boolean,
    }[]
    pastTenseVerb: string,
    buttonLink: string,
    filterMetric: {
        name: string,
        value: string
    },
    shortUserDescription: string,
    longUserDescription: string,
    averageId: string
}

export enum BrandAnalysisRequestReturnType {
    Succeeded,
    Failed
}

export function makeBrandAnalysisCrossbreakRequests(props: IBrandAnalysisRequestProps,
    questionTypeLookup: { [key: string]: MainQuestionType; },
    categories: { key: AnalysisSubPageEntityType, metric: Metric }[],
    subsetId: string,
    timeSelection: ITimeSelectionOptions,
    then: (r: { key: AnalysisSubPageEntityType, results: CrossbreakCompetitionResults | null, returnType: BrandAnalysisRequestReturnType }) => void) {
    const client = Factory.DataClientWithNoHandler(throwErr => throwErr());

    categories.map(async m => {
        const requestModel = ViewHelper.createCuratedRequestModel(
            [props.brand.id],
            [props.primaryMetric],
            props.curatedFilters,
            props.brand.id,
            {},
            subsetId,
            timeSelection
        );

        const isBasedOnSingleChoice = questionTypeLookup[m.metric.name] == MainQuestionType.SingleChoice;
        const crossMeasure = new CrossMeasure({
            measureName: m.metric.name,
            filterInstances: getAvailableCrossMeasureFilterInstances(m.metric, props.entityConfiguration, false, isBasedOnSingleChoice),
            childMeasures: [],
            multipleChoiceByValue: false,
        });

        const requestModelWithBreaks = new CuratedResultsModelWithCrossbreaks({
            curatedResultsModel: requestModel,
            breaks: [crossMeasure]
        });
        return client.getGroupedCrossbreakCompetitionResults(requestModelWithBreaks)
            .then(r => then(
                { key: m.key, results: r.groupedBreakResults[0].breakResults, returnType: BrandAnalysisRequestReturnType.Succeeded }))
            .catch((e: any) => {
                if (e.typeDiscriminator === NoDataError.typeDiscriminator) {
                    then({key: m.key, results: null, returnType: BrandAnalysisRequestReturnType.Succeeded})
                } else {
                    console.error(e);
                    then({key: m.key, results: null, returnType: BrandAnalysisRequestReturnType.Failed})
                }
            })
    });
}
