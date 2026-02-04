import { MetricFilterState } from './metricFilterState';
import { Metric } from '../metrics/metric';
import { FilterValueMapping } from '../metrics/metricSet';
import { EntityType, IEntityType } from '../BrandVueApi';

describe('MetricFilterState withX methods', () => {
    const type1 = createEntityType('type1');
    const filterValueMapping1Yes = new FilterValueMapping("Yes", "1:Yes", ["1"]);
    let metric: Metric;
    let initialState: MetricFilterState;

    beforeEach(() => {
        metric = new Metric({});
        initialState = new MetricFilterState();
        initialState.metric = metric;
        initialState.entityInstances = {};
        initialState.values = [];
        initialState.invert = false;
        initialState.treatPrimaryValuesAsRange = false;
        initialState.isAdvanced = true;
        initialState.isRange = false;
    });

    it('should clear the filter state', () => {
        const newState = initialState.withCleared();
        expect(newState.values).toEqual([]);
        expect(newState.entityInstances).toEqual({});
    });

    it('should update the filter state with a single entity instance', () => {
        const entityInstanceType = 'type1';
        const entityInstanceId = '1';
        const newState = initialState.withInstance(entityInstanceType, entityInstanceId);
        expect(newState.entityInstances[entityInstanceType]).toEqual([1]);
    });

    it('should update the filter state with multiple entity instances', () => {
        metric.filterValueMapping = [filterValueMapping1Yes];
        metric.entityCombination = [type1];
        const entityInstanceIds = [1, 2, 3];
        const newState = initialState.withInstances(type1.identifier, entityInstanceIds, false);
        expect(newState.entityInstances[type1.identifier]).toEqual(entityInstanceIds);
    });

    it('should update the filter state with a value', () => {
        metric.filterValueMapping = [filterValueMapping1Yes];
        metric.entityCombination = [type1];
        const value = '1,2,3';
        const newState = initialState.withValues(value);
        expect(newState.values).toEqual([1, 2, 3]);
    });

    it('should update the filter state with an inverted value', () => {
        metric.filterValueMapping = [filterValueMapping1Yes];
        metric.entityCombination = [type1];
        const value = '!1,2,3';
        const newState = initialState.withValues(value);
        expect(newState.values).toEqual([1, 2, 3]);
        expect(newState.invert).toBe(true);
    });

    it('should update the filter state with a range', () => {
        initialState.isRange = true;
        initialState.treatPrimaryValuesAsRange = true;
        const min = 1;
        const max = 10;
        const newState = initialState.withRange(min, max);
        expect(newState.values).toEqual([1, 10]);
    });

    it('should update the filter state with a range and treat primary values as range', () => {
        initialState.isRange = true;
        initialState.treatPrimaryValuesAsRange = true;
        const min = 45;
        const max = 67;
        const newState = initialState.withRange(min, max);
        expect(newState.values).toEqual([45, 67]);
    });


    it('should update the filter state with a single entity instance and metric', () => {
        metric = new Metric(null, {
            name: "Advertising Awareness",
            filterValueMapping: [new FilterValueMapping("Aware", "1", ["1"]), new FilterValueMapping("Not aware", "!1", ["!1"])],
            entityCombination: [createEntityType("brand", true)]
        });
        const entityInstanceType = 'brand';
        const entityInstanceId = '321';
        const newState = initialState.withInstance(entityInstanceType, entityInstanceId);
        expect(newState.entityInstances[entityInstanceType]).toEqual([321]);
    });

    it('should update the filter state with a value and metric', () => {
        metric = new Metric(null, {
            name: "Advertising Awareness",
            filterValueMapping: [new FilterValueMapping("Aware", "1", ["1"]), new FilterValueMapping("Not aware", "!1", ["!1"])],
            entityCombination: [createEntityType("brand", true)]
        });
        initialState.metric = metric;
        const value = '1';
        const newState = initialState.withValues(value);
        expect(newState.values).toEqual([1]);
    });
    
    it('should update the filter state with a range and metric', () => {
        metric = new Metric({
            name: "Annual Household Income in £s: 000s",
            filterValueMapping: [new FilterValueMapping("Range", "Range", ["Range"])],
            entityCombination: []
        });
        initialState.metric = metric;
        initialState.isRange = true;
        initialState.treatPrimaryValuesAsRange = true;
        const min = 6;
        const max = 34;
        const newState = initialState.withRange(min, max);
        expect(newState.values).toEqual([6, 34]);
    });

    it('should update the filter state with multiple entity instances and metric', () => {
        metric = new Metric({
            name: "Product Penetration",
            filterValueMapping: [new FilterValueMapping("Yes", "1", ["1"])],
            entityCombination: [createEntityType("brand", true), createEntityType("product", false)]
        });
        initialState.metric = metric;
        const entityInstanceType = 'product';
        const entityInstanceIds = [1];
        const newState = initialState.withInstances(entityInstanceType, entityInstanceIds, false);
        expect(newState.entityInstances[entityInstanceType]).toEqual(entityInstanceIds);
    });
});

function createEntityType(identifier: string, isBrand = false, isProfile = false): IEntityType {
    return new EntityType({ identifier, isBrand: isBrand, isProfile: isProfile, displayNamePlural: identifier, displayNameSingular: identifier });
}