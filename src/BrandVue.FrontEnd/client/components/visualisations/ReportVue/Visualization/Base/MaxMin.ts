export class MaxMin {
    
	public Max?: number;
	public Min?: number;
	constructor(min?: number, max?: number) {
		this.Min = min;
		this.Max = max;
	}

	public static GetMaxMin(items: any[], prop: string) {
		let maxMin = new MaxMin(undefined, undefined);
		items.forEach(function (item) {
			let itemValue = item[prop];
			maxMin.Min = !maxMin.Min ? itemValue : maxMin.Min > itemValue ? itemValue : maxMin.Min;
			maxMin.Max = !maxMin.Max ? itemValue : maxMin.Max < itemValue ? itemValue : maxMin.Max;
		});
		return maxMin;
	}
}
