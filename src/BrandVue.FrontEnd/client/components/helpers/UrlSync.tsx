import * as React from "react";
import { useEffect, useRef, useState } from "react";
import { useDispatch } from "react-redux";
import { useLocation, useSearchParams } from "react-router-dom";
import { useAppSelector } from "../../state/store";
import { IActiveBreaks, IEntitySetSelection, setActiveBreaks, setEntitySets } from "../../state/entitySelectionSlice";
import { useEntityConfigurationStateContext } from "../../entity/EntityConfigurationStateContext";
import { IEntityType, SavedBreakCombination } from "../../BrandVueApi";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { selectEntitySelectionState } from "client/state/entitySelectionSelectors";
import { Metric } from "../../metrics/metric";
import { getCurrentPageInfo } from "./PagesHelper";
import { PaneType } from "../panes/PaneType";
import { getCategorySortKey } from "./CategorySortKeyHelper";
import { ArrayHelper } from "./ArrayHelper";
import { setPrimaryMetric } from "client/state/applicationSlice";
import { dsession } from "client/dsession";
import { useReadVueQueryParams } from "./UrlHelper";
import { useMetricStateContext } from "client/metrics/MetricStateContext";
import { useSavedBreaksContext } from "../visualisations/Crosstab/SavedBreaksContext";
import { PageHandler } from "../PageHandler";
import Throbber from "../throbber/Throbber";
import { createParamsFromRelevantState, sanitizeEntitySelections, simplifyEntitySelectionState } from "../../entity/SelectionsUrlHelper";
import { setScorecardPeriod } from "../../state/timeSelectionSlice";

export const paramNames = {
    entitySetId: "Set",
    active: "active",
    highlighted: "highlighted",
    entitySetAverages: "entitySetAverages",
    sortKey: "activeSortedBy",
    entityTypes: "entityTypes",
    audienceId: "AudienceId",
    audienceInstanceOrMappingIds: "AudienceInstances",
    audienceMultipleChoiceByValue: "AudienceByValue",
    scorecardPeriod: "ScPeriod",
};

export const legacyParamNames = {
    splitBy: "SplitBy",
    filterBy: "FilterBy",
};

export function getAudienceFromUrl(
    searchParams: URLSearchParams,
    savedBreaks: SavedBreakCombination[],
    validMetrics: Metric[],
    activeEntityType: IEntityType | null,
    entityConfiguration: IEntityConfiguration
): IActiveBreaks {
    const audienceId = searchParams.get(paramNames.audienceId);
    if (audienceId) {
        const instanceIdsStr = searchParams.get(paramNames.audienceInstanceOrMappingIds) || "";
        const selectedInstanceOrMappingIds = instanceIdsStr ? instanceIdsStr.split(",").map((id) => parseInt(id)) : [];
        const multipleChoiceByValue = searchParams.get(paramNames.audienceMultipleChoiceByValue) === "true";

        const activeBreaks: IActiveBreaks = {
            audienceId: parseInt(audienceId),
            selectedInstanceOrMappingIds,
            multipleChoiceByValue,
        };

        if (activeBreaks.multipleChoiceByValue) {
            const selectedAudience = savedBreaks.find((b) => b.id === activeBreaks.audienceId);
            const metric = validMetrics.find((m) => m.name === selectedAudience?.breaks[0]?.measureName);
            if (metric && metric.entityCombination[0]?.identifier !== activeEntityType?.identifier) {
                //"For each chosen entityType" is only available for matching types, reset selected instances
                return {
                    audienceId: parseInt(audienceId),
                    selectedInstanceOrMappingIds: entityConfiguration.getAllEnabledInstancesForTypeOrdered(metric.entityCombination[0]).map((i) => i.id),
                    multipleChoiceByValue: false,
                };
            }
        }

        return activeBreaks;
    }
    return {};
}

