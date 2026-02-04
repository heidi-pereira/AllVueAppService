import * as SurveyVueUtils from './SurveyVueUtils';
import * as BrandVueApi from '../../BrandVueApi';
import { ApplicationConfiguration } from "client/ApplicationConfiguration";
import { IAverageDescriptor, AverageDescriptor, AverageStrategy, MakeUpTo, TotalisationPeriodUnit, WeightAcross, WeightingMethod, WeightingPeriodUnit } from "client/BrandVueApi";
import { getUserVisibleAverages } from "client/components/helpers/SurveyVueUtils";

function makeAverage(overrides: Partial<AverageDescriptor>): IAverageDescriptor {
    return new AverageDescriptor({
        averageId: '1',
        displayName: 'Test Average',
        order: 1,
        group: [],
        totalisationPeriodUnit: TotalisationPeriodUnit.Day,
        numberOfPeriodsInAverage: 1,
        weightingMethod: WeightingMethod.QuotaCell,
        weightAcross: WeightAcross.SinglePeriod,
        averageStrategy: AverageStrategy.OverAllPeriods,
        makeUpTo: MakeUpTo.Day,
        weightingPeriodUnit: WeightingPeriodUnit.SameAsTotalization,
        includeResponseIds: false,
        internalIndex: 0,
        isDefault: false,
        allowPartial: false,
        authCompanyShortCode: "",
        isHiddenFromUsers: false,
        subset: [],
        environment: [],
        roles: [],
        disabled: false,
        ...overrides
    });
}

function makeMockApplicationConfiguration(startDate: Date, endDate: Date): ApplicationConfiguration {
    return {
        hasLoadedData: true,
        dateOfFirstDataPoint: startDate,
        dateOfLastDataPoint: endDate,
    } as ApplicationConfiguration;
}

