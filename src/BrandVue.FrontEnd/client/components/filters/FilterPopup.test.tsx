import React from "react";
import { render, screen, within } from '@testing-library/react';
import userEvent from "@testing-library/user-event";
import "@testing-library/jest-dom";
import { FilterPopup } from "./FilterPopup";
import { dsession } from "../../dsession";
import { PageHandler } from "../PageHandler";
import { FilterValueMapping, MetricSet } from "../../metrics/metricSet";
import { filterSet as FilterSet } from "../../filter/filterSet";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { viewBase } from "../../core/viewBase";
import {IFilterStateCondensed, MetricFilterState} from "../../filter/metricFilterState";
import { filter } from "../../filter/filter";
import { Metric } from "../../metrics/metric";
import { EntityInstance } from "../../entity/EntityInstance";
import { filterItem } from "../../filter/filterItem";
import { AriaRoles } from "../../helpers/ReactTestingLibraryHelpers";
import {EntityConfiguration, IEntityConfiguration } from "../../entity/EntityConfiguration";
import { MixPanel } from "../mixpanel/MixPanel";
import { EntityType } from "../../BrandVueApi";
import * as EntityConfigurationStateContext from "../../entity/EntityConfigurationStateContext";
import {EntitySet} from "../../entity/EntitySet";
import {EntityInstanceGroup} from "../../entity/EntityInstanceGroup";
import {EntitySetAverageGroup} from "../../entity/EntitySetAverageGroup";
import { MixPanelModel } from "../mixpanel/MixPanelHelper";
import { TagManagerProvider } from "../../TagManagerContext";
import {useNavigate} from "react-router-dom";
import { IGoogleTagManager } from "client/googleTagManager";
import {mock} from "jest-mock-extended";


jest.mock('react-router-dom', () => ({
    useNavigate: jest.fn(),
    useLocation: jest.fn()
}));

const mockSetQueryParameters = jest.fn();

jest.mock("../helpers/UrlHelper", () => ({
    useWriteVueQueryParams: () => ({
        setQueryParameter: jest.fn(),
        setQueryParameters: mockSetQueryParameters
    }),
    useReadVueQueryParams: () => ({
        getQueryParameters: jest.fn(),
        getQueryParameter: jest.fn(),
    }),

}));


const GENDER = 'Gender'
const REGION = 'Region'
const SEG = 'Seg'
const YES_NO_QUESTION = "YnQ"
const AGE_QUESTION = "AgeQ"
const BRAND_QUESTION = "BrandQ"
const GROUP_QUESTION = "GroupQ"
const MALE = 'Male'
const FEMALE = 'Female'
const SOUTH = 'South'
const NORTH = 'North'
const SEG_ONE = 'Seg-1'
const SEG_TWO = 'Seg-2'
const SEG_THREE = 'Seg-3'
const YES = "yes"
const NO = "No"
const GOOD = "good"
const BAD = "Bad"

const BRAND_ONE = new EntityInstance(1, "brand-one");
const BRAND_TWO = new EntityInstance(2, "brand-two");
const BRAND_THREE = new EntityInstance(3, "brand-three");

const BRAND_ENTTIY_TYPE = new EntityType({
    identifier: "brand",
    displayNameSingular: "Brand",
    displayNamePlural: "Brands",
    isProfile: false,
    isBrand: true,
});

const RANGE_METRIC: Partial<Metric> = {
    name: AGE_QUESTION,
    entityCombination: [],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterValueMapping: [
        new FilterValueMapping("Range", "Range", ["Range"])
    ]
}

const RADIO_BUTTON_METRIC: Partial<Metric> = {
    name: YES_NO_QUESTION,
    entityCombination: [],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterValueMapping: [
        new FilterValueMapping(YES, YES, ["1"]),
        new FilterValueMapping(NO, NO, ["2,3"])
    ]
}

const WITH_BRAND_METRIC: Partial<Metric> = {
    name: BRAND_QUESTION,
    entityCombination: [BRAND_ENTTIY_TYPE],
    isProfileMetric: () => true,
    isBrandMetric: () => false,
    filterValueMapping: [
        new FilterValueMapping(GOOD, GOOD, []),
        new FilterValueMapping(BAD, BAD, [])
    ]
}

