import { MultiMetricResults } from "../../../BrandVueApi";
import { EntityInstance } from "../../../entity/EntityInstance";
import { Metric } from "../../../metrics/metric";

export default class Comparison {
    readonly key: string;
    readonly isValid: boolean;
    readonly hasData: boolean;

    constructor(readonly brand: EntityInstance, readonly metric: Metric | null, readonly data: MultiMetricResults | null) {
        this.key = `${brand.name}-${metric?.name ?? 'null'}`;
        this.isValid = EntityInstance.isValidBrand(brand.id) && this.metric != null;
        this.hasData = data != null;
    }
}