describe("getUserVisibleAverages", () => {
    const april1 = new Date("2024-04-01T00:00:00Z");
    const april30 = new Date("2024-04-30T23:59:59Z");

    it("includes a day average if there is enough data", () => {
        const avg = makeAverage({ numberOfPeriodsInAverage: 5, totalisationPeriodUnit: TotalisationPeriodUnit.Day });
        const appConfig = makeMockApplicationConfiguration(april1, april30);
        const result = getUserVisibleAverages(appConfig, [avg], true, "subset");
        expect(result).toContain(avg);
    });

    it("excludes a day average if there is not enough data", () => {
        const avg = makeAverage({ numberOfPeriodsInAverage: 40, totalisationPeriodUnit: TotalisationPeriodUnit.Day });
        const appConfig = makeMockApplicationConfiguration(april1, april30);
        const result = getUserVisibleAverages(appConfig, [avg], true, "subset");
        expect(result).not.toContain(avg);
    });

    it("includes 'All' period regardless of available data", () => {
        const avg = makeAverage({ totalisationPeriodUnit: TotalisationPeriodUnit.All, numberOfPeriodsInAverage: 1 });
        const appConfig = makeMockApplicationConfiguration(april1, april30);
        const result = getUserVisibleAverages(appConfig, [avg], true, "subset");
        expect(result).toContain(avg);
    });

    it("filters out hidden averages", () => {
        const avg = makeAverage({ isHiddenFromUsers: true });
        const appConfig = makeMockApplicationConfiguration(april1, april30);
        const result = getUserVisibleAverages(appConfig, [avg], true, "subset");
        expect(result).not.toContain(avg);
    });

    it("filters by weighting option", () => {
        const avgWeighted = makeAverage({ weightingMethod: WeightingMethod.QuotaCell });
        const avgUnweighted = makeAverage({ weightingMethod: WeightingMethod.None, averageId: '2' });

        const appConfig = makeMockApplicationConfiguration(april1, april30);

        // When isDataWeighted is true, only QuotaCell averages are included
        expect(getUserVisibleAverages(appConfig, [avgWeighted, avgUnweighted], true, "subset")).toContain(avgWeighted);
        expect(getUserVisibleAverages(appConfig, [avgWeighted, avgUnweighted], true, "subset")).not.toContain(avgUnweighted);

        // When isDataWeighted is false, only None averages are included
        expect(getUserVisibleAverages(appConfig, [avgWeighted, avgUnweighted], false, "subset")).toContain(avgUnweighted);
        expect(getUserVisibleAverages(appConfig, [avgWeighted, avgUnweighted], false, "subset")).not.toContain(avgWeighted);
    });

    it("includes a month average only if there is enough data", () => {
        // Not enough data for 1 month: needs to go back to March 30, but first data is April 1
        const avg = makeAverage({ totalisationPeriodUnit: TotalisationPeriodUnit.Month, numberOfPeriodsInAverage: 1 });
        const appConfig = makeMockApplicationConfiguration(april1, april30);
        expect(getUserVisibleAverages(appConfig, [avg], true, "subset")).not.toContain(avg);

        // Enough data for 1 month: data starts at March 30
        const appConfig2 = makeMockApplicationConfiguration(new Date("2024-03-30T00:00:00Z"), april30);
        expect(getUserVisibleAverages(appConfig2, [avg], true, "subset")).toContain(avg);
    });

        
    describe('getErrorMessage', () => {
        const defaultMessage = 'An error occurred';
        let isSwaggerSpy: jest.SpyInstance | undefined;

        afterEach(() => {
            if (isSwaggerSpy) {
                isSwaggerSpy.mockRestore();
                isSwaggerSpy = undefined;
            }
            jest.restoreAllMocks();
        });

        test('returns default for null/undefined', () => {
            expect(SurveyVueUtils.getErrorMessage(null)).toBe(defaultMessage);
            expect(SurveyVueUtils.getErrorMessage(undefined)).toBe(defaultMessage);
        });

        test('returns plain string', () => {
            expect(SurveyVueUtils.getErrorMessage('I <3 errors')).toBe('I <3 errors');
        });

        test('returns message from Error instance', () => {
            expect(SurveyVueUtils.getErrorMessage(new Error('err1'))).toBe('err1');
        });

        test('returns message from object.message when string', () => {
            expect(SurveyVueUtils.getErrorMessage({ message: 'obj msg' })).toBe('obj msg');
        });

        test('stringifies object.message when it is an object', () => {
            const nested = { message: { inner: 'ok', code: 42 } };
            expect(SurveyVueUtils.getErrorMessage(nested)).toBe(JSON.stringify(nested.message));
        });

        test('object without message returns default', () => {
            expect(SurveyVueUtils.getErrorMessage({ foo: 'bar' })).toBe(defaultMessage);
        });

        describe('SwaggerException parsing', () => {
            test('parses response that is JSON string and returns string value', () => {
                const swagger = { response: JSON.stringify('a swagger string') } as any;
                isSwaggerSpy = jest.spyOn(BrandVueApi.SwaggerException, 'isSwaggerException' as any).mockImplementation(() => true);
                const original = BrandVueApi.SwaggerException as any;
                // call with object that will be treated as swagger exception
                expect(SurveyVueUtils.getErrorMessage(swagger)).toBe('a swagger string');
            });

            test('parses response JSON with detail field', () => {
                const swagger = { response: JSON.stringify({ detail: 'detail text' }) } as any;
                isSwaggerSpy = jest.spyOn(BrandVueApi.SwaggerException, 'isSwaggerException' as any).mockImplementation(() => true);
                expect(SurveyVueUtils.getErrorMessage(swagger)).toBe('detail text');
            });

            test('invalid JSON response falls back to raw response string', () => {
                const swagger = { response: 'not json' } as any;
                isSwaggerSpy = jest.spyOn(BrandVueApi.SwaggerException, 'isSwaggerException' as any).mockImplementation(() => true);
                expect(SurveyVueUtils.getErrorMessage(swagger)).toBe('not json');
            });
        });
    });

});