import React, { useEffect } from "react";
import {
    IAverageDescriptor,
    Factory,
    IApplicationUser,
    ICustomPeriod,
    MainQuestionType,
    PageDescriptor,
    Report,
    PermissionFeaturesOptions,
} from "../../../BrandVueApi";
import { useMetricStateContext } from "../../../metrics/MetricStateContext";
import { dsession } from "../../../dsession";
import { useSavedReportsContext } from "./SavedReportsContext";
import Throbber from "../../throbber/Throbber";
import ReportsPageDisplay from "./ReportsPageDisplay";
import { CuratedFilters } from "../../../filter/CuratedFilters";
import { UserContext } from "../../../GlobalContext";
import ReportsDashboard from "./ReportsDashboard";
import { FilterStateProvider } from "../../../filter/FilterStateContext";
import { getMetricFilterFromDefault } from "./Filtering/FilterHelper";
import { MetricFilterState } from "../../../filter/metricFilterState";
import { Metric } from "../../../metrics/metric";
import { CatchReportAndDisplayErrors } from "../../CatchReportAndDisplayErrors";
import { ApplicationConfiguration } from "../../../ApplicationConfiguration";
import { VariableProvider} from "../Variables/VariableModal/Utils/VariableContext";
import { IGoogleTagManager } from "../../../googleTagManager";
import { ProductConfigurationContext } from "../../../ProductConfigurationContext";
import { hasAllVuePermissionsOrSystemAdmin } from "../../helpers/FeaturesHelper";
import { useEntityConfigurationStateContext } from "../../../entity/EntityConfigurationStateContext";
import { useAppDispatch, useAppSelector } from '../../../state/store';
import { updateSubset, selectSubsetId } from '../../../state/subsetSlice';
import { selectCurrentReportOrNull, selectReportIsSelected } from "../../../state/reportSelectors";
import { useReportsPageUrl } from '../../hooks/reportHooks';
import { selectAllReports, selectDefaultReportId, selectIsDataInSyncWithDatabase, selectIsLoadingReports, setIsDataInSyncWithDatabase } from "client/state/reportSlice";
import BrandVueOnlyLowSampleHelper from "../BrandVueOnlyLowSampleHelper";
import { ProductConfiguration } from "client/ProductConfiguration";

interface IReportsPageProps {
    session: dsession;
    googleTagManager: IGoogleTagManager;
    applicationConfiguration: ApplicationConfiguration;
    curatedFilters: CuratedFilters;
    averages: IAverageDescriptor[];
    productConfiguration: ProductConfiguration;
}

export type ReportWithPage = {
    report: Report;
    page: PageDescriptor;
}

