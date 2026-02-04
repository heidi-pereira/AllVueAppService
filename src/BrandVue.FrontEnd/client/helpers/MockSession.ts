import { dsession } from "../dsession";
import { PageHandler } from "../components/PageHandler";
import { AverageDescriptor, IAverageDescriptor, PageDescriptor, Subset, ApplicationUser } from "../BrandVueApi";
import { EntityConfiguration, IEntityConfigurationModel } from "../entity/EntityConfiguration";
import { MockActiveView, MockApplication } from "./MockApp";
import { ApplicationConfiguration } from "../ApplicationConfiguration";
import { ProductConfiguration } from "../ProductConfiguration";
import { DataSubsetManager } from "../DataSubsetManager";
import { Metric } from "../metrics/metric";
import { filterSet } from "../filter/filterSet";
import { setActivePage } from "../components/helpers/PagesHelper";

export const SubSetMock = new Subset({
        alias: "UK",
        description: "UK",
        disabled: false,
        displayName: "UK",
        displayNameShort: "UK",
        enableRawDataApiAccess: false,
        environment: [],
        externalUrl: "UK",
        id: "UK",
        index: 1,
        iso2LetterCountryCode: "UK",
        minimumDataSpan: "",
        order: 1,
        productId: 1,
        alwaysShowDataUpToCurrentDate: false
    });

export const DataSubsetMock = {
    selectedSubset: SubSetMock,
    supportsDataSubset:jest.fn(()=>true),
    setSelectedSubsetById: jest.fn(),
    parseSupportedSubsets: jest.fn(),
    filterMetricByCurrentSubset: jest.fn((metrics: Metric[]) => metrics),
}
export function createMockSession(averageFilter?: (average: IAverageDescriptor) => boolean): dsession {
    DataSubsetManager.selectedSubset = new Subset();
    DataSubsetManager.selectedSubset.id = "UK";
    const session = new dsession();
    const pageHandler = new PageHandler(session);
    var averageDescriptors = (averageFilter
        ? convertedTestAverages.filter(averageFilter)
        : convertedTestAverages);
    session.averages = averageDescriptors;
    session.pageHandler = pageHandler;
    session.selectedSubsetId = DataSubsetManager.selectedSubset.id;
    const filters = new filterSet();
    filters.filters = [];
    filters.filterLookup = {};
    session.activeView = new MockActiveView();
    setActivePage(new PageDescriptor());
    return session;
}

export function createMockProductConfiguration() {
    const productConfiguration = new ProductConfiguration();
    productConfiguration.productName = "MockApp";
    productConfiguration.user = new ApplicationUser();
    productConfiguration.googleTags = [];
    productConfiguration.gaTags = [];
    return productConfiguration;
}

export function createMockApplicationConfiguration(firstDataPointDate: Date, lastDataPointDate: Date, hasLoadedData: boolean = true) {
    const applicationConfiguration = new ApplicationConfiguration();
    applicationConfiguration.hasLoadedData = hasLoadedData;
    applicationConfiguration.dateOfFirstDataPoint = firstDataPointDate;
    applicationConfiguration.dateOfLastDataPoint = lastDataPointDate;
    return applicationConfiguration;
}

export function createMockEntityConfiguration(entityConfigurationModels?: IEntityConfigurationModel[]) {
    return new EntityConfiguration(
        entityConfigurationModels || MockApplication.mockEntityModels,
        MockApplication.brandEntityType.identifier,
        DataSubsetManager.selectedSubset.id
    );
}

