import * as BrandVueApi from "../BrandVueApi";

export class EntityInstance {

    public static AllInstancesId: number = -1;

    public static AllInstances: EntityInstance = new EntityInstance(-1, "All Instances");

    public id: number;
    public name: string;
    public enabledBySubset: { [key: string]: boolean; } = {};
    public imageUrl: string;


    public categories;
    public filters;

    constructor(id?: number, name?: string, enabled?: { [key: string]: boolean; }, imageUrl?:string) {
        if (id === 0 || id) {
            this.id = id as number;
        }

        if (name) {
            this.name = name as string;
        }

        if (enabled) {
            this.enabledBySubset = enabled as { [key: string]: boolean; };;
        }
        if (imageUrl) {
            this.imageUrl = imageUrl as string;
        }
    }

    public static isValidBrand(brandId: number): boolean {
        return brandId > this.AllInstancesId;
    }
    public static isAllBrands(brandId: number): boolean {
        return brandId === this.AllInstancesId;
    }

    public static convertInstanceFromApi(instance: BrandVueApi.IEntityInstance) {
        const newInstance = new EntityInstance();
        newInstance.id = instance.id;
        newInstance.name = instance.name;
        newInstance.enabledBySubset = instance.enabledBySubset;
        newInstance.imageUrl = instance.imageUrl;

        return newInstance;
    }

    public clone(): EntityInstance {
        return new EntityInstance(this.id, this.name, this.enabledBySubset, this.imageUrl);
    }
}

interface IEntityInstanceSortOptions {
    ignoreCase?: boolean;
}

export function sortEntityInstances(ei1: EntityInstance, ei2: EntityInstance, options?: IEntityInstanceSortOptions) {
    let name1: string = ei1.name;
    let name2: string = ei2.name;
    if (!(options?.ignoreCase === false)) {
        name1 = name1.toLowerCase();
        name2 = name2.toLowerCase();
    }
    
    if (name1 > name2) return 1;
    if (name1 < name2) return -1;
    return 0;
}
