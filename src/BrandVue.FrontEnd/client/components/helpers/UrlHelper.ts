import QueryString from "query-string";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { allViewTypes } from "./ViewTypeHelper";
import moment from "moment";
import { ComparisonPeriodSelection } from "../../BrandVueApi";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { useCallback } from "react";
import { EntitySelectionState } from "client/state/entitySelectionSlice";
import { PageHandler } from "../PageHandler";
import { createParamsFromRelevantState } from "client/entity/SelectionsUrlHelper";
import { TimeSelectionState } from "client/state/timeSelectionSlice";

export interface IQueryParams {
    average: string;
    start?: string;
    end?: string;
    period?: ComparisonPeriodSelection;
    showAverageIndicator?: string;
    metric?: string;
    baseVariableId1?: number;
    baseVariableId2?: number;
}

// List of optional modifiers that can affect the query parameters when building a URL
export interface IQueryParamModifiers {
    filters?: CuratedFilters;
    averageId?: string;
}

export const QueryStringParamNames = {
    average: "Average",
    start: "Start",
    end: "End",
    range: "Range",
    now: "Now",
    period: "Period",
    subset: "Subset",
    showAverageIndicator: "ShowAverageIndicator",
    urlSafeMetricName: "metric",
    showDataLabels: "ShowDataLabels",
    baseVariableId1: "BaseId1",
    baseVariableId2: "BaseId2",
    appMode: "appMode",
};

export interface IQueryStringParam {
    name: string;
    value: string | number | string[] | number[] | null | undefined;
    includeEmptyInUrl?: boolean;
}
///These parameters will allow empty set [] to be returned and included in the url without casting it to null
export const ParamsToAllowEmptySetInUrl = ["EntitySetAverages"];

export const paramString = (currentQueryString: string, newParameters: IQueryStringParam[]): {} => {
    const parsedQueryString = QueryString.parse(currentQueryString);
    for (let i = 0; i < newParameters.length; i++) {
        const name = newParameters[i].name;
        const value = newParameters[i].value;

        // No need to replace " " with "+" as they that is done on the return statement
        parsedQueryString[name] = Array.isArray(value) ? value.join(".") : value;
        if (!(ParamsToAllowEmptySetInUrl.includes(name) ?? false)) {
            if (parsedQueryString[name] === "") {
                delete parsedQueryString[name];
            }
        } else {
            if (value == null) {
                delete parsedQueryString[name];
            }
        }
    }
    return parsedQueryString;
};

export const constructQueryString = (currentQueryString: string, newParameters: IQueryStringParam[]): string => {
    var parsedQueryString = paramString(currentQueryString, newParameters);
    return `?${QueryString.stringify(parsedQueryString, { encode: true }).replace(/%20/g, "+")}`;
};

export const constructQueryStringIncludeEntitySet = (
    currentQueryString: string,
    newParameters: IQueryStringParam[],
    entitySelectionState: EntitySelectionState,
    timeSelectionState: TimeSelectionState,
    pageHandler: PageHandler
): string => {
    var parsedQueryString = paramString(currentQueryString, newParameters);
    var entitySetParams = createParamsFromRelevantState(entitySelectionState, timeSelectionState, entitySelectionState.priorityOrderedEntityTypes, pageHandler);
    return `?${QueryString.stringify({ ...parsedQueryString, ...entitySetParams }, { encode: true }).replace(/%20/g, "+")}`;
};

export interface IReadVueQueryParams {
    getQueryParameter: <T = string>(parameterName: string, defaultValue?: T) => T | undefined;
    getQueryParameterArray: <T = string>(parameterName: string) => T[];
    getQueryParameterInt: (parameterName: string) => number | undefined;
    getQueryParameterIntArray: (parameterName: string) => number[];
    getQueryParamsFromQueryString: () => IQueryParams;
}

export const useReadVueQueryParams = (): IReadVueQueryParams => {
    const [searchParams] = useSearchParams();

    const getQueryParameter = useCallback(
        <T = string>(parameterName: string, defaultValue?: T): T | undefined => {
            const value = searchParams.get(parameterName);
            return value !== null ? (value as unknown as T) : defaultValue;
        },
        [searchParams]
    );

    const getQueryParameterArray = useCallback(
        <T = string>(parameterName: string): T[] => {
            const value = getQueryParameter(parameterName);
            return value ? (value.split(",") as unknown as T[]) : [];
        },
        [getQueryParameter]
    );

    const getQueryParameterInt = useCallback(
        (parameterName: string): number | undefined => {
            const value = getQueryParameter(parameterName);
            return value ? parseInt(value, 10) : undefined;
        },
        [getQueryParameter]
    );

    const getQueryParameterIntArray = useCallback(
        (parameterName: string): number[] => {
            const value = getQueryParameterArray(parameterName);
            return value.map((v: string) => parseInt(v, 10)).filter((v: number) => !isNaN(v));
        },
        [getQueryParameterArray]
    );

    const getQueryParamsFromQueryString = useCallback((): IQueryParams => {
        return {
            average: getQueryParameter<string>(QueryStringParamNames.average)!,
            start: getQueryParameter<string>(QueryStringParamNames.start),
            end: getQueryParameter<string>(QueryStringParamNames.end),
            period: getQueryParameter<ComparisonPeriodSelection>(QueryStringParamNames.period),
            showAverageIndicator: getQueryParameter<string>(QueryStringParamNames.showAverageIndicator),
            metric: getQueryParameter<string>(QueryStringParamNames.urlSafeMetricName),
            baseVariableId1: getQueryParameterInt(QueryStringParamNames.baseVariableId1),
            baseVariableId2: getQueryParameterInt(QueryStringParamNames.baseVariableId2),
        };
    }, [getQueryParameter, getQueryParameterArray, getQueryParameterInt, getQueryParameterIntArray]);

    return {
        getQueryParameter,
        getQueryParameterArray,
        getQueryParameterInt,
        getQueryParameterIntArray,
        getQueryParamsFromQueryString,
    };
};
export interface IWriteVueQueryParams {
    setQueryParameter: (name: string, value: string | number | string[] | number[] | undefined) => void;
    setQueryParameters: (params: IQueryStringParam[]) => void;
    updateQueryParametersInQueryString: (newParameters: IQueryParams, oldQueryParams: IQueryParams) => void;
}

