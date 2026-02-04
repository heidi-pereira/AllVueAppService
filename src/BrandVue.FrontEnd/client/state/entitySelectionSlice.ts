import {createSlice, PayloadAction} from '@reduxjs/toolkit';
import {CategorySortKey, IEntityType,} from "../BrandVueApi";
import {EntitySet} from "../entity/EntitySet";
import {EntityInstance} from "../entity/EntityInstance";
import {categorySortMap} from "../components/helpers/CategorySortKeyHelper";

export interface EntitySelectionState {
    entitySets: Record<string, IEntitySetSelection>;
    priorityOrderedEntityTypes: IEntityType[];
    categorySortKey: CategorySortKey;
    activeBreaks: IActiveBreaks;
}

export interface IActiveBreaks {
    audienceId?: number;
    selectedInstanceOrMappingIds?: number[];
    multipleChoiceByValue?: boolean;
}

export interface IEntitySetSelection {
    active?: number;
    highlighted?: number[];
    entitySetId?: number;
    entitySetAverages?: number[];
}

const initialState: EntitySelectionState = {
    activeBreaks: {},
    entitySets: {},
    priorityOrderedEntityTypes: [],
    categorySortKey: CategorySortKey.None
};

const entitySelectionSlice = createSlice({
    name: 'entityState',
    initialState,
    reducers: {
        setActiveBreaks(state, action: PayloadAction<IActiveBreaks>) {
            state.activeBreaks = action.payload;
        },
        setEntitySets(state, action: PayloadAction<{ selections: {entityType:string, entitySet: IEntitySetSelection}[], priorityOrderedEntityTypes?:IEntityType[], categorySortKey?: CategorySortKey | null }>) {
            let {selections, priorityOrderedEntityTypes} = action.payload;
            for(let selection of selections) {
                state.entitySets[selection.entityType] = selection.entitySet;
            }
            if(priorityOrderedEntityTypes) {
                state.priorityOrderedEntityTypes = priorityOrderedEntityTypes;
            }
            state.categorySortKey = action.payload.categorySortKey ?? state.categorySortKey;
        },
        setActiveEntitySet(state, action: PayloadAction<{entitySet: EntitySet}>) {
            const { entitySet } = action.payload;
            state.entitySets[entitySet.type.identifier] = {
                active: entitySet.mainInstance?.id,
                highlighted: entitySet.getInstances().getAll().map(x=>x.id),
                entitySetId: entitySet.id,
                entitySetAverages: entitySet.getAverages().getAll().filter(a=>a.entitySetId).map(a=>a.entitySetId),
            };
        },
        setActiveEntityInstance(state, action: PayloadAction<{entityType: IEntityType, instance: EntityInstance}>) {
            const { entityType, instance } = action.payload;
            if(state.entitySets[entityType.identifier]) {
                state.entitySets[entityType.identifier].active = instance.id;
            }
        },
        setChartAxes(state, action: PayloadAction<IEntityType[]>) {
            state.priorityOrderedEntityTypes = action.payload;
        },
        setSplitBy(state, action: PayloadAction<IEntityType>) {
            state.priorityOrderedEntityTypes = state.priorityOrderedEntityTypes.some(x=>x.identifier === action.payload.identifier) 
                ? [...state.priorityOrderedEntityTypes].toSorted((x) => x.identifier == action.payload.identifier ? -1 : 1) 
                : [action.payload, ...state.priorityOrderedEntityTypes];
        },
        setFilterBy(state, action: PayloadAction<{filterBy: EntityInstance, filterByType: IEntityType}>) {
            state.entitySets[action.payload.filterByType.identifier] = {
                ...state.entitySets[action.payload.filterByType.identifier] ?? {},
                active: action.payload.filterBy.id
            };
        },
        setCategorySortKey(state, action: PayloadAction<string>) {
            state.categorySortKey = categorySortMap[action.payload] || CategorySortKey.None;
        }
    },
});

export const { setActiveBreaks, setEntitySets, setActiveEntitySet, setActiveEntityInstance,  setChartAxes, setSplitBy, setFilterBy, setCategorySortKey } = entitySelectionSlice.actions;
export default entitySelectionSlice.reducer;