import * as BrandVueApi from "./BrandVueApi";

    export class periodResult {

        // This exists in lodash, but seems heavyweight for a really minor perf gain
        private static getLast<T>(arr: T[], predicate: (t: T) => boolean) {
            for (let i = arr.length - 1; i >= 0; i--) {
                if (predicate(arr[i])) {
                    return arr[i];
                }
            }
            return null;
        }

        public static getRank(brandWeightedDailyResults: BrandVueApi.EntityWeightedDailyResults[], title: string, brandId: number, dateToCompare: Date): number {
            var l: rnkItem[] = [];
            var brandRes: rnkItem | undefined;
            brandWeightedDailyResults.forEach(entityResults => {
                const b = entityResults.entityInstance;
                if (b.id !== 1000) {

                    const res = periodResult.getLast(entityResults.weightedDailyResults, bw => bw.date.getTime() === dateToCompare.getTime());
                    if (res) {
                        const ri = new rnkItem(b.id, res.weightedResult, 0);
                        if (b.id === brandId) {
                            brandRes = ri;
                        };
                        l.push(ri);
                    }
                }
            });
            l.sort((a, b) => b.val - a.val);
            var tr: number = 1;
            l.forEach(r => {
                r.rnk = tr;
                tr += 1;
            });
            if (brandRes) {
                return brandRes.rnk;
            } else {
                return 0;
            }
        }
    }

    export class rnkItem {

        public id: number;
        public val: number;
        public rnk: number;

        constructor(id: number, val: number, rnk: number) {
            this.id = id;
            this.val = val;
            this.rnk = rnk;
        }

    }