const ReportsPage = (props: IReportsPageProps) => {
    const { reportsDispatch } = useSavedReportsContext();
    const { enabledMetricSet, metricsForReports } = useMetricStateContext();
    const [questionTypeLookup, setQuestionTypeLookup] = React.useState<{[key: string]: MainQuestionType}>({});
    const [dataWaves, setDataWaves] = React.useState<ICustomPeriod[]>([]);
    const [isLoading, setIsLoading] = React.useState<boolean>(true);
    const { productConfiguration } = React.useContext(ProductConfigurationContext);
    const { entityConfiguration } = useEntityConfigurationStateContext();

    const dispatch = useAppDispatch();
    const subsetId = useAppSelector(selectSubsetId);
    const currentReportPage = useAppSelector(selectCurrentReportOrNull);
    const allReports = useAppSelector(selectAllReports);
    const reportsPageUrl = useReportsPageUrl();
    const reportIsSelected = useAppSelector(selectReportIsSelected);
    const isLoadingReports = useAppSelector(selectIsLoadingReports);
    const isDataInSyncWithDatabase = useAppSelector(selectIsDataInSyncWithDatabase);
    const defaultReportId = useAppSelector(selectDefaultReportId);

    React.useEffect(() => {
        if (currentReportPage?.report.subsetId && currentReportPage.report.subsetId !== subsetId) {
            dispatch(updateSubset(currentReportPage.report.subsetId));
        }
    }, [currentReportPage?.report.subsetId, subsetId, dispatch]);

    
    useEffect(() => {
        const lowSampleThreshold = currentReportPage?.report?.lowSampleThreshold ?? props.productConfiguration.lowSampleForBrand;
        BrandVueOnlyLowSampleHelper.initialiseThresholds(lowSampleThreshold, props.productConfiguration.noSampleForBrand);
    }, [currentReportPage?.report?.savedReportId, currentReportPage?.report?.lowSampleThreshold]);

    React.useEffect(() => {
        setIsLoading(true);
        const client = Factory.MetaDataClient(throwError => throwError());
        const questionTypesPromise = client.getQuestionTypes(subsetId);
        const customPeriodsPromise = client.getCustomPeriods();
        Promise.all([questionTypesPromise, customPeriodsPromise]).then(results => {
            const [questionTypes, customPeriods] = results;
            setQuestionTypeLookup(questionTypes);
            setDataWaves(customPeriods);
            setIsLoading(false);
        });
    }, [JSON.stringify(allReports)]);

    React.useEffect(() => {
        let intervalId: number | undefined;
        const reportId = currentReportPage?.report?.savedReportId;
        const reportGuid = currentReportPage?.report?.modifiedGuid;
        if (reportId && reportGuid) {
            intervalId = window.setInterval(() => {
                reportsDispatch({type: "POLL_FOR_UPDATES", data: {reportGUID: reportGuid, reportId: reportId}});
            }, 30000);
        }
        return () => {
            if (intervalId !== undefined) {
                clearInterval(intervalId);
            }
        };
    }, [currentReportPage?.report?.savedReportId, currentReportPage?.report?.modifiedGuid]);

    if (isLoading || (isLoadingReports && allReports.length == 0)) {
        return (
            <div className="throbber-container-fixed">
                <Throbber />
            </div>
        );
    }

    const convertDefaultFiltersToMetricFilters = (): MetricFilterState[] => {
        return currentReportPage?.report.defaultFilters.map(f => getMetricFilterFromDefault(f, metricsForReports, entityConfiguration)).flatMap(f => {
            return f.filters;
        }) ?? [];
    }

    const getAvailableMetricsForFilter = (): Metric[] => {
        return currentReportPage?.report.defaultFilters.flatMap(f => {
            var metric = f.measureName && enabledMetricSet.getMetric(f.measureName);
            if (metric) {
                return [metric];
            }
            return [];
        }) ?? [];
    }

    const getContent = (user: IApplicationUser | null) => {
        const canEditReports = hasAllVuePermissionsOrSystemAdmin(productConfiguration, [PermissionFeaturesOptions.ReportsAddEdit]);

        if (!reportIsSelected || !currentReportPage?.page) {
            if (!defaultReportId) {
                return (
                    <ReportsDashboard
                        canEditReports={canEditReports}
                        metricsForReports={metricsForReports}
                        curatedFilters={props.curatedFilters}
                        questionTypeLookup={questionTypeLookup}
                        googleTagManager={props.googleTagManager}
                        pageHandler={props.session.pageHandler}
                        user={user}
                        reportsPageUrl={reportsPageUrl!}
                        applicationConfiguration={props.applicationConfiguration}
                        averages={props.averages}
                    />
                );
            }
            return (
                <div id="reports-page">
                    Failed to find matching page
                </div>
            );
        }
        return (
            <VariableProvider
                user={user}
                nonMapFileSurveys={productConfiguration.nonMapFileSurveys}
                googleTagManager={props.googleTagManager}
                pageHandler={props.session.pageHandler}
                isSurveyGroup={productConfiguration.isSurveyGroup}
            >
                <ReportsPageDisplay
                    key={`${currentReportPage.report.savedReportId}-${currentReportPage.report.modifiedGuid}`}
                    metricsForReports={metricsForReports}
                    dataWaves={dataWaves}
                    googleTagManager={props.googleTagManager}
                    pageHandler={props.session.pageHandler}
                    curatedFilters={props.curatedFilters}
                    questionTypeLookup={questionTypeLookup}
                    canEditReports={canEditReports}
                    user={user}
                    reportsPageUrl={reportsPageUrl!}
                    applicationConfiguration={props.applicationConfiguration}
                    averages={props.averages}
                    isDataInSyncWithDatabase={isDataInSyncWithDatabase}
                    setIsDataInSyncWithDatabase={(isInSync: boolean) => dispatch(setIsDataInSyncWithDatabase(isInSync))}
                />
            </VariableProvider>
        );
    }

    return (
        <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration}
            childInfo={{"Report": currentReportPage?.report?.savedReportId.toString() ?? 'None'}}
        >
            <FilterStateProvider initialFilters={convertDefaultFiltersToMetricFilters()} metrics={getAvailableMetricsForFilter()} googleTagManager={props.googleTagManager}
                    pageHandler={props.session.pageHandler}>
                <UserContext.Consumer>
                    {(user) => getContent(user)}
                </UserContext.Consumer>
            </FilterStateProvider>
        </CatchReportAndDisplayErrors>
    );
}

export default ReportsPage;