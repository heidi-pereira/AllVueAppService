import {
    ComparisonPeriodSelection,
    CompositeFilterModel,
    DemographicFilter,
    EntityInstanceRequest,
    FilterOperator,
    GroupedVariableDefinition,
    IEntityType,
    InclusiveRangeVariableComponent,
    InstanceListVariableComponent,
    InstanceVariableComponentOperator,
    Period,
    SigConfidenceLevel,
    TemporaryVariableInstanceRequestModel,
    TemporaryVariableRequestModel,
    VariableComponent,
    VariableGrouping,
    VariableRangeComparisonOperator
} from "../../BrandVueApi";
import { getSplitByAndFilterByEntityTypes } from "../../components/helpers/SurveyVueUtils";
import { IEntityConfiguration } from "../../entity/EntityConfiguration";
import { CuratedFilters } from "../../filter/CuratedFilters";
import { Metric } from "../../metrics/metric";
import { ITimeSelectionOptions } from "../../state/ITimeSelectionOptions";
import { MappedTableGrouping, TableFilterInstance, TableItem, TableItemInstance, TableItemInstances } from "./TableBuilderTypes";

export const Int32 = {
    MinValue: -2147483648,
    MaxValue: 2147483647
};

export const ProfileEntityType: IEntityType = {
    identifier: 'profile',
    displayNameSingular: 'Profile',
    displayNamePlural: 'Profile',
    isProfile: true,
    isBrand: false
};

export function plural(count: number, singular: string, plural: string) {
    return count === 1 ? singular : plural;
}

export function camelCaseToWords(str: string): string {
    return str.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase();
}

export function copyTableItem(tableItem: TableItem): TableItem {
    //metric does not need to be cloned, re-use the reference
    return {
        ...tableItem,
        primaryInstances: copyTableItemInstances(tableItem.primaryInstances),
        filterInstances: tableItem.filterInstances.map(i => copyTableItemInstances(i))
    };
}

function copyTableItemInstances(instanceCollection: TableItemInstances): TableItemInstances {
    return {
        ...instanceCollection,
        instances: instanceCollection.instances.map(i => ({
            ...i,
            instanceIds: [...i.instanceIds]
        }))
    };
}

export function getTableItemForMetric(metric: Metric, entityConfiguration: IEntityConfiguration): TableItem {
    const entityTypes = getSplitByAndFilterByEntityTypes(metric, metric.defaultSplitByEntityTypeName, entityConfiguration);
    const primaryInstances = entityTypes.splitByEntityType
        ? getTableItemInstancesForEntityType(entityTypes.splitByEntityType, entityConfiguration)
        : getZeroEntityTableItemInstances(metric);
    return {
        metric: metric,
        label: metric.helpText ?? metric.displayName ?? metric.varCode,
        primaryInstances: primaryInstances,
        filterInstances: entityTypes.filterByEntityTypes.map(et => getTableItemInstancesForEntityType(et, entityConfiguration))
    };
};

function getTableItemInstancesForEntityType(entityType: IEntityType, entityConfiguration: IEntityConfiguration): TableItemInstances {
    const entityInstances = entityConfiguration.getAllEnabledInstancesForTypeOrdered(entityType);
    return {
        entityType: entityType,
        instances: entityInstances.map((instance, index) => ({
            instanceIds: [instance.id],
            label: instance.name,
            originalLabel: instance.name,
            enabled: true,
            isNet: false,
            originalIndex: index
        }))
    };
}

function getZeroEntityTableItemInstances(metric: Metric): TableItemInstances {
    return {
        entityType: ProfileEntityType,
        instances: [{
            instanceIds: [],
            label: metric.displayName,
            originalLabel: metric.displayName,
            enabled: true,
            isNet: false,
            originalIndex: 0
        }]
    };
}

export function tableItemsToTemporaryVariableRequestModel(
    groupedRows: MappedTableGrouping[],
    groupedColumns: MappedTableGrouping[],
    subsetId: string,
    curatedFilters: CuratedFilters,
    timeSelection: ITimeSelectionOptions
) {
    return new TemporaryVariableRequestModel({
        subsetId: subsetId,
        period: new Period({
            average: curatedFilters.average.averageId,
            comparisonDates: curatedFilters.comparisonDates(false, timeSelection, false, ComparisonPeriodSelection.CurrentPeriodOnly)
        }),
        activeBrandId: -1,
        demographicFilter: new DemographicFilter(),
        filterModel: new CompositeFilterModel({
            filterOperator: FilterOperator.And,
            filters: [],
            compositeFilters: [],
        }),

        rows: groupedRows.map(rowGroup => tableGroupingToVariableRequest(rowGroup, 'row')),
        breaks: groupedColumns.map(colGroup => tableGroupingToVariableRequest(colGroup, 'column')),
    })
}