const ENTITY_INSTANCES = [BRAND_ONE, BRAND_TWO, BRAND_THREE] as EntityInstance[]
    
const ENTITY_CONFIGURATION =  new EntityConfiguration([ {
    EntityType: BRAND_ENTTIY_TYPE,
    EntitySets: [
        new EntitySet(1, BRAND_ENTTIY_TYPE, "All",
            new EntityInstanceGroup(ENTITY_INSTANCES), 
            false, false, BRAND_ONE as EntityInstance, 
            new EntitySetAverageGroup([]), false, false, ),
    ],
    DefaultEntitySetName: "brand",
    AllInstances: ENTITY_INSTANCES,
}], "brand", "All");

jest.mock("../../googleTagManager")
jest.mock("../helpers/UrlHelper")
jest.mock("../../DataSubsetManager")
const mockMixPanelClient = {
    init: jest.fn(),
    identify: jest.fn(),
    track: jest.fn(),
    reset: jest.fn(),
    setPeople: jest.fn()
  };
const mockedGoogleTagManager = mock<IGoogleTagManager>()


const setUpCompulsoryFilterSet = () => {
    const gender_options = [{ caption: MALE, idList: ['m'] } as filterItem, { caption: FEMALE, idList: ['f'] } as filterItem]
    const gender_filter: Partial<filter> = { name: GENDER, field: GENDER, initialDescription: 'test gender filter', filterItems: gender_options, getDefaultValue: () => gender_options.map(o => o.idList.join(",")) }

    const region_options = [{ caption: NORTH, idList: ['n'] } as filterItem, { caption: SOUTH, idList: ['s'] } as filterItem]
    const region_filter: Partial<filter> = { name: REGION, field: REGION, initialDescription: 'test region filter', filterItems: region_options, getDefaultValue: () => region_options.map(o => o.idList.join(",")) }

    const seg_options = [{ caption: SEG_ONE, idList: ['1'] } as filterItem, { caption: SEG_TWO, idList: ['2'] } as filterItem, { caption: SEG_THREE, idList: ['3'] } as filterItem]
    const seg_filter: Partial<filter> = { name: SEG, field: SEG, initialDescription: 'test seg filter', filterItems: seg_options, getDefaultValue: () => seg_options.map(o => o.idList.join(",")) }

    const filters = [gender_filter as filter, region_filter as filter, seg_filter as filter]
    const filterSet: Partial<FilterSet> = {
        filters: filters,
        filterLookup: filters.reduce((total, currentValue) => {
            total[currentValue.name] = currentValue
            return total
        }, {})
    }
    return filterSet as FilterSet
}

const combineMetricAndAdvancedMetricFiltersAsMetricSet = (metrics: Metric[] = [], advancedMetrics: Metric[] = []) => {
    const metricSet: Partial<MetricSet> = { metrics: metrics.concat(advancedMetrics) }
    return metricSet as MetricSet
}

const createMetricFilterState = (filterName:string, values: number[], metric: Metric, isAdvanced: boolean, isRange: boolean, entityInstances: {[entityType: string]: number[]}) : MetricFilterState => {
    var filterState = new MetricFilterState();
    filterState.name = filterName;
    filterState.values = values;
    filterState.metric = metric;
    filterState.isAdvanced = isAdvanced;
    filterState.isRange = isRange;
    filterState.entityInstances = entityInstances;
    filterState.description = (entityInstances: {[index: string]: number[]}, value: string, entityConfiguration: IEntityConfiguration | null) => JSON.stringify({entityInstances, value});
    return filterState;
}

const setUpStubbedMetricFiltersWithValuesFromMetrics = (metrics: Metric[] = [], advancedMetrics: Metric[] = []) => {    
    let metricFilters = metrics.map(m => 
        createMetricFilterState(m.name, [], m, false,   m.name === AGE_QUESTION,
            {})
    );
    return metricFilters.concat(advancedMetrics.map(m =>
            createMetricFilterState(m.name, [], m, true,   m.name === AGE_QUESTION,
                {})
    ));
}

