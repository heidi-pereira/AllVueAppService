import { IActiveBreaks, setActiveBreaks } from "client/state/entitySelectionSlice";
import {
    ComparisonPeriodSelection,
    CrossMeasure,
    CrossMeasureFilterInstance,
    MainQuestionType,
    SavedBreakCombination
} from "../../BrandVueApi";
import { Metric } from "../../metrics/metric";
import { shouldUseFilterValueMappingAsBreak } from "./SurveyVueUtils";
import { shouldPanesRestrictToCurrentPeriod } from "./PagesHelper";
import { QueryStringParamNames } from "./UrlHelper";
import { AppDispatch } from "../../state/store";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { PageHandler } from "../PageHandler";
export type AudienceCategoryGroup = {
    categoryName: string;
    group: SavedBreakCombination[];
}
export type CategorizedAudienceCollection = {[key: string]: AudienceCategoryGroup};

export const UNCATEGORIZED_AUDIENCE_NAME = "Other";

export function groupAudiencesByCategory(audienceOptions: SavedBreakCombination[]): CategorizedAudienceCollection {
    const result = audienceOptions.reduce((categoryGroups: CategorizedAudienceCollection, audience: SavedBreakCombination) => {
        const categoryName = audience.category ?? UNCATEGORIZED_AUDIENCE_NAME;
        if (!categoryGroups[categoryName]) {
            const audienceGroup: AudienceCategoryGroup = {
                categoryName: categoryName,
                group: []
            };
            categoryGroups[categoryName] = audienceGroup
        }
        categoryGroups[categoryName].group.push(audience);
        return categoryGroups;
    }, {});
    Object.values(result).forEach(category => category.group.sort((a,b) => a.name.localeCompare(b.name)));
    return result;
}

export function setBreaksAndPeriod(breaks: IActiveBreaks, setQueryParameter: (name: string, value: any) => void, curatedFilters: CuratedFilters, pageHandler: PageHandler, dispatch: AppDispatch) {
    if (shouldPanesRestrictToCurrentPeriod(breaks, pageHandler.getPanesToRender())) {
        setQueryParameter(QueryStringParamNames.period, ComparisonPeriodSelection.CurrentPeriodOnly);
        curatedFilters.comparisonPeriodSelection = ComparisonPeriodSelection.CurrentPeriodOnly;
    }
    dispatch(setActiveBreaks(breaks));
}

export function filterAudiences(audiences: SavedBreakCombination[], searchText: string) {
    const lowered = searchText.toLowerCase();
    return audiences.filter(a => a.name.toLowerCase().includes(lowered) ||
        (a.category && a.category.toLowerCase().includes(lowered)));
}

export const getActiveAudienceBreaks = (activeBreaks: IActiveBreaks | undefined, 
        savedBreaks: SavedBreakCombination[], 
        validMetrics: Metric[],
        questionTypeLookup: { [key: string]: MainQuestionType }) => {
    if (activeBreaks?.audienceId && (activeBreaks.selectedInstanceOrMappingIds?.length ?? 0) > 0) {
        const selectedAudience = savedBreaks.find(b => b.id === activeBreaks.audienceId);
        const metric = validMetrics.find(m => m.name === selectedAudience?.breaks[0]?.measureName);
        if (selectedAudience && metric) {
            return getCrossMeasureForAudience(selectedAudience, activeBreaks.selectedInstanceOrMappingIds, activeBreaks.multipleChoiceByValue,
                metric, questionTypeLookup);
        }
    }
}

export function getCrossMeasureForAudience(
    audience: SavedBreakCombination | undefined,
    audienceInstances: number[] | undefined,
    audienceMultipleChoiceByValue: boolean | undefined,
    metric: Metric | undefined,
    questionTypeLookup: { [key: string]: MainQuestionType; })
{
    if (audience) {
        const crossMeasure = new CrossMeasure({
            ...audience.breaks[0],
            multipleChoiceByValue: audienceMultipleChoiceByValue ?? false
        });

        if (audienceInstances && metric) {
            const isBasedOnSingleChoice = questionTypeLookup[metric.name] == MainQuestionType.SingleChoice;
            if (shouldUseFilterValueMappingAsBreak(metric, audienceMultipleChoiceByValue ?? false, isBasedOnSingleChoice)) {
                crossMeasure.filterInstances = audienceInstances.map(index => new CrossMeasureFilterInstance({
                    filterValueMappingName: metric.filterValueMapping[index].fullText,
                    instanceId: -1,
                }));
            } else {
                crossMeasure.filterInstances = audienceInstances.map(id => new CrossMeasureFilterInstance({
                    filterValueMappingName: "",
                    instanceId: id
                }));
            }
        }

        return crossMeasure;
    }
}

export function getActiveBreaksFromSelection(audience: SavedBreakCombination | undefined, crossMeasure: CrossMeasure | undefined, metric: Metric | undefined,
                                             questionTypeLookup: { [key: string]: MainQuestionType; }) : IActiveBreaks
{
    let instances: number[] | undefined = undefined;
    if (metric && crossMeasure?.filterInstances && crossMeasure.filterInstances.length > 0) {
        const isBasedOnSingleChoice = questionTypeLookup[metric!.name] == MainQuestionType.SingleChoice;
        if (shouldUseFilterValueMappingAsBreak(metric, crossMeasure.multipleChoiceByValue, isBasedOnSingleChoice)) {
            instances = crossMeasure.filterInstances.map(i => metric.filterValueMapping.findIndex(mapping => mapping.fullText === i.filterValueMappingName));
        } else {
            instances = crossMeasure.filterInstances.map(i => i.instanceId);
        }
    }
        
    return {
        audienceId: audience?.id,
        selectedInstanceOrMappingIds: instances,
        multipleChoiceByValue: crossMeasure?.multipleChoiceByValue ?? undefined,
    }
}

export function isAudienceActive(activeBreaks: IActiveBreaks) {
    return ((activeBreaks.audienceId ?? 0) > 0) &&
        ((activeBreaks.selectedInstanceOrMappingIds?.length ?? 0) > 0);
}