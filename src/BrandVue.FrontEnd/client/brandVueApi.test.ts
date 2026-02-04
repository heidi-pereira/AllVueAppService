import * as BrandVueApi from "./BrandVueApi";
import { FilterOperator, SigConfidenceLevel } from "./BrandVueApi";
import fetchMock from "jest-fetch-mock";
import { setupDefaultMockFetch } from './helpers/MockBrandVueApi';
import Factory = BrandVueApi.Factory;

test("URL compression does not throw", async () => {
    setupDefaultMockFetch();

    const model = getDummyModel();
    const result = await Factory.DataClient(() => { }, "https://barometer.wgsn.com").breakdown(model);
    expect(fetchMock.mock.calls.length).toBeGreaterThan(0);
    expect(fetchMock.mock.calls[0].length).toBeGreaterThan(0);

    // There's a test of the decryption in ApiModelGetValidationTests.Test_CuratedResultsRequest_ReturnsOK_WhenModelIsValid
    expect(fetchMock.mock.calls[0][0]).toEqual(expect.stringContaining("https://barometer.wgsn.com/api/data/breakdown?model="));
});

function getDummyModel(): BrandVueApi.MultiEntityRequestModel {
    const measureFilters = [
        new BrandVueApi.MeasureFilterRequestModel({
            measureName: "Custom Ages (Group 1)",
            entityInstances: Object.fromEntries([["brand", [-1]]]),
            values: [35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74],
            invert: false,
            treatPrimaryValuesAsRange: false
        })
    ];

    const filterModel = new BrandVueApi.CompositeFilterModel({filterOperator: FilterOperator.And, filters: measureFilters, compositeFilters: []});
    return new BrandVueApi.MultiEntityRequestModel({
        measureName: "Satisfaction In-Store",
        subsetId: "UK",
        period: new BrandVueApi.Period({
            average: "12Weeks",
            comparisonDates: [new BrandVueApi.CalculationPeriodSpan({ startDate: new Date("2017-03-18T00:00:00.000Z"), endDate: new Date("2017-10-04T00:00:00.000Z") })]
        }),
        demographicFilter: new BrandVueApi.DemographicFilter({
            ageGroups: ["16-24", "25-39", "40-54", "55-74"],
            genders: ["F", "M"],
            regions: ["L", "N", "M", "S"],
            socioEconomicGroups: ["1", "2"]
        }),
        filterModel: filterModel,
        additionalMeasureFilters: [],
        includeSignificance: false,
        sigConfidenceLevel: SigConfidenceLevel.NinetyFive,
        dataRequest: new BrandVueApi.EntityInstanceRequest({
            type: "Satisfaction In-Store",
            entityInstanceIds: [73, 74]
        }),
        filterBy: measureFilters.map(filterInstance => new BrandVueApi.EntityInstanceRequest({
            type: filterInstance.measureName,
            entityInstanceIds: Object.values(filterInstance.entityInstances)[0]
        })),
        baseExpressionOverrides: []
    });
}