const setUpMockedPageHandler = (metricSet: MetricSet, metricFilters: MetricFilterState[]) => {
    const pageHandler: Partial<PageHandler> = {
        getMetricFiltersFromArgs: jest.fn().mockImplementation((metric: Metric) => {
            return setUpStubbedMetricFiltersWithValuesFromMetrics([metric])[0]
        }),
        getMetricFilters: jest.fn().mockImplementation((mSet: MetricSet) => {
            expect(JSON.stringify(mSet.metrics.map(m => m.name).sort())).toEqual(JSON.stringify(metricSet.metrics!.map(m => m.name).sort()))
            return metricFilters;
        }),
        getGroupMetricFilters: jest.fn().mockImplementation((mSet: MetricSet) => {
            return [];
        })
    }

    return pageHandler as PageHandler
}


const setUpSession = (filterSet: FilterSet, pageHandler: PageHandler, curatedFilters: CuratedFilters) => {
    const session: Partial<dsession> = {
        activeView: { curatedFilters: curatedFilters } as viewBase,
        filters: filterSet,
        pageHandler: pageHandler
    }
    return session as dsession
}

const renderFilterPopUpWithFilters = (metrics: Metric[] = [], advancedMetrics: Metric [] = []) => {
    const filterSet = setUpCompulsoryFilterSet()
    const metricSet = combineMetricAndAdvancedMetricFiltersAsMetricSet(metrics, advancedMetrics)
    const metricFilters = setUpStubbedMetricFiltersWithValuesFromMetrics(metrics, advancedMetrics)
    const pageHandler = setUpMockedPageHandler(metricSet, metricFilters)
    const curatedFilters = new CuratedFilters(filterSet, ENTITY_CONFIGURATION, metricFilters)
    const session = setUpSession(filterSet, pageHandler, curatedFilters)
    const mixPanelModelInstance: MixPanelModel = {
        userId: "userIdTest",
        projectId: "mixPanelTokenTest",
        client: mockMixPanelClient,
        isAllVue: false,
        productName: "BrandVue",
        project: "subProductIdTest",
        kimbleProposalId:"",
    };

    MixPanel.init(mixPanelModelInstance);

    render(
        <TagManagerProvider>
                <FilterPopup
                        filters={session.filters}
                        activeView={session.activeView}
                        metrics={metricSet}
                        entityConfiguration={ENTITY_CONFIGURATION}
                        pageHandler={session.pageHandler}
                        googleTagManager={mockedGoogleTagManager} /></TagManagerProvider>);
    return pageHandler;
}

jest.spyOn(EntityConfigurationStateContext, "useEntityConfigurationStateContext")
    .mockImplementation(() => ({ 
        entityConfiguration: ENTITY_CONFIGURATION,
        hasEntityConfigurationLoaded: true, 
        entityConfigurationDispatch: () => Promise.resolve() })
    );

