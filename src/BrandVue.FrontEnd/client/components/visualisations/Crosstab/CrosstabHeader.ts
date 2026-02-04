import { CrosstabCategory } from "../../../BrandVueApi";

export class CrosstabHeader {
    id: string;
    name: string | undefined;
    significanceIdentifier: string | undefined;
    subHeaders: CrosstabHeader[];
    depth: number;
    columnSpan: number;
    columnHasData: boolean;

    public constructor(id: string, name: string | undefined, significanceIdentifier: string | undefined, subHeaders: CrosstabHeader[]) {
        this.id = id;
        this.name = name;
        this.significanceIdentifier = significanceIdentifier;
        this.subHeaders = subHeaders;
        this.depth = this.getDepth(subHeaders);
        this.columnSpan = this.getColumnSpan(subHeaders);
    }

    public static fromApi(category: CrosstabCategory): CrosstabHeader {
        return new CrosstabHeader(category.id,
            category.displayName ?? category.name,
            category.significanceIdentifier,
            category.subCategories.map(CrosstabHeader.fromApi))
    }

    public extendToDepth(depth: number): CrosstabHeader {
        if (this.depth === depth) return this;

        const newRoot = new CrosstabHeader(this.id, undefined, undefined, [this]);
        return newRoot.extendToDepth(depth);
    }

    public getColumnsAtDepth(depth: number): CrosstabHeader[] {
        if (this.depth === depth) return [this];
        return this.subHeaders.reduce<CrosstabHeader[]>((all, subHeader) => all.concat(subHeader.getColumnsAtDepth(depth)), [])
    }

    private getDepth(subHeaders: CrosstabHeader[]): number {
        if (subHeaders.length === 0) return 0;

        return 1 + Math.max(...subHeaders.map(s => s.depth));
    }

    private getColumnSpan(subHeaders: CrosstabHeader[]): number {
        if (subHeaders.length === 0) return 1;
        return subHeaders.reduce((sum, subHeader) => sum + subHeader.columnSpan, 0);
    }
}