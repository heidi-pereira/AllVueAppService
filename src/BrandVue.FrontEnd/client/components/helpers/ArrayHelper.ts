export class ArrayHelper {
    public static isEqual(a: any[], b: any[]) : boolean {
        if (a === b) return true;
        if (a == null || b == null) return false;
        if (a.length !== b.length) return false;
        return a.every((item, index) => item === b[index]);
    }

    public static groupBy<T>(a: T[], e: (current:any)=>string) : { [id: string] : any[]; } {
        return a.reduce((acc, cur) => {
            const key = e(cur);
            if (!acc[key]) acc[key] = [];
            acc[key].push(cur);
            return acc;
        }, {});
    }
    
    public static maxBy(array : any[], accessor:(T)=>number) {
        return array.reduce((c,e)=>accessor(c) > accessor(e) ? c : e)
    }

    public static sumBy(array : any[], accessor:(T)=>number) {
        return array.reduce((c,e)=>c + accessor(e), 0)
    }
    
    /// Sorts the array by the given accessor function, maintaining the original order of elements with the same priority.
    public static prioritySortThenMaintainOrder<T>(
        array: T[],
        accessor: (item: T) => string,
        priorityOrder: string[]
    ): T[] {
        return array.toSorted((a, b) => {
            const indexA = priorityOrder.indexOf(accessor(a));
            const indexB = priorityOrder.indexOf(accessor(b));

            return (indexA === -1 ? Infinity : indexA) - (indexB === -1 ? Infinity : indexB);
        });
    }
}

interface Array<T> {
    maxBy(accessor:(T)=>number): T;
    sumBy(accessor:(T)=>number): number;
}