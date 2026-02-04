import {Metric} from "../../metrics/metric";
import {EntitySet} from "../../entity/EntitySet";
import * as BrandVueApi from "../../BrandVueApi";
import {IEntityConfiguration} from "../../entity/EntityConfiguration";
import {useEffect, useState} from "react";

export const MAX_TABLES_PER_PAGE = 25;

export type PaginationData = {
    currentPageNo: number,
    noOfTablesPerPage: number,
    totalNoOfTables: number
}

export const usePaginationDict = (
    initialState: {[paginationKey: string]: PaginationData},
    paginationKey: string | undefined,
    metric: Metric | undefined,
    entitySet: EntitySet | undefined,
    secondaryEntitySets: EntitySet[],
    entityConfiguration: IEntityConfiguration) => {
    const [paginationDict, setPaginationDict] = useState<{[paginationKey: string]: PaginationData}>(initialState)

    useEffect(() => {
        const currentPaginationData = getCurrentPaginationData()
        if (currentPaginationData && metric && entitySet) {
            const tableCount = getTotalNoOfTables(metric, entitySet, secondaryEntitySets, entityConfiguration)
            if (currentPaginationData.totalNoOfTables != tableCount) {
                setCurrentPaginationData(1, currentPaginationData.noOfTablesPerPage, tableCount)
            }
        }
    }, [paginationKey, paginationDict, metric?.name, entitySet?.id, JSON.stringify(entitySet?.getInstances().getAll().map(e => e.id).sort()), JSON.stringify(secondaryEntitySets.map(e => e.id).sort()), entityConfiguration])


    const setCurrentPaginationData = (pageNo: number, noOfTablesPerPage: number, totalNoOfTables: number) => {
        if (paginationKey && paginationKey in paginationDict){
            setPaginationDict({...paginationDict, [paginationKey] : {currentPageNo: pageNo, noOfTablesPerPage: noOfTablesPerPage, totalNoOfTables: totalNoOfTables}})
        }
    }

    const getCurrentPaginationData = (): PaginationData => {
        if (paginationKey && paginationKey in paginationDict) {
            return paginationDict[paginationKey]
        }
        return {noOfTablesPerPage: MAX_TABLES_PER_PAGE, currentPageNo: 1, totalNoOfTables: 1}
    }

    const getTotalNoOfTables = (metric: Metric, entitySet: EntitySet, secondaryEntitySets: EntitySet[], entityConfiguration: IEntityConfiguration): number => {
        function getSetForType(type: BrandVueApi.IEntityType): EntitySet | undefined {
            if (type.identifier === entitySet.type.identifier) {
                return entitySet;
            } else {
                return secondaryEntitySets.find(set => type.identifier === set.type.identifier);
            }
        }
        const splitByType = entitySet.type;
        const filterByTypes = metric.entityCombination.filter(et => et.identifier !== splitByType.identifier);
        const filterBySets = filterByTypes.map(et => getSetForType(et) ?? entityConfiguration.getAllEnabledInstancesOrderedAsSet(et));
        const numberInstances = filterBySets.map(s => s.getInstances().getAll().length);
        return numberInstances.reduce((a, b) => a * b, 1);
    }

    return {
        paginationDict: paginationDict,
        setPaginationDict: setPaginationDict,
        setCurrentPaginationData: setCurrentPaginationData,
        getCurrentPaginationData: getCurrentPaginationData
    }
}