describe("Check Filter Pop up renders correctly", () => {
    const user = userEvent.setup()
    const mockNavigate = jest.fn();
    
    beforeEach(() => {
        (useNavigate as jest.Mock).mockReturnValue(mockNavigate);
        jest.clearAllMocks()
    });

    it("Check filter button rendered and modal is hidden when first rendered", async () => {
        renderFilterPopUpWithFilters()

        const buttonElement = screen.queryByRole(AriaRoles.BUTTON);
        expect(buttonElement).toBeVisible();

        const buttonTextElement = within(buttonElement!).queryByText('Filters')
        expect(buttonTextElement).toBeVisible();

        let modalElement = screen.queryByRole(AriaRoles.MODAL);
        expect(modalElement).toBeNull();
    });

    it("Click filters button opens modal and rendered correctly", async () => {
        renderFilterPopUpWithFilters()

        const buttonElement = screen.getByRole(AriaRoles.BUTTON);
        await user.click(buttonElement)

        let modalElement = screen.queryByRole(AriaRoles.MODAL);
        expect(modalElement).toBeVisible();
        expect(mockMixPanelClient.track).toHaveBeenCalledWith('Filters Opened', MixPanel.Props['filtersOpened']);
    });


    it("test compulsory gender, region and seg filters render correctly", async () => {
        renderFilterPopUpWithFilters()
        await user.click(screen.getByRole(AriaRoles.BUTTON))

        const genderTextElement = screen.queryByText(GENDER);
        expect(genderTextElement).toBeVisible();
        const maleCheckboxElement = screen.queryByRole(AriaRoles.CHECKBOX, { name: MALE })
        const femaleCheckboxElement = screen.queryByRole(AriaRoles.CHECKBOX, { name: FEMALE })
        expect(maleCheckboxElement).toBeVisible();
        expect(femaleCheckboxElement).toBeVisible();

        const regionTextElement = screen.queryByText(REGION);
        expect(regionTextElement).toBeVisible();
        const northCheckboxElement = screen.queryByRole(AriaRoles.CHECKBOX, { name: NORTH })
        const southCheckboxElement = screen.queryByRole(AriaRoles.CHECKBOX, { name: SOUTH })
        expect(northCheckboxElement).toBeVisible();
        expect(southCheckboxElement).toBeVisible();

        const segTextElement = screen.queryByText(SEG);
        expect(segTextElement).toBeVisible();
        const segOneCheckboxElement = screen.queryByRole(AriaRoles.CHECKBOX, { name: SEG_ONE })
        const segTwoCheckboxElement = screen.queryByRole(AriaRoles.CHECKBOX, { name: SEG_TWO })
        const segThreeCheckboxElement = screen.queryByRole(AriaRoles.CHECKBOX, { name: SEG_THREE })
        expect(segOneCheckboxElement).toBeVisible();
        expect(segTwoCheckboxElement).toBeVisible();
        expect(segThreeCheckboxElement).toBeVisible();
    });


    it("check range filter renders correctly", async () => {
        renderFilterPopUpWithFilters([RANGE_METRIC as Metric])
        await user.click(screen.getByRole(AriaRoles.BUTTON))

        const rangeQuestionText = screen.queryByText(AGE_QUESTION);
        expect(rangeQuestionText).toBeVisible();

        const rangeDropdown = screen.queryByLabelText("Option drop down for: Range type selector")
        expect(rangeDropdown).toBeVisible()

        const minValueInput = screen.queryByRole(AriaRoles.SPIN_BUTTON, { name: "Min range value" })
        expect(minValueInput).toBeVisible()

        const maxValueInput = screen.queryByRole(AriaRoles.SPIN_BUTTON, { name: "Max range value" })
        expect(maxValueInput).toBeVisible()
    });


    it("check radio button filter renders correctly", async () => {
        renderFilterPopUpWithFilters([RADIO_BUTTON_METRIC as Metric])

        await user.click(screen.getByRole(AriaRoles.BUTTON))

        const rangeQuestionText = screen.queryByText(YES_NO_QUESTION);
        expect(rangeQuestionText).toBeVisible();

        const yesRadioButton = screen.queryByRole(AriaRoles.RADIO_BUTTON, { name: YES })
        expect(yesRadioButton).toBeVisible()

        const noRadioButton = screen.queryByRole(AriaRoles.RADIO_BUTTON, { name: NO })
        expect(noRadioButton).toBeVisible()
    });


    it("check brand filter renders correctly", async () => {
        renderFilterPopUpWithFilters([WITH_BRAND_METRIC as Metric])

        await user.click(screen.getByRole(AriaRoles.BUTTON))

        const rangeQuestionText = screen.queryByText(BRAND_QUESTION);
        expect(rangeQuestionText).toBeVisible();

        const rangeDropdown = screen.queryByLabelText("Option drop down for: Brand selector");
        expect(rangeDropdown).toBeVisible()

        const goodRadioButton = screen.queryByRole(AriaRoles.RADIO_BUTTON, { name: GOOD })
        expect(goodRadioButton).toBeVisible()

        const badRadioButton = screen.queryByRole(AriaRoles.RADIO_BUTTON, { name: BAD })
        expect(badRadioButton).toBeVisible()
    });


    it("click advanced filters and correct filters show", async () => {
        renderFilterPopUpWithFilters([RANGE_METRIC as Metric], [WITH_BRAND_METRIC as Metric, RADIO_BUTTON_METRIC as Metric])

        await user.click(screen.getByRole(AriaRoles.BUTTON))

        let rangeText = screen.getByText(AGE_QUESTION);
        expect(rangeText).toBeVisible();

        let BrandText = screen.queryByText(BRAND_QUESTION);
        expect(BrandText).toBeNull();

        let radioButtonText = screen.queryByText(YES_NO_QUESTION);
        expect(radioButtonText).toBeNull();

        await user.click(screen.getByText('Advanced'))

        rangeText = screen.getByText(AGE_QUESTION);
        expect(rangeText).toBeVisible();

        BrandText = screen.queryByText(BRAND_QUESTION);
        expect(BrandText).toBeVisible();

        radioButtonText = screen.queryByText(YES_NO_QUESTION);
        expect(radioButtonText).toBeVisible();
    });

    it("Can select brand for advanced filter", async () => {
        renderFilterPopUpWithFilters([], [WITH_BRAND_METRIC as Metric]);
        await user.click(screen.getByRole(AriaRoles.BUTTON));
        await user.click(screen.getByText('Advanced'));
        await user.click(screen.getByText('Select...'));
        await user.click(screen.getByText(BRAND_TWO.name!));

        const SelectedBrandTwo = screen.queryByText(BRAND_TWO.name!);
        expect(SelectedBrandTwo).toBeVisible();
    })

    it("check selecting filters works", async () => {
        renderFilterPopUpWithFilters()

        await user.click(screen.getByRole(AriaRoles.BUTTON))

        await user.click(screen.getByRole(AriaRoles.CHECKBOX, { name: MALE }))

        const cancelGender = screen.queryByTitle(`Clear '${GENDER}' filter`)
        expect(cancelGender).toBeVisible()
    });


    it("test apply brand filter updates the correct query parameters and google events fired", async () => {
        const pageHandler = renderFilterPopUpWithFilters()
        
        await user.click(screen.getByRole(AriaRoles.BUTTON))

        await user.click(screen.getByRole(AriaRoles.CHECKBOX, { name: MALE }))

        await user.click(screen.getByRole(AriaRoles.BUTTON, { name: "Apply filters" }))

        expect(mockSetQueryParameters).toBeCalledTimes(1)
        expect(mockSetQueryParameters.mock.calls[0][0]).toContainEqual({ "name": "Gender", "value": ["m"] })
        expect(mockedGoogleTagManager.addEvent).toBeCalledTimes(2)
        expect(mockedGoogleTagManager.addEvent).toHaveBeenCalledWith("applyFilter", expect.objectContaining(pageHandler));
    });


    it("test apply metric filter updates the correct query parameters and google event fired", async () => {
        const pageHandler = renderFilterPopUpWithFilters([RADIO_BUTTON_METRIC as Metric])
        await user.click(screen.getByRole(AriaRoles.BUTTON))

        await user.click(screen.getByRole(AriaRoles.RADIO_BUTTON, { name: YES }))

        await user.click(screen.getByRole(AriaRoles.BUTTON, { name: "Apply filters" }))

        expect(mockSetQueryParameters).toBeCalledTimes(1)
        expect(mockSetQueryParameters.mock.calls[0][0]).toEqual([
            { "name": "f" + YES_NO_QUESTION, "value": JSON.stringify({ v: [1], i: false, e: {}, r: false } as IFilterStateCondensed) }, 
            { "name": YES_NO_QUESTION, "value": "" },
            { "name": "Gender", "value": [] },
            { "name": "Region", "value": [] },
            { "name": "Seg", "value": [] },

        ]);
        expect(mockedGoogleTagManager.addEvent).toBeCalledTimes(2)
        expect(mockedGoogleTagManager.addEvent).toHaveBeenCalledWith("applyFilter", expect.objectContaining(pageHandler))
    });
});