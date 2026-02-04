import { IEntityType, MultipleEntitySplitByAndFilterBy } from "../../../../../BrandVueApi";
import { EntityInstance } from "../../../../../entity/EntityInstance";

export interface IFilterInstancePickerProps {
    entityType: IEntityType;
    selectedInstances: EntityInstance[];
    allInstances: EntityInstance[];
    config?: MultipleEntitySplitByAndFilterBy;
    updatePartWithConfig?(newConfig: MultipleEntitySplitByAndFilterBy): void;
}