import {
    SingleGroupVariableDefinition,
    GroupedVariableDefinition,
    VariableGrouping,
    InstanceListVariableComponent,
    VariableConfigurationModel,
    EntityType,
    BaseGroupedVariableDefinition,
} from "../../../../../BrandVueApi";
import { VariableGroupWithSample } from "./GroupedVariableModalContent";
import { Metric } from "../../../../../metrics/metric";
import { CreateVariableDefinition } from "./GroupedVariableModalContentHelper";

//setup
const mockMetricSingleEntity = new Metric(null);
mockMetricSingleEntity.variableConfigurationId = 1;
mockMetricSingleEntity.entityCombination = [
    new EntityType({displayNamePlural: "1", displayNameSingular: "1", identifier:"1", isBrand: false, isProfile: false}),
];

//we always use groupedVariableDefinition by default
const variableDefinitionSingleEntity = new GroupedVariableDefinition();

const mockVariableSingleEntity = new VariableConfigurationModel({
    id: 1,
    identifier: 'mockIdentifier',
    definition: variableDefinitionSingleEntity,
    displayName: 'mockIdentifier',
    productShortCode: 'survey',
    subProductId: "test"
});

const mockInstanceListComponentSingleEntity = new InstanceListVariableComponent();
mockInstanceListComponentSingleEntity.fromVariableIdentifier = 'mockIdentifier';

const mockVariableGroupSingleEntity: VariableGrouping = new VariableGrouping({
    toEntityInstanceId: 1,
    toEntityInstanceName: 'Group 1',
    component: mockInstanceListComponentSingleEntity,        
});

const newGroupsSingleEntity: VariableGroupWithSample[] = [
    { group: mockVariableGroupSingleEntity, sample: undefined }
];

variableDefinitionSingleEntity.groups = [mockVariableGroupSingleEntity];
variableDefinitionSingleEntity.toEntityTypeName = 'EntityTypeName';
variableDefinitionSingleEntity.toEntityTypeDisplayNamePlural = 'EntityTypeNamePlural';

const mockMetricMultiEntity = new Metric(null);
mockMetricMultiEntity.variableConfigurationId = 1;
mockMetricMultiEntity.entityCombination = [
    new EntityType({displayNamePlural: "1", displayNameSingular: "1", identifier:"1", isBrand: false, isProfile: false}),
    new EntityType({displayNamePlural: "2", displayNameSingular: "2", identifier:"2", isBrand: false, isProfile: false}),
];

const variableDefinitionMultiEntity = new GroupedVariableDefinition();    
const mockVariableMultiEntity = new VariableConfigurationModel({
    id: 1,
    identifier: 'mockIdentifier',
    definition: variableDefinitionMultiEntity,
    displayName: 'mockIdentifier',
    productShortCode: 'survey',
    subProductId: "test"
});

const mockInstanceListComponentMultiEntity = new InstanceListVariableComponent();
mockInstanceListComponentMultiEntity.fromVariableIdentifier = 'mockIdentifier';

const mockVariableGroupMultiEntity: VariableGrouping = new VariableGrouping({
    toEntityInstanceId: 1,
    toEntityInstanceName: 'Group 1',
    component: mockInstanceListComponentMultiEntity,        
});

const newGroupsMultiEntity: VariableGroupWithSample[] = [
    { group: mockVariableGroupMultiEntity, sample: undefined }
];

variableDefinitionMultiEntity.groups = [mockVariableGroupMultiEntity];
variableDefinitionMultiEntity.toEntityTypeName = 'EntityTypeName';
variableDefinitionMultiEntity.toEntityTypeDisplayNamePlural = 'EntityTypeNamePlural';


describe('CreateVariableDefinition', () => {
    test('should return SingleGroupVariableDefinition for variable based on two entity metric', () => {    
        const result = CreateVariableDefinition({newGroups: newGroupsMultiEntity,
            metrics: [mockMetricMultiEntity],
            variableDefinition: variableDefinitionMultiEntity,
            variableName: 'Test Variable',
            variables: [mockVariableMultiEntity],
            isBase: false,
            flattenMultiEntity: false
        });
        expect(result).toBeInstanceOf(SingleGroupVariableDefinition);
        const SGVD = result as SingleGroupVariableDefinition;
        expect(SGVD.group).toEqual(mockVariableGroupMultiEntity);
    });

    test('should return GroupedVariableDefinition for variable created for single entity metric using a single group', () => {
        const result = CreateVariableDefinition({newGroups: newGroupsSingleEntity,
            metrics: [mockMetricSingleEntity],
            variableDefinition: variableDefinitionSingleEntity,
            variableName: 'Test Variable',
            variables: [mockVariableSingleEntity],
            isBase: false,
            flattenMultiEntity: false
        });
        expect(result).toBeInstanceOf(GroupedVariableDefinition);
        const GVD = result as GroupedVariableDefinition;
        expect(GVD.groups.length).toEqual(1);
    });

    test('should return GroupedVariableDefinition for variable created for single entity metric using two groups', () => {
        const mockVariableGroupSingleEntity: VariableGrouping = new VariableGrouping({
            toEntityInstanceId: 2,
            toEntityInstanceName: 'Group 2',
            component: mockInstanceListComponentSingleEntity,        
        });
    
        const secondVariableGroup: VariableGroupWithSample[] = [
            { group: mockVariableGroupSingleEntity, sample: undefined }
        ];
    
        const newGroupsConcat = newGroupsSingleEntity.concat(secondVariableGroup);
        const result = CreateVariableDefinition({newGroups: newGroupsConcat,
            metrics: [mockMetricSingleEntity],
            variableDefinition: variableDefinitionSingleEntity,
            variableName: 'Test Variable',
            variables: [mockVariableSingleEntity],
            isBase: false,
            flattenMultiEntity: false
        });
        expect(result).toBeInstanceOf(GroupedVariableDefinition);
        const GVD = result as GroupedVariableDefinition;
        expect(GVD.groups.length).toEqual(2);
    });

    test('should return BaseGroupedVariableDefinition when isBase is true', () => {
        const result = CreateVariableDefinition({newGroups: newGroupsSingleEntity,
            metrics: [mockMetricSingleEntity],
            variableDefinition: variableDefinitionSingleEntity,
            variableName: 'Test Variable',
            variables: [mockVariableSingleEntity],
            isBase: true,
            flattenMultiEntity: false
        });
        expect(result).toBeInstanceOf(BaseGroupedVariableDefinition);
    });
});