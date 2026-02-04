import { IEntityType } from "../BrandVueApi";
import { EntityInstance } from "./EntityInstance";

export class FilterInstance {
    public readonly type: IEntityType;
    public readonly instance: EntityInstance;
    
    public constructor(type: IEntityType, instance: EntityInstance) {
        this.type = type;
        this.instance = instance;
    }
}