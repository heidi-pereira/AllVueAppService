import { dsession } from "../dsession";
import defineProperty from "./defineProperty";
import {
    IAverageDescriptor,
    AverageDescriptor,
    MakeUpTo,
    PageDescriptor,
    IEntityType,
    IEntityTypeConfiguration,
    EntityType,
    DemographicFilter,
    MeasureFilterRequestModel,
    CompositeFilterModel,
    ComparisonPeriodSelection,
    WeightAcross, 
    WeightingMethod,
    WeightingPeriodUnit, 
    TotalisationPeriodUnit, 
    AverageStrategy
} from "../BrandVueApi";
import { filterSet } from "../filter/filterSet";
import { FilterValueMapping, MetricSet } from "../metrics/metricSet";
import { EntityInstance } from "../entity/EntityInstance";
import { initialisePages } from "../components/helpers/PagesHelper";
import { EntityInstanceGroup } from "../entity/EntityInstanceGroup";
import { EntitySetAverageGroup } from "../entity/EntitySetAverageGroup";
import { EntitySet } from "../entity/EntitySet";
import { Metric } from "../metrics/metric";
import { EntityConfiguration, IEntityConfigurationModel } from "../entity/EntityConfiguration";
import { CuratedFilters } from "../filter/CuratedFilters";
import * as BrandVueApi from "../BrandVueApi";
import { IFilterState } from "client/filter/metricFilterState";

export type SessionEnrichmentOptions = { averageType: string; selectedView?: number };

const YES = "Yes";
const NO = "No";
const GOOD = "Good";
const BAD = "Bad";

export const YES_NO_QUESTION = "DoYouLike";
export const BRAND_QUESTION = "ExperienceRating";
export const IS_GOOD_QUESTION = "IsBrandGood";
export const IS_FUN_QUESTION = "IsBrandFun";
export const IS_PURPLE_QUESTION = "IsBrandPurple";
export const IMAGE_METRIC = "ImageAttributes";

export const brandEntityType = new EntityType({
    identifier: "brand",
    displayNameSingular: "Brand",
    displayNamePlural: "Brands",
    isProfile: false,
    isBrand: true,
});

export const productEntityType = new EntityType({
    identifier: "product",
    displayNameSingular: "Product",
    displayNamePlural: "Products",
    isProfile: false,
    isBrand: false,
});

export const imageEntityType = new EntityType({
    identifier: "image",
    displayNameSingular: "Image",
    displayNamePlural: "Images",
    isProfile: false,
    isBrand: false,
});

const RadioButtonMetric = new Metric(null, {
    name: YES_NO_QUESTION,
    entityCombination: [],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterGroup: "",
    filterValueMapping: [new FilterValueMapping(YES, YES, ["1"]), new FilterValueMapping(NO, NO, ["2,3"])],
});

const BrandMetric = new Metric(null, {
    name: BRAND_QUESTION,
    entityCombination: [brandEntityType],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterGroup: "",
    filterValueMapping: [new FilterValueMapping(GOOD, GOOD, []), new FilterValueMapping(BAD, BAD, [])],
});

const BrandMetric2 = new Metric(null, {
    name: IS_GOOD_QUESTION,
    entityCombination: [brandEntityType],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterGroup: "",
    filterValueMapping: [new FilterValueMapping(GOOD, GOOD, []), new FilterValueMapping(BAD, BAD, [])],
});

const BrandMetric3 = new Metric(null, {
    name: IS_FUN_QUESTION,
    entityCombination: [brandEntityType],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterGroup: "",
    filterValueMapping: [new FilterValueMapping(GOOD, GOOD, []), new FilterValueMapping(BAD, BAD, [])],
});

const BrandMetric4 = new Metric(null, {
    name: IS_PURPLE_QUESTION,
    entityCombination: [brandEntityType],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterGroup: "",
    filterValueMapping: [new FilterValueMapping(GOOD, GOOD, []), new FilterValueMapping(BAD, BAD, [])],
});

const ImageMetric = new Metric(null, {
    name: IMAGE_METRIC,
    entityCombination: [brandEntityType, imageEntityType],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterGroup: "",
    filterValueMapping: [new FilterValueMapping(GOOD, GOOD, []), new FilterValueMapping(BAD, BAD, [])],
});

export const enrichSession = (session: dsession, options: SessionEnrichmentOptions, additionalOptions?: ((session: dsession) => void)[]) => {
    const mockFilters = MockCuratedFilters as any as CuratedFilters;
    mockFilters.average = AverageDescriptor.fromJS(session.averages.find((a) => a.averageId === options.averageType));
    const mockActiveView = new MockActiveView();
    defineProperty(session, mockActiveView, "activeView");
    defineProperty(mockActiveView, mockFilters, "curatedFilters");
    session.coreViewType = options.selectedView || 0;
    if (additionalOptions && additionalOptions.length > 0) {
        additionalOptions.forEach((o) => o(session));
    }
    const startPage = new PageDescriptor();
    startPage.startPage = true;
    startPage.name = "Start";
    startPage.displayName = "Start";
    session.pages = [startPage];
    initialisePages(session.pages);
    session.filters = new filterSet();
    session.activeView.activeMetrics = MockApplication.allMetrics.metrics;
};

export class MockApplication {
    constructor(public readonly session: dsession) {}

    public static mainBrand = new EntityInstance(0, "00 - Main brand");
    public static brand1 = new EntityInstance(1, "01 - Brand One");
    public static brand2 = new EntityInstance(2, "02 - Brand Two");
    public static brand3 = new EntityInstance(3, "03 - Brand Three");
    static initBrandGroup = new EntityInstanceGroup([MockApplication.mainBrand, MockApplication.brand1, MockApplication.brand2]);
    static sectorBrandGroup = new EntityInstanceGroup([MockApplication.mainBrand, MockApplication.brand1, MockApplication.brand2, MockApplication.brand3]);
    static averageGroup = new EntitySetAverageGroup([]);

    public static averages = [
        new AverageDescriptor({
            averageId: 'Monthly',
            displayName: 'Monthly',
            order: 1,
            group: [],
            totalisationPeriodUnit: TotalisationPeriodUnit.Month,
            numberOfPeriodsInAverage: 1,
            weightingMethod: WeightingMethod.QuotaCell,
            weightAcross: WeightAcross.SinglePeriod,
            averageStrategy: AverageStrategy.OverAllPeriods,
            makeUpTo: MakeUpTo.MonthEnd,
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
        }),
        new AverageDescriptor({
            averageId: 'Quarterly',
            displayName: 'Quarterly',
            order: 2,
            group: [],
            totalisationPeriodUnit: TotalisationPeriodUnit.Month,
            numberOfPeriodsInAverage: 3,
            weightingMethod: WeightingMethod.QuotaCell,
            weightAcross: WeightAcross.SinglePeriod,
            averageStrategy: AverageStrategy.OverAllPeriods,
            makeUpTo: MakeUpTo.QuarterEnd,
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
        }),
        new AverageDescriptor({
            averageId: 'Annual',
            displayName: 'Annual',
            order: 3,
            group: [],
            totalisationPeriodUnit: TotalisationPeriodUnit.Month,
            numberOfPeriodsInAverage: 12,
            weightingMethod: WeightingMethod.QuotaCell,
            weightAcross: WeightAcross.SinglePeriod,
            averageStrategy: AverageStrategy.OverAllPeriods,
            makeUpTo: MakeUpTo.CalendarYearEnd,
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
        })
    ]
    