function UrlSync(props: { session: dsession; children: React.ReactNode }): React.ReactElement {
    const [searchParams, setSearchParams] = useSearchParams();
    const { getQueryParameter } = useReadVueQueryParams();
    const { savedBreaks } = useSavedBreaksContext();
    const metricStateContext = useMetricStateContext();
    const dispatch = useDispatch();
    const entitySelectionState = useAppSelector(selectEntitySelectionState);
    const timeSelectionState = useAppSelector((state) => state.timeSelection);
    const isSessionLoaded = useAppSelector((state) => state.application.isSessionLoaded);
    const [isStateLoaded, setStateLoaded] = useState(false);
    const previousEntityLoadedState = useRef(false);
    const { hasEntityConfigurationLoaded, entityConfiguration } = useEntityConfigurationStateContext();
    const location = useLocation();

    const lastProcessedSearch = useRef<string | null>(null);
    const lastProcessedPath = useRef<string | null>(null);

    function getCaseInsensitiveParam(searchParams: URLSearchParams, paramName: string): string | null {
        const exactMatch = searchParams.get(paramName);
        if (exactMatch !== null) return exactMatch;
        const lowerParamName = paramName.toLowerCase();
        for (const [key, value] of searchParams.entries()) {
            if (key.toLowerCase() === lowerParamName) {
                return value;
            }
        }
        return null;
    }

    function createEntitySelectionFromParams(
        params: URLSearchParams,
        orderedEntityTypes: IEntityType[],
        pageHandler: PageHandler
    ): {
        entityType: string;
        entitySet: IEntitySetSelection;
    }[] {
        let results: { entityType: string; entitySet: IEntitySetSelection }[] = [];
        const filterBy = getCaseInsensitiveParam(params, legacyParamNames.filterBy);
        let i = 1;
        for (let entityType of orderedEntityTypes) {
            const suffix = i == 1 ? "" : i.toString();
            const entitySetId = getCaseInsensitiveParam(params, paramNames.entitySetId + suffix);
            const highlighted = getCaseInsensitiveParam(params, paramNames.highlighted + suffix);

            let selection: IEntitySetSelection = {
                entitySetId: entitySetId ? parseInt(entitySetId) : undefined,
                highlighted: highlighted?.split(".").map((x) => parseInt(x)) ?? undefined,
                active: getCaseInsensitiveParam(params, paramNames.active + suffix)
                    ? parseInt(getCaseInsensitiveParam(params, paramNames.active + suffix)!)
                    : undefined,
                entitySetAverages:
                    getCaseInsensitiveParam(params, paramNames.entitySetAverages + suffix)
                        ?.split(".")
                        .map((x) => parseInt(x)) ?? undefined,
            };
            if (i == 2 && filterBy) {
                selection.active = parseInt(filterBy);
            }
            results.push({ entityType: entityType.identifier, entitySet: selection });
            i++;
        }
        return results;
    }

    function defaultEntityTypeOrderForPage() {
        var activeMetrics = props.session.activeView.activeMetrics;
        var entityTypesForView = props.session.activeView.getEntityCombination();
        const activeMetricDefaultEntityType = activeMetrics === undefined || activeMetrics.length === 0 ? null : activeMetrics[0].defaultSplitByEntityTypeName;
        const currentPage = getCurrentPageInfo(location).page;
        const currentPageDefaultEntityType =
            currentPage?.panes?.length > 0 && currentPage?.panes[0].parts?.length > 0 ? currentPage.panes[0].parts[0].defaultSplitBy : null;

        var defaultEntityTypesInPriorityOrder: string[] = [
            currentPageDefaultEntityType,
            activeMetricDefaultEntityType,
            entityConfiguration.defaultEntityType?.identifier ?? entityTypesForView.find((x) => x.isBrand)?.identifier,
        ].filter((x) => x != null);
        return ArrayHelper.prioritySortThenMaintainOrder(entityTypesForView, (item) => item.identifier, defaultEntityTypesInPriorityOrder);
    }

    // we can add edge cases here if needed. if this method becomes complex, consider moving reviewing the logic of applying url params to a specific page
    function pageForcesEntityOrder() {
        const currentPageInfo = getCurrentPageInfo(location);
        const currentPage = currentPageInfo.page;

        //in reports page, we always use the default priority order
        return currentPage?.panes?.some((pane) => pane.paneType === PaneType.reportsPage || pane.paneType === PaneType.reportSubPage) ?? false;
    }

    function createActiveEntityTypesFromOtherState(paramSplitBy: string | null, paramEntityTypes: string[] | null): IEntityType[] {
        var entityTypesForView = defaultEntityTypeOrderForPage();

        if (pageForcesEntityOrder()) {
            return entityTypesForView;
        }

        const orderedEntityTypesFromState = [
            ...(paramEntityTypes ?? []),
            ...entitySelectionState.priorityOrderedEntityTypes.map((et) => et.identifier),
            ...(paramSplitBy ? [paramSplitBy] : []),
        ];

        return ArrayHelper.prioritySortThenMaintainOrder(entityTypesForView, (item) => item.identifier, orderedEntityTypesFromState);
    }

    const readyToSyncFromUrl = isSessionLoaded && hasEntityConfigurationLoaded && metricStateContext.hasMetricsLoaded;

    useEffect(() => {
        if (readyToSyncFromUrl) {
            //even if the search params are synced, the page handler still may need to be updated
            if (lastProcessedSearch.current === location.search && lastProcessedPath.current == location.pathname) {
                return;
            }
            props.session.pageHandler.updateActiveDashPage(
                metricStateContext.enabledMetricSet,
                metricStateContext.crosstabPageMetrics,
                location,
                getQueryParameter
            );
            lastProcessedSearch.current = location.search;
            lastProcessedPath.current = location.pathname;
            const activeMetrics = props.session.activeView.activeMetrics;
            const splitBy = getCaseInsensitiveParam(searchParams, legacyParamNames.splitBy);
            const paramEntityTypes = getCaseInsensitiveParam(searchParams, paramNames.entityTypes)?.split(".") ?? null;
            const entityTypes = createActiveEntityTypesFromOtherState(splitBy, paramEntityTypes);
            const scorecardPeriod = getCaseInsensitiveParam(searchParams, paramNames.scorecardPeriod);
            let newEntityState = createEntitySelectionFromParams(searchParams, entityTypes, props.session.pageHandler);

            // Sanitize the entity selections before dispatching to store
            newEntityState = sanitizeEntitySelections(
                newEntityState,
                entityConfiguration,
                previousEntityLoadedState.current ? entitySelectionState.priorityOrderedEntityTypes : undefined
            );

            const sortKey = getCaseInsensitiveParam(searchParams, paramNames.sortKey);
            dispatch(
                setEntitySets({
                    selections: newEntityState,
                    priorityOrderedEntityTypes: entityTypes,
                    categorySortKey: sortKey ? getCategorySortKey(sortKey) : null,
                })
            );
            dispatch(
                setScorecardPeriod(
                    scorecardPeriod ??
                        getQueryParameter<string>("Average") ??
                        (props.session.averages.find((a) => a.isDefault) ?? props.session.averages[0]).averageId
                )
            );
            dispatch(setActiveBreaks(getAudienceFromUrl(searchParams, savedBreaks, activeMetrics, entityTypes?.[0], entityConfiguration)));
            dispatch(setPrimaryMetric(activeMetrics[0]));
            setStateLoaded(true);
        }
        previousEntityLoadedState.current = hasEntityConfigurationLoaded;
    }, [location.pathname, location.search, readyToSyncFromUrl]);

    const isReadyForSyncToUrl = isSessionLoaded && hasEntityConfigurationLoaded && metricStateContext.hasMetricsLoaded && isStateLoaded;

    useEffect(() => {
        // skip if this state change was triggered by URL changes
        if (!isReadyForSyncToUrl) {
            return;
        }
        const newSearchParams = new URLSearchParams(searchParams);

        // Simplify the entity selection state before creating params
        const simplifiedState = simplifyEntitySelectionState(entitySelectionState, entityConfiguration);
        const entityTypesForUrl = ArrayHelper.isEqual(
            defaultEntityTypeOrderForPage().map((x) => x.identifier),
            entitySelectionState.priorityOrderedEntityTypes.map((x) => x.identifier)
        )
            ? null
            : entitySelectionState.priorityOrderedEntityTypes; //only write types if different from default
        Object.entries(createParamsFromRelevantState(simplifiedState, timeSelectionState, entityTypesForUrl, props.session.pageHandler)).forEach(
            ([key, values]) => {
                if (values == null) {
                    newSearchParams.delete(key);
                } else {
                    newSearchParams.set(key, values);
                }
            }
        );
        newSearchParams.sort();
        const newSearchString = newSearchParams.toString();
        if (newSearchString !== searchParams.toString()) {
            lastProcessedSearch.current = "?" + newSearchString;
            lastProcessedPath.current = location.pathname;
            setSearchParams(newSearchParams, { replace: true });
        }
    }, [entitySelectionState, timeSelectionState, isReadyForSyncToUrl]);

    return isStateLoaded ? (
        <>{props.children}</>
    ) : (
        <div id="ld" className="loading-container">
            <Throbber />
        </div>
    );
}

export default UrlSync;