const testAverages = [
    {
        "averageId": "Weekly",
        "displayName": "Weekly",
        "order": 50,
        "group": ["Calendar", "CalendarShort"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 7,
        "weightingMethod": "QuotaCell",
        "weightAcross": "SinglePeriod",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "WeekEnd",
        "includeResponseIds": false,
        "internalIndex": 0,
        "isDefault": false,
        "disabled": false
    },
    {
        "averageId": "Monthly",
        "displayName": "Monthly",
        "order": 100,
        "group": ["Calendar", "CalendarShort"],
        "totalisationPeriodUnit": "Month",
        "numberOfPeriodsInAverage": 1,
        "weightingMethod": "QuotaCell",
        "weightAcross": "SinglePeriod",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "MonthEnd",
        "includeResponseIds": false,
        "internalIndex": 0,
        "isDefault": true,
        "disabled": false
    },
    {
        "averageId": "MonthlyOver3Months",
        "displayName": "Monthly (over 3 months)",
        "order": 200,
        "group": ["Calendar", "CalendarLong"],
        "totalisationPeriodUnit": "Month",
        "numberOfPeriodsInAverage": 3,
        "weightingMethod": "QuotaCell",
        "weightAcross": "SinglePeriod",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "MonthEnd",
        "includeResponseIds": false,
        "internalIndex": 1,
        "isDefault": false,
        "disabled": false
    },
    {
        "averageId": "MonthlyOver6Months",
        "displayName": "Monthly (over 6 months)",
        "order": 300,
        "group": ["Calendar", "CalendarLong"],
        "totalisationPeriodUnit": "Month",
        "numberOfPeriodsInAverage": 6,
        "weightingMethod": "QuotaCell",
        "weightAcross": "SinglePeriod",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "MonthEnd",
        "includeResponseIds": false,
        "internalIndex": 2,
        "isDefault": false,
        "disabled": false
    },
    {
        "averageId": "Quarterly",
        "displayName": "Quarterly",
        "order": 400,
        "group": ["Calendar", "CalendarShort"],
        "totalisationPeriodUnit": "Month",
        "numberOfPeriodsInAverage": 3,
        "weightingMethod": "QuotaCell",
        "weightAcross": "SinglePeriod",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "QuarterEnd",
        "includeResponseIds": false,
        "internalIndex": 3,
        "isDefault": false,
        "disabled": false
    },
    {
        "averageId": "HalfYearly",
        "displayName": "Half Yearly",
        "order": 475,
        "group": ["Calendar", "CalendarShort"],
        "totalisationPeriodUnit": "Month",
        "numberOfPeriodsInAverage": 6,
        "weightingMethod": "QuotaCell",
        "weightAcross": "SinglePeriod",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "HalfYearEnd",
        "includeResponseIds": false,
        "internalIndex": 5,
        "isDefault": false,
        "environment": ["dev", "test", "beta"],
        "disabled": false
    },
    {
        "averageId": "Annual",
        "displayName": "Annual",
        "order": 500,
        "group": ["Calendar", "CalendarLong"],
        "totalisationPeriodUnit": "Month",
        "numberOfPeriodsInAverage": 12,
        "weightingMethod": "QuotaCell",
        "weightAcross": "SinglePeriod",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "CalendarYearEnd",
        "includeResponseIds": false,
        "internalIndex": 6,
        "isDefault": false,
        "environment": ["dev", "test", "beta"],
        "disabled": false
    },
    {
        "averageId": "Daily",
        "displayName": "Daily (DEBUG ONLY)",
        "order": 600,
        "group": ["Daily", "DailyShort"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 1,
        "weightingMethod": "QuotaCell",
        "weightAcross": "AllPeriods",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "Day",
        "includeResponseIds": false,
        "internalIndex": 7,
        "isDefault": false,
        "environment": ["dev", "test", "beta"],
        "disabled": false
    },
    {
        "averageId": "3Days",
        "displayName": "3 days (DEBUG ONLY)",
        "order": 700,
        "group": ["Daily", "DailyShort"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 3,
        "weightingMethod": "QuotaCell",
        "weightAcross": "AllPeriods",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "Day",
        "includeResponseIds": false,
        "internalIndex": 8,
        "isDefault": false,
        "environment": ["dev", "test", "beta"],
        "disabled": false
    },
    {
        "averageId": "7Days",
        "displayName": "7 days",
        "order": 800,
        "group": ["Daily", "DailyShort"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 7,
        "weightingMethod": "QuotaCell",
        "weightAcross": "AllPeriods",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "Day",
        "includeResponseIds": false,
        "internalIndex": 9,
        "isDefault": false,
        "environment": ["dev", "test", "beta"],
        "disabled": false
    },
    {
        "averageId": "14Days",
        "displayName": "14 days",
        "order": 900,
        "group": ["Daily", "DailyShort"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 14,
        "weightingMethod": "QuotaCell",
        "weightAcross": "AllPeriods",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "Day",
        "includeResponseIds": false,
        "internalIndex": 10,
        "isDefault": false,
        "disabled": false
    },
    {
        "averageId": "28Days",
        "displayName": "28 days",
        "order": 1000,
        "group": ["Daily", "DailyShort"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 28,
        "weightingMethod": "QuotaCell",
        "weightAcross": "AllPeriods",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "Day",
        "includeResponseIds": false,
        "internalIndex": 11,
        "isDefault": false,
        "disabled": false
    },
    {
        "averageId": "12Weeks",
        "displayName": "12 weeks",
        "order": 1100,
        "group": ["Daily", "DailyShort"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 84,
        "weightingMethod": "QuotaCell",
        "weightAcross": "AllPeriods",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "Day",
        "includeResponseIds": false,
        "internalIndex": 12,
        "isDefault": false,
        "disabled": false
    },
    {
        "averageId": "26Weeks",
        "displayName": "26 weeks",
        "order": 1200,
        "group": ["Daily", "DailyLong"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 182,
        "weightingMethod": "QuotaCell",
        "weightAcross": "AllPeriods",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "Day",
        "includeResponseIds": false,
        "internalIndex": 13,
        "isDefault": false,
        "environment": ["dev", "test", "beta"],
        "disabled": false
    },
    {
        "averageId": "52Weeks",
        "displayName": "52 weeks",
        "order": 1300,
        "group": ["Daily", "DailyLong"],
        "totalisationPeriodUnit": "Day",
        "numberOfPeriodsInAverage": 364,
        "weightingMethod": "QuotaCell",
        "weightAcross": "AllPeriods",
        "averageStrategy": "OverAllPeriods",
        "makeUpTo": "Day",
        "includeResponseIds": false,
        "internalIndex": 14,
        "isDefault": false,
        "environment": ["dev", "test", "beta"],
        "disabled": false
    }
];

export const convertedTestAverages = testAverages.map(a => AverageDescriptor.fromJS(a));