export const useWriteVueQueryParams = (navigate: ReturnType<typeof useNavigate>, location: ReturnType<typeof useLocation>): IWriteVueQueryParams => {
    const setQueryParameter = useCallback(
        (name: string, value: string | number | string[] | number[] | undefined) => {
            setQueryParameters([{ name: name, value: value }]);
        },
        [navigate, location]
    );

    const setQueryParameters = useCallback(
        (params: IQueryStringParam[]) => {
            const currentSearch = location.search.startsWith("?") ? location.search : `?${location.search}`;
            params = params.toSorted();
            const newSearch = constructQueryString(currentSearch, params);

            if (currentSearch !== newSearch) {
                navigate(
                    {
                        pathname: location.pathname,
                        search: newSearch,
                        hash: location.hash,
                    },
                    { replace: true }
                );
            }
        },
        [navigate, location]
    );

    const updateQueryParametersInQueryString = useCallback(
        (newParameters: IQueryParams, oldParameters: IQueryParams) => {
            const paramsToUpdate: { name: string; value: any }[] = [
                { name: QueryStringParamNames.start, value: newParameters.start },
                { name: QueryStringParamNames.end, value: newParameters.end },
            ];

            // Conditional parameters
            const conditionalParams = [
                { check: oldParameters.average, param: { name: QueryStringParamNames.average, value: newParameters.average } },
                { check: oldParameters.period, param: { name: QueryStringParamNames.period, value: newParameters.period } },
                { check: oldParameters.metric, param: { name: QueryStringParamNames.urlSafeMetricName, value: newParameters.metric } },
                { check: oldParameters.baseVariableId1, param: { name: QueryStringParamNames.baseVariableId1, value: newParameters.baseVariableId1 } },
                { check: oldParameters.baseVariableId2, param: { name: QueryStringParamNames.baseVariableId2, value: newParameters.baseVariableId2 } },
            ];

            conditionalParams.forEach(({ check, param }) => {
                if (check) {
                    paramsToUpdate.push(param);
                }
            });

            setQueryParameters(
                paramsToUpdate.map((a) => ({
                    ...a,
                    includeEmptyInUrl: ParamsToAllowEmptySetInUrl.includes(a.name),
                }))
            );
        },
        [setQueryParameters]
    );

    return {
        setQueryParameter,
        setQueryParameters,
        updateQueryParametersInQueryString,
    };
};

export const parseBrandVuePath = (path: string) => {
    const s = path.split("/");
    const requestedViewType = allViewTypes.find((i) => i.url === "/" + s[s.length - 1]);

    if (requestedViewType) {
        s.pop();
    }

    const pagePart = s.join("/");

    return { RequestedPageUrl: pagePart, RequestedViewUrl: requestedViewType?.url };
};

export const getUrlSafePageName = (pageName: string): string => {
    return pageName
        .toLowerCase()
        .replace(/[\s:]/g, "-")
        .replace(/[?#\[\]@!$'()*+,;=%\\]+/g, "")
        .replace(/&/g, "and")
        .replace(/\//g, "or");
};

export const getPathByPageName = (pageName: string): string => {
    return "/" + getUrlSafePageName(pageName);
};

export const getBasePathByPageName = (pageName: string): string => {
    return "/ui" + getPathByPageName(pageName);
};

export const replaceQueryParams = (currentQuery: string, queryModifiers: IQueryParamModifiers, readParams: IReadVueQueryParams): string => {
    const currentQueryParams = readParams.getQueryParamsFromQueryString();
    const requestedFilters = queryModifiers.filters;

    const finalAverageId = queryModifiers.averageId || requestedFilters?.average?.averageId || currentQueryParams.average;

    const finalStart = requestedFilters?.startDate != null ? moment.utc(requestedFilters.startDate).format("YYYY-MM-DD") : currentQueryParams.start;
    const finalEnd = requestedFilters?.endDate != null ? moment.utc(requestedFilters.endDate).format("YYYY-MM-DD") : currentQueryParams.end;
    const finalPeriod = requestedFilters?.comparisonPeriodSelection ?? currentQueryParams.period;

    const values = [
        { name: QueryStringParamNames.average, value: finalAverageId },
        { name: QueryStringParamNames.start, value: finalStart },
        { name: QueryStringParamNames.end, value: finalEnd },
        { name: QueryStringParamNames.period, value: finalPeriod },
        { name: QueryStringParamNames.showAverageIndicator, value: currentQueryParams.showAverageIndicator },
    ];
    return constructQueryString(currentQuery, values);
};