function tableGroupingToVariableRequest(grouping: MappedTableGrouping, type: 'row' | 'column'): TemporaryVariableInstanceRequestModel {
    return new TemporaryVariableInstanceRequestModel({
        filterBy: grouping.filterInstances.map(fi => new EntityInstanceRequest({
            type: fi.entityType.identifier,
            entityInstanceIds: fi.instance.instanceIds
        })),
        definition: new GroupedVariableDefinition({
            toEntityTypeName: `${grouping.groupedLabel}_temporary_${type}_variable`,
            toEntityTypeDisplayNamePlural: '',
            groups: grouping.items.instances.map((instance, index) => new VariableGrouping({
                toEntityInstanceId: index + 1,
                toEntityInstanceName: instance.label,
                component: getVariableComponentFrom(
                    grouping.items.entityType,
                    grouping.metric.primaryVariableIdentifier,
                    instance,
                    grouping.filterInstances
                ),
            }))
        })
    });
}

function getVariableComponentFrom(
    entityType: IEntityType,
    primaryVariableIdentifier: string,
    instance: TableItemInstance,
    filterInstances: TableFilterInstance[]
): VariableComponent {
    if (entityType === ProfileEntityType) {
        return new InclusiveRangeVariableComponent({
            fromVariableIdentifier: primaryVariableIdentifier,
            operator: VariableRangeComparisonOperator.GreaterThan,
            min: Int32.MinValue,
            max: Int32.MaxValue,
            exactValues: [],
            inverted: false,
            resultEntityTypeNames: filterInstances.map(fi => fi.entityType.identifier)
        });
    } else {
        return new InstanceListVariableComponent({
            fromVariableIdentifier: primaryVariableIdentifier,
            fromEntityTypeName: entityType.identifier,
            operator: InstanceVariableComponentOperator.Or,
            instanceIds: instance.instanceIds,
            resultEntityTypeNames: filterInstances.map(fi => fi.entityType.identifier)
        });
    }
}

export function getSignificanceLevelText(level: SigConfidenceLevel): string {
    switch (level) {
        case SigConfidenceLevel.Ninety:
            return "90%";
        case SigConfidenceLevel.NinetyEight:
            return "98%";
        case SigConfidenceLevel.NinetyNine:
            return "99%";
        case SigConfidenceLevel.NinetyFive:
        default:
            return "95% (Default)";
    }
}

export function cartesianProduct<T>(arrays: T[][], maxOutputSize: number = 100000): T[][] {
    const totalCombinations = arrays.reduce((acc, arr) => acc * arr.length, 1);
    if (totalCombinations > maxOutputSize) {
        throw new Error(`Cartesian product too large: would produce ${totalCombinations} combinations.`);
    }
    const result: T[][] = [[]];
    for (const arr of arrays) {
        const temp: T[][] = [];
        for (const prev of result) {
            for (const el of arr) {
                temp.push([...prev, el]);
            }
        }
        result.splice(0, result.length, ...temp);
    }
    return result;
}

export function mapTableGroupings(tableItems: TableItem[], allowMultiEntity: boolean): MappedTableGrouping[] {
    const items = allowMultiEntity ? tableItems : tableItems.filter(ti => ti.filterInstances.length === 0);
    return items.flatMap(tableItem => {
        const enabledPrimaryInstances: TableItemInstances = {
            ...tableItem.primaryInstances,
            instances: tableItem.primaryInstances.instances.filter(i => i.enabled)
        };
        if (tableItem.filterInstances.length === 0) {
            return [{
                metric: tableItem.metric,
                groupedLabel: tableItem.label,
                items: enabledPrimaryInstances,
                filterInstances: []
            }];
        } else {
            const enabledFilterInstances = tableItem.filterInstances
                .map(fi => fi.instances.filter(i => i.enabled && !i.isNet).map(i => ({
                    entityType: fi.entityType,
                    instance: i
                })))
                .filter(fi => fi.length > 0);
            const productOfFilterInstances = cartesianProduct(enabledFilterInstances);

            return productOfFilterInstances.flatMap(filterInstanceCombination => {
                const label = `${tableItem.label} - ${filterInstanceCombination.map(i => i.instance.label).join(', ')}`;
                return {
                    metric: tableItem.metric,
                    groupedLabel: label,
                    items: enabledPrimaryInstances,
                    filterInstances: filterInstanceCombination
                };
            });
        }
    }).filter(tableItem => tableItem.items.instances.length > 0);
}