    public static averageFilter = (average: IAverageDescriptor) =>
        average.makeUpTo === MakeUpTo.MonthEnd || average.makeUpTo === MakeUpTo.QuarterEnd || average.makeUpTo === MakeUpTo.CalendarYearEnd;
    public static otherEntitySetId = 5;
    public static brandEntityType = brandEntityType;
    public static productEntityType = productEntityType;
    public static defaultBrandSet = new EntitySet(
        1,
        brandEntityType,
        "Test brand set",
        MockApplication.initBrandGroup,
        false,
        true,
        MockApplication.mainBrand,
        MockApplication.averageGroup
    );
    public static sectorBrandSet = new EntitySet(
        2,
        brandEntityType,
        "Sector brand set",
        MockApplication.sectorBrandGroup,
        true,
        false,
        MockApplication.mainBrand,
        MockApplication.averageGroup
    );
    public static otherBrandSet = new EntitySet(
        MockApplication.otherEntitySetId,
        brandEntityType,
        "First",
        new EntityInstanceGroup([]),
        false,
        false,
        MockApplication.mainBrand,
        MockApplication.averageGroup
    );
    public static mainProduct = new EntityInstance(0, "00 - Main product");
    public static product1 = new EntityInstance(1, "01 - Product One");
    public static nonOverlappingProduct1 = new EntityInstance(11, "11 - Product Two");
    public static nonOverlappingProduct2 = new EntityInstance(12, "12 - Product Three");
    public static overlappingProductGroup = new EntityInstanceGroup([MockApplication.mainProduct, MockApplication.product1]);
    public static nonOverlappingProductGroup = new EntityInstanceGroup([MockApplication.nonOverlappingProduct1, MockApplication.nonOverlappingProduct2]);
    public static defaultProductSet = new EntitySet(
        2,
        productEntityType,
        "Products",
        new EntityInstanceGroup([MockApplication.mainProduct]),
        false,
        true,
        MockApplication.mainProduct,
        MockApplication.averageGroup
    );
    public static overlappingProductSet = new EntitySet(
        3,
        productEntityType,
        "Test product set",
        MockApplication.overlappingProductGroup,
        false,
        false,
        MockApplication.mainProduct,
        MockApplication.averageGroup
    );
    public static nonOverlappingProductSet = new EntitySet(
        4,
        productEntityType,
        "Test product set 2",
        MockApplication.nonOverlappingProductGroup,
        false,
        false,
        MockApplication.nonOverlappingProduct2,
        MockApplication.averageGroup
    );
    public static allMetrics = new MetricSet({ metrics: [RadioButtonMetric, BrandMetric, BrandMetric2, BrandMetric3, BrandMetric4, ImageMetric] });
    public static mockEntityModels: IEntityConfigurationModel[] = [
        {
            EntityType: brandEntityType,
            EntitySets: [MockApplication.defaultBrandSet, MockApplication.sectorBrandSet, MockApplication.otherBrandSet],
            DefaultEntitySetName: MockApplication.defaultBrandSet.name,
            AllInstances: [MockApplication.mainBrand, MockApplication.brand1, MockApplication.brand2, MockApplication.brand3],
        },
        {
            EntityType: imageEntityType,
            EntitySets: [new EntitySet(1, imageEntityType, "image", new EntityInstanceGroup([]), false, false, undefined, MockApplication.averageGroup)],
            DefaultEntitySetName: "image",
            AllInstances: [],
        },
        {
            EntityType: productEntityType,
            EntitySets: [MockApplication.defaultProductSet, MockApplication.nonOverlappingProductSet, MockApplication.overlappingProductSet],
            DefaultEntitySetName: MockApplication.defaultProductSet.name,
            AllInstances: [
                MockApplication.mainProduct,
                MockApplication.product1,
                MockApplication.nonOverlappingProduct1,
                MockApplication.nonOverlappingProduct2,
            ],
        },
    ];
    public static mockEntityConfiguration = new EntityConfiguration(MockApplication.mockEntityModels, "brand", "All");

    enrichSession(options: SessionEnrichmentOptions, additionalOptions?: ((session: dsession) => void)[]): void {
        enrichSession(this.session, options, additionalOptions);
    }
}

class MockCuratedFilter {
    constructor() {
        this._demographicFilter = new DemographicFilter();
        this._filterDescriptions = {};
        this.measureFilters = [];
    }
    _startDate: Date;
    _endDate: Date;

    average: IAverageDescriptor;
    private _demographicFilter: DemographicFilter;
    measureFilters: MeasureFilterRequestModel[];

    _filterDescriptions: { [key: string]: { name: string; filter: string } };

    private readonly _compositeFilters: BrandVueApi.CompositeFilterModel[] = [];
    get demographicFilter(): BrandVueApi.DemographicFilter {
        return this._demographicFilter;
    }
    get compositeFilters(): CompositeFilterModel[] {
        return this._compositeFilters;
    }
    get filterDescriptions(): { [key: string]: { name: string; filter: string } } {
        return this._filterDescriptions;
    }
    get startDate(): Date {
        return this._startDate;
    }
    get endDate(): Date {
        return this._endDate;
    }
    public setDates(startDate: Date, endDate: Date) {
        this._startDate = startDate;
        this._endDate = endDate;
    }
    public setEndDate(endDate: Date) {
        this._endDate = endDate;
    }

    private _average: IAverageDescriptor;
    private _comparisonPeriodSelection: ComparisonPeriodSelection;
    private _measureFilters: MeasureFilterRequestModel[];
    private _scorecardAverage: IAverageDescriptor;
}

export class MockActiveView {
    activeMetrics: Metric[] = [BrandMetric, ImageMetric];
    curatedFilters: CuratedFilters;
    activeBrand: EntityInstance = MockApplication.mainBrand;
    getEntityCombination(): IEntityType[] {
        return [brandEntityType, imageEntityType];
    }
}

export const MockCuratedFilters = new MockCuratedFilter();

export const createEntityConfiguration = (entityType: IEntityType, id: number = 1): IEntityTypeConfiguration => {
    return {
        id: id,
        identifier: entityType.identifier,
        displayNameSingular: entityType.displayNameSingular,
        displayNamePlural: entityType.displayNamePlural,
        surveyChoiceSetNames: [],
        createdFrom: undefined,
        subProductId: "1",
        productShortCode: "survey",
    };